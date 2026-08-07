using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

/// <summary>
/// Assembles one executable route runtime by binding already-planned contributions to exact
/// package registrations. Component loading is materialization only: it never expands or changes
/// the semantic selection captured by <see cref="LanguagePlan"/>.
/// </summary>
public static class LanguageRouteRuntimeAssembler
{
    public static LanguageRouteRuntimeProvider CreateProvider(
        LanguagePlan plan,
        IEnumerable<ILanguageRouteComponentSource> componentSources)
    {
        var registry = CreateRegistry(plan, componentSources);
        return CreateProvider(plan, registry);
    }

    internal static LanguageRouteRuntimeProvider CreateProvider(
        LanguagePlan plan,
        LanguageRouteComponentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(registry);
        if (plan.RuntimeProvider == null || plan.RuntimeProviderContribution == null)
            throw new InvalidOperationException("A planning-only language plan cannot assemble a route runtime provider.");

        return new LanguageRouteRuntimeProvider(
            plan.RuntimeProvider.ProviderId,
            plan.RuntimeProvider.Version,
            plan.Definition.ToolchainApiVersion,
            plan.RuntimeProviderContribution.Contribution.Id,
            registry);
    }

    internal static LanguageRouteComponentRegistry CreateRegistry(
        LanguagePlan plan,
        IEnumerable<ILanguageRouteComponentSource> componentSources)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(componentSources);
        if (plan.RuntimeProvider == null || plan.RuntimeProviderContribution == null)
            throw new InvalidOperationException("A planning-only language plan cannot assemble route runtime components.");

        var sources = componentSources.ToArray();
        if (sources.Any(static source => source == null))
            throw new ArgumentException("Runtime component sources must not contain null entries.", nameof(componentSources));

        var duplicate = sources.GroupBy(static source => (source.Descriptor.Id, source.Descriptor.Version))
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate != null)
        {
            throw new InvalidOperationException(
                $"Multiple runtime component sources were supplied for package '{duplicate.Key.Id.Value}' version '{duplicate.Key.Version.Value}'.");
        }

        var selectedPackages = plan.Contributions
            .Select(static contribution => (contribution.PackageId, contribution.PackageVersion))
            .Distinct()
            .ToHashSet();
        var selectedSources = sources
            .Where(source => selectedPackages.Contains((source.Descriptor.Id, source.Descriptor.Version)))
            .ToDictionary(static source => (source.Descriptor.Id, source.Descriptor.Version));

        ValidateSourceBindings(plan, selectedSources);
        return BindSelectedImplementations(plan, selectedSources);
    }

    private static void ValidateSourceBindings(
        LanguagePlan plan,
        IReadOnlyDictionary<(LanguagePackageId PackageId, LanguageVersion PackageVersion), ILanguageRouteComponentSource> sources)
    {
        foreach (var source in sources.Values)
        {
            if (source.Descriptor.ToolchainApiVersion != plan.Definition.ToolchainApiVersion)
            {
                throw new InvalidOperationException(
                    $"Runtime component source '{source.Descriptor.Id.Value}' targets Toolchain API " +
                    $"'{source.Descriptor.ToolchainApiVersion}', but the plan targets '{plan.Definition.ToolchainApiVersion}'.");
            }
        }

        _ = GetRequiredSource(plan.RuntimeProviderContribution!, sources);
        foreach (var contribution in plan.Contributions)
            _ = GetRequiredSource(contribution, sources);
    }

    private static LanguageRouteComponentRegistry BindSelectedImplementations(
        LanguagePlan plan,
        IReadOnlyDictionary<(LanguagePackageId PackageId, LanguageVersion PackageVersion), ILanguageRouteComponentSource> sources)
    {
        var registry = new LanguageRouteComponentRegistry();
        var boundTransformers = new HashSet<LanguageContributionId>();
        var boundExecutors = new HashSet<(LanguageContributionId ContributionId, BackendId Backend, LanguageArtifactContract InputContract)>();

        foreach (var route in plan.Routes.Values.OrderBy(static route => route.Backend.Value, StringComparer.Ordinal))
        {
            foreach (var step in route.Steps)
            {
                var contribution = GetRequiredContribution(plan, step.ContributionId);
                var source = GetRequiredSource(contribution, sources);
                if (!source.Components.Transformers.TryGetValue(step.ContributionId, out var transformer))
                {
                    throw new InvalidOperationException(
                        $"Package '{contribution.PackageId.Value}' version '{contribution.PackageVersion.Value}' does not export transformer implementation '{step.ContributionId.Value}'.");
                }
                if (transformer.SourceContract != step.SourceContract || transformer.TargetContract != step.TargetContract)
                {
                    throw new InvalidOperationException(
                        $"Transformer implementation '{step.ContributionId.Value}' does not match the exact artifact contracts selected by the language plan.");
                }

                if (boundTransformers.Add(step.ContributionId))
                    registry.AddTransformer(transformer);
            }

            var capability = LanguageCapabilities.Backend(route.Backend);
            var backendContribution = plan.Contributions.Single(
                item => item.Contribution.ProvidesCapabilities.Contains(capability));
            var backendSource = GetRequiredSource(backendContribution, sources);
            var executors = backendSource.Components.Executors
                .Where(executor =>
                    executor.ContributionId == backendContribution.Contribution.Id &&
                    executor.Backend == route.Backend &&
                    executor.InputContract == route.TargetContract)
                .ToArray();
            if (executors.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Package '{backendContribution.PackageId.Value}' version '{backendContribution.PackageVersion.Value}' must export exactly one executor implementation matching contribution '{backendContribution.Contribution.Id.Value}', backend '{route.Backend.Value}', and input '{route.TargetContract}', but {executors.Length} were found.");
            }

            var executor = executors[0];
            var key = (executor.ContributionId, executor.Backend, executor.InputContract);
            if (boundExecutors.Add(key))
                registry.AddExecutor(executor);
        }

        return registry;
    }

    private static ResolvedLanguageContribution GetRequiredContribution(
        LanguagePlan plan,
        LanguageContributionId contributionId)
    {
        var matches = plan.Contributions
            .Where(contribution => contribution.Contribution.Id == contributionId)
            .ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"Language plan must contain exactly one resolved contribution '{contributionId.Value}', but {matches.Length} were found.");
        }
        return matches[0];
    }

    private static ILanguageRouteComponentSource GetRequiredSource(
        ResolvedLanguageContribution contribution,
        IReadOnlyDictionary<(LanguagePackageId PackageId, LanguageVersion PackageVersion), ILanguageRouteComponentSource> sources)
    {
        if (!sources.TryGetValue((contribution.PackageId, contribution.PackageVersion), out var source))
        {
            throw new InvalidOperationException(
                $"No runtime component source was supplied for selected package '{contribution.PackageId.Value}' version '{contribution.PackageVersion.Value}'.");
        }

        var actualManifest = LanguageFeatureManifestSerializer.ComputeSha256(source.Descriptor);
        if (!StringComparer.Ordinal.Equals(actualManifest, contribution.ManifestSha256))
        {
            throw new InvalidOperationException(
                $"Runtime component source '{contribution.PackageId.Value}' version '{contribution.PackageVersion.Value}' " +
                "does not match the exact package manifest captured by the language plan.");
        }
        if (!contribution.PackageIdentity.IsImplementationInstance(source))
        {
            throw new InvalidOperationException(
                $"Runtime component source '{contribution.PackageId.Value}' version '{contribution.PackageVersion.Value}' " +
                "is not the exact package implementation registered during language planning.");
        }
        if (!source.Descriptor.Contributions.Any(item => item.Id == contribution.Contribution.Id))
        {
            throw new InvalidOperationException(
                $"Runtime component source '{contribution.PackageId.Value}' does not declare selected contribution '{contribution.Contribution.Id.Value}'.");
        }

        return source;
    }
}
