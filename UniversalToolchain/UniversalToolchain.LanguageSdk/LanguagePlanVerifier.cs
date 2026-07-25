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

        var contributionIds = plan.Contributions.Select(static x => x.Contribution.Id).ToHashSet();
        if (plan.RuntimeProviderContribution != null)
        {
            var selectedRuntimeContribution = plan.Contributions.SingleOrDefault(
                contribution => contribution.Contribution.Id == plan.RuntimeProviderContribution.Contribution.Id);
            if (selectedRuntimeContribution == null)
                Fail("The runtime-provider contribution is not part of the selected contribution set.");
            if (!ReferenceEquals(selectedRuntimeContribution, plan.RuntimeProviderContribution))
                Fail("The runtime-provider contribution does not reference the verified selected contribution instance.");
        }

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
                if (route.Backend != backend)
                    Fail($"Route dictionary entry for backend '{backend.Value}' contains a mismatched route.");
                if (!LanguageArtifactRoute.ContractsConnect(plan.Definition.EntryArtifact, route.SourceContract))
                    Fail($"Route for backend '{backend.Value}' does not start at the language entry artifact.");
                if (route.Steps.Count == 0)
                    Fail($"Executable route for backend '{backend.Value}' must not be empty.");
                foreach (var step in route.Steps)
                {
                    if (!contributionIds.Contains(step.ContributionId))
                        Fail($"Route for backend '{backend.Value}' references unselected contribution '{step.ContributionId.Value}'.");
                }
            }

            var extraRoutes = plan.Routes.Keys.Except(plan.Definition.Backends).ToArray();
            if (extraRoutes.Length != 0)
                Fail($"The plan contains routes for unselected backends: {string.Join(", ", extraRoutes.Select(static x => x.Value))}.");
        }
        else if (plan.Routes.Count != 0 || plan.RuntimeProviderContribution != null)
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
            {
                Fail($"Package '{package.Key.Value}' is represented by multiple registry-issued identities in one language plan.");
            }
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
