using System.Diagnostics.CodeAnalysis;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

public sealed class LanguagePlanVerificationException : InvalidOperationException
{
    public LanguagePlanVerificationException(string message) : base(message)
    {
    }
}

public static class LanguagePlanVerifier
{
    public static void Verify(LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        RequireUnique(plan.Features.Select(static x => x.Feature.Id.Value), "feature");
        RequireUnique(plan.Contributions.Select(static x => x.Contribution.Id.Value), "contribution");
        ValidateDigests(plan.Features.Select(static x => (x.Feature.Id.Value, x.ManifestSha256)));
        ValidateDigests(plan.Contributions.Select(static x => (x.Contribution.Id.Value, x.ManifestSha256)));
        ValidateRegistrationIdentityConsistency(plan);

        var contributionsById = plan.Contributions.ToDictionary(static contribution => contribution.Contribution.Id);
        ValidateRuntimeProvider(plan, contributionsById);

        if (plan.Definition.IsExecutable)
        {
            if (plan.RuntimeProviderContribution == null)
                Fail("An executable language plan has no runtime-provider contribution.");
            if (plan.Routes.Count != plan.Definition.Backends.Count)
                Fail("An executable language plan must contain exactly one route per selected backend.");

            foreach (var backend in plan.Definition.Backends)
            {
                if (!plan.Routes.TryGetValue(backend, out var route))
                    Fail($"The executable language plan has no route for backend '{backend.Value}'.");
                ValidateRoute(plan, backend, route, contributionsById);
            }

            var extraRoutes = plan.Routes.Keys.Except(plan.Definition.Backends).ToArray();
            if (extraRoutes.Length != 0)
                Fail($"The plan contains routes for unselected backends: {string.Join(", ", extraRoutes.Select(static x => x.Value))}.");
        }
        else if (plan.Routes.Count != 0 || plan.RuntimeProviderContribution != null || plan.RuntimeProvider != null)
        {
            Fail("A planning-only language plan must not contain runtime routes or a runtime provider.");
        }

        var expectedHash = LanguagePlanCanonicalizer.ComputeHash(
            plan.Definition,
            plan.Features,
            plan.Contributions,
            plan.RuntimeProviderContribution,
            plan.Routes.Values);
        if (!StringComparer.Ordinal.Equals(expectedHash, plan.PlanHash))
            Fail("The language plan hash does not match its canonical content.");
    }

    internal static string RecomputeHash(LanguagePlan plan) => LanguagePlanCanonicalizer.ComputeHash(
        plan.Definition,
        plan.Features,
        plan.Contributions,
        plan.RuntimeProviderContribution,
        plan.Routes.Values);

    private static void ValidateRuntimeProvider(
        LanguagePlan plan,
        IReadOnlyDictionary<LanguageContributionId, ResolvedLanguageContribution> contributionsById)
    {
        if (plan.RuntimeProviderContribution == null)
        {
            if (plan.RuntimeProvider != null)
                Fail("The language plan exposes a runtime-provider reference without a selected provider contribution.");
            return;
        }

        var runtimeContributionId = plan.RuntimeProviderContribution.Contribution.Id;
        if (!contributionsById.TryGetValue(runtimeContributionId, out var selectedRuntimeContribution))
            Fail("The runtime-provider contribution is not part of the selected contribution set.");
        if (!ReferenceEquals(selectedRuntimeContribution, plan.RuntimeProviderContribution))
            Fail("The runtime-provider contribution does not reference the verified selected contribution instance.");

        var descriptor = plan.RuntimeProviderContribution.Contribution;
        if (descriptor.RuntimeProviderId == null || descriptor.RuntimeProviderVersion == null)
            Fail("The selected runtime-provider contribution does not declare a provider identity and version.");
        if (plan.RuntimeProvider == null ||
            plan.RuntimeProvider.ProviderId != descriptor.RuntimeProviderId.Value ||
            plan.RuntimeProvider.Version != descriptor.RuntimeProviderVersion.Value)
        {
            Fail("The runtime-provider reference does not match the selected provider contribution.");
        }
    }

    private static void ValidateRoute(
        LanguagePlan plan,
        BackendId backend,
        LanguageArtifactRoute route,
        IReadOnlyDictionary<LanguageContributionId, ResolvedLanguageContribution> contributionsById)
    {
        if (route.Backend != backend)
            Fail($"Route dictionary entry for backend '{backend.Value}' contains a mismatched route.");
        if (!LanguageArtifactRoute.ContractsConnect(plan.Definition.EntryArtifact, route.SourceContract))
            Fail($"Route for backend '{backend.Value}' does not start at the language entry artifact.");
        if (route.Steps.Count == 0)
            Fail($"Executable route for backend '{backend.Value}' must not be empty.");

        var backendCapability = LanguageCapabilities.Backend(backend);
        var backendOwners = plan.Contributions
            .Where(contribution => contribution.Contribution.ProvidesCapabilities.Contains(backendCapability))
            .ToArray();
        if (backendOwners.Length != 1)
            Fail($"Backend '{backend.Value}' must have exactly one verified contribution owner, but {backendOwners.Length} are present.");

        var backendOwner = backendOwners[0].Contribution;
        var runtimeInputs = plan.RuntimeProviderContribution!.Contribution.RuntimeInputContracts;
        var hasRuntimeInput = runtimeInputs.TryGetValue(backend, out var runtimeInputContract);
        LanguageArtifactContract expectedTarget;
        if (backendOwner.BackendInputContract is { } backendInputContract)
        {
            expectedTarget = backendInputContract;
            if (hasRuntimeInput && runtimeInputContract != backendInputContract)
            {
                Fail(
                    $"Backend '{backend.Value}' execution contract '{backendInputContract}' conflicts with runtime-provider input '{runtimeInputContract}'.");
            }
        }
        else if (hasRuntimeInput)
        {
            expectedTarget = runtimeInputContract;
        }
        else
        {
            Fail($"Backend '{backend.Value}' has no verified execution input contract.");
            return;
        }
        if (!LanguageArtifactRoute.ContractsConnect(route.TargetContract, expectedTarget))
            Fail($"Route for backend '{backend.Value}' does not end at the verified backend execution contract.");
        if (backendOwner.Transformation != null && route.Steps[^1].ContributionId != backendOwner.Id)
        {
            Fail(
                $"Route for backend '{backend.Value}' does not terminate with its selected transforming backend contribution '{backendOwner.Id.Value}'.");
        }

        var stepIndexes = new Dictionary<LanguageContributionId, int>();
        for (var index = 0; index < route.Steps.Count; index++)
        {
            var step = route.Steps[index];
            if (!stepIndexes.TryAdd(step.ContributionId, index))
                Fail($"Route for backend '{backend.Value}' repeats contribution '{step.ContributionId.Value}'.");
            if (!contributionsById.TryGetValue(step.ContributionId, out var resolvedContribution))
                Fail($"Route for backend '{backend.Value}' references unselected contribution '{step.ContributionId.Value}'.");

            var contribution = resolvedContribution.Contribution;
            var transformation = contribution.Transformation;
            if (transformation == null)
                Fail($"Route contribution '{step.ContributionId.Value}' has no planned artifact transformation.");
            if (contribution.SupportedBackends.Count != 0 && !contribution.SupportedBackends.Contains(backend))
                Fail($"Route contribution '{step.ContributionId.Value}' does not support backend '{backend.Value}'.");
            if (step.SourceContract != transformation.SourceContract ||
                step.TargetContract != transformation.TargetContract ||
                step.Cost != transformation.Cost)
            {
                Fail($"Route step '{step.ContributionId.Value}' does not match its verified contribution transformation.");
            }
        }

        foreach (var (contributionId, index) in stepIndexes)
        {
            var contribution = contributionsById[contributionId].Contribution;
            foreach (var before in contribution.BeforeContributions)
            {
                if (stepIndexes.TryGetValue(before, out var beforeIndex) && index >= beforeIndex)
                    Fail($"Route for backend '{backend.Value}' violates before-order '{contributionId.Value}' -> '{before.Value}'.");
            }
            foreach (var after in contribution.AfterContributions)
            {
                if (stepIndexes.TryGetValue(after, out var afterIndex) && index <= afterIndex)
                    Fail($"Route for backend '{backend.Value}' violates after-order '{after.Value}' -> '{contributionId.Value}'.");
            }
        }
    }

    private static void ValidateRegistrationIdentityConsistency(LanguagePlan plan)
    {
        var resolvedItems = plan.Features
            .Select(static feature => (feature.PackageId, feature.PackageIdentity))
            .Concat(plan.Contributions.Select(static contribution => (contribution.PackageId, contribution.PackageIdentity)));

        foreach (var package in resolvedItems.GroupBy(static item => item.PackageId))
        {
            var identities = package.Select(static item => item.PackageIdentity)
                .Distinct(ReferenceEqualityComparer.Instance)
                .ToArray();
            if (identities.Length != 1)
                Fail($"Package '{package.Key.Value}' is represented by multiple registry-issued identities in one language plan.");
        }
    }

    private static void RequireUnique(IEnumerable<string> values, string kind)
    {
        var duplicate = values.GroupBy(static x => x, StringComparer.Ordinal).FirstOrDefault(static x => x.Count() > 1);
        if (duplicate != null)
            Fail($"The language plan contains duplicate {kind} ID '{duplicate.Key}'.");
    }

    private static void ValidateDigests(IEnumerable<(string Owner, string Digest)> values)
    {
        foreach (var (owner, digest) in values)
        {
            if (digest.Length != 64 || digest.Any(static character => !Uri.IsHexDigit(character)))
                Fail($"Component '{owner}' has an invalid SHA-256 manifest digest.");
        }
    }

    [DoesNotReturn]
    private static void Fail(string message) => throw new LanguagePlanVerificationException(message);
}
