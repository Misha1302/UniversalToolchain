using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

/// <summary>
/// Assembles one executable route runtime from immutable component catalogs exported by all
/// packages selected by a language plan. Component implementations are bound to the exact
/// package manifests captured in the plan, not merely to matching package IDs and versions.
/// </summary>
public static class LanguageRouteRuntimeAssembler
{
    public static LanguageRouteRuntimeProvider CreateProvider(
        LanguagePlan plan,
        IEnumerable<ILanguageRouteComponentSource> componentSources)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(componentSources);
        if (plan.RuntimeProvider == null || plan.RuntimeProviderContribution == null)
            throw new InvalidOperationException("A planning-only language plan cannot assemble a route runtime provider.");

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
        ValidateSelectedImplementations(plan, selectedSources);

        var registry = new LanguageRouteComponentRegistry();
        foreach (var source in selectedSources.Values
                     .OrderBy(static source => source.Descriptor.Id.Value, StringComparer.Ordinal)
                     .ThenBy(static source => source.Descriptor.Version.Value, StringComparer.Ordinal))
        {
            registry.AddCatalog(source.Components);
        }

        return new LanguageRouteRuntimeProvider(
            plan.RuntimeProvider.ProviderId,
            plan.RuntimeProvider.Version,
            plan.Definition.ToolchainApiVersion,
            plan.RuntimeProviderContribution.Contribution.Id,
            registry);
    }

    private static void ValidateSourceBindings(
        LanguagePlan plan,
        IReadOnlyDictionary<(LanguagePackageId PackageId, LanguageVersion PackageVersion), ILanguageRouteComponentSource> sources)
    {
        // The selected runtime contribution is executable state even when it does not itself
        // expose a transformer or executor. Its package source must therefore be present and
        // bound to the exact manifest used by planning.
        _ = GetRequiredSource(plan.RuntimeProviderContribution!, sources);

        foreach (var source in sources.Values)
        {
            if (source.Descriptor.ToolchainApiVersion != plan.Definition.ToolchainApiVersion)
            {
                throw new InvalidOperationException(
                    $"Runtime component source '{source.Descriptor.Id.Value}' targets Toolchain API " +
                    $"'{source.Descriptor.ToolchainApiVersion}', but the plan targets '{plan.Definition.ToolchainApiVersion}'.");
            }

            var selectedFromPackage = plan.Contributions
                .Where(contribution => contribution.PackageId == source.Descriptor.Id &&
                                       contribution.PackageVersion == source.Descriptor.Version)
                .ToArray();
            if (selectedFromPackage.Length == 0)
                continue;

            var actualManifest = LanguageFeatureManifestSerializer.ComputeSha256(source.Descriptor);
            var expectedManifests = selectedFromPackage
                .Select(static contribution => contribution.ManifestSha256)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (expectedManifests.Length != 1 ||
                !StringComparer.Ordinal.Equals(expectedManifests[0], actualManifest))
            {
                throw new InvalidOperationException(
                    $"Runtime component source '{source.Descriptor.Id.Value}' version '{source.Descriptor.Version.Value}' " +
                    "does not match the exact package manifest captured by the language plan.");
            }

            var declared = source.Descriptor.Contributions.Select(static item => item.Id).ToHashSet();
            var missing = selectedFromPackage
                .Select(static item => item.Contribution.Id)
                .Where(id => !declared.Contains(id))
                .OrderBy(static id => id.Value, StringComparer.Ordinal)
                .ToArray();
            if (missing.Length != 0)
            {
                throw new InvalidOperationException(
                    $"Runtime component source '{source.Descriptor.Id.Value}' does not declare selected contribution(s): " +
                    string.Join(", ", missing.Select(static id => id.Value)) + ".");
            }
        }
    }

    private static void ValidateSelectedImplementations(
        LanguagePlan plan,
        IReadOnlyDictionary<(LanguagePackageId PackageId, LanguageVersion PackageVersion), ILanguageRouteComponentSource> sources)
    {
        foreach (var route in plan.Routes.Values)
        {
            foreach (var step in route.Steps)
            {
                var contribution = plan.Contributions.Single(item => item.Contribution.Id == step.ContributionId);
                var source = GetRequiredSource(contribution, sources);
                if (!source.Components.Transformers.TryGetValue(step.ContributionId, out var transformer))
                {
                    throw new InvalidOperationException(
                        $"Package '{contribution.PackageId.Value}' version '{contribution.PackageVersion.Value}' does not export transformer implementation '{step.ContributionId.Value}'.");
                }
                if (transformer.SourceContract != step.SourceContract || transformer.TargetContract != step.TargetContract)
                {
                    throw new InvalidOperationException(
                        $"Transformer implementation '{step.ContributionId.Value}' does not match the artifact contracts selected by the language plan.");
                }
            }

            var capability = LanguageCapabilities.Backend(route.Backend);
            var backendContribution = plan.Contributions.Single(
                item => item.Contribution.ProvidesCapabilities.Contains(capability));
            var backendSource = GetRequiredSource(backendContribution, sources);
            if (!backendSource.Components.Executors.Any(executor =>
                    executor.ContributionId == backendContribution.Contribution.Id &&
                    executor.Backend == route.Backend &&
                    LanguageArtifactRoute.ContractsConnect(route.TargetContract, executor.InputContract)))
            {
                throw new InvalidOperationException(
                    $"Package '{backendContribution.PackageId.Value}' version '{backendContribution.PackageVersion.Value}' does not export an executor implementation matching contribution '{backendContribution.Contribution.Id.Value}', backend '{route.Backend.Value}', and input '{route.TargetContract}'.");
            }
        }
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

        if (!StringComparer.Ordinal.Equals(LanguageFeatureManifestSerializer.ComputeSha256(source.Descriptor), contribution.ManifestSha256))
        {
            throw new InvalidOperationException(
                $"Runtime component source '{contribution.PackageId.Value}' version '{contribution.PackageVersion.Value}' " +
                "does not match the exact package manifest captured by the language plan.");
        }
        return source;
    }
}
