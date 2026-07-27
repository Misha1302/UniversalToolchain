using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistModuleSelection
{
    private static readonly LanguagePackageDescriptor CanonicalDescriptor = new WistLanguageFeaturePackage().Descriptor;
    private static readonly string CanonicalManifestSha256 = LanguageFeatureManifestSerializer.ComputeSha256(CanonicalDescriptor);
    private static readonly IReadOnlySet<LanguageContributionId> CanonicalContributionIds = CanonicalDescriptor.Contributions.Select(static x => x.Id).ToHashSet();
    private static readonly IReadOnlySet<LanguageFeatureId> CanonicalFeatureIds = CanonicalDescriptor.Features.Select(static x => x.Id).ToHashSet();

    public static IReadOnlyList<string> GetModuleAliases(LanguagePlan plan) =>
        GetAliases(plan, LanguageSlots.FrontendSyntax, "wist.moduleAlias", "module");

    public static IReadOnlyList<string> GetOptimizerAliases(LanguagePlan plan) =>
        GetAliases(plan, LanguageSlots.Optimizers, "wist.optimizerAlias", "optimizer");

    public static void ValidateCanonicalPackageProvenance(LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        LanguagePlanVerifier.Verify(plan);

        var relevant = plan.Routes.Values
            .SelectMany(static route => route.Steps)
            .Select(static step => step.ContributionId)
            .Concat(plan.Contributions
                .Where(static x => x.Contribution.Slot is var slot &&
                    (slot == LanguageSlots.FrontendSyntax || slot == LanguageSlots.Optimizers))
                .Select(static x => x.Contribution.Id))
            .ToHashSet();
        if (plan.RuntimeProviderContribution != null)
            relevant.Add(plan.RuntimeProviderContribution.Contribution.Id);

        foreach (var contribution in plan.Contributions.Where(x => relevant.Contains(x.Contribution.Id)))
        {
            if (!CanonicalContributionIds.Contains(contribution.Contribution.Id) ||
                !contribution.PackageIdentity.IsImplementation(typeof(WistLanguageFeaturePackage)) ||
                contribution.PackageId != WistLanguageFeaturePackage.PackageId ||
                contribution.PackageVersion != WistLanguageFeaturePackage.PackageVersion ||
                !StringComparer.Ordinal.Equals(contribution.ManifestSha256, CanonicalManifestSha256))
            {
                throw new InvalidOperationException(
                    $"Runtime-relevant contribution '{contribution.Contribution.Id.Value}' is not owned by the canonical Wist package.");
            }
        }

        foreach (var feature in plan.Features.Where(x => CanonicalFeatureIds.Contains(x.Feature.Id)))
        {
            if (!feature.PackageIdentity.IsImplementation(typeof(WistLanguageFeaturePackage)) ||
                feature.PackageId != WistLanguageFeaturePackage.PackageId ||
                feature.PackageVersion != WistLanguageFeaturePackage.PackageVersion ||
                !StringComparer.Ordinal.Equals(feature.ManifestSha256, CanonicalManifestSha256))
            {
                throw new InvalidOperationException($"Feature '{feature.Feature.Id.Value}' is not owned by the canonical Wist package.");
            }
        }
    }

    public static IReadOnlySet<string> GetExpectedRuntimeBackendAliases(LanguagePlan plan) =>
        plan.Definition.Backends.Select(static backend => backend.Value switch
            {
                "cil" => "cil",
                "interpreter" => "interpreter",
                _ => throw new InvalidOperationException($"Unsupported Wist backend '{backend.Value}'.")
            })
            .ToHashSet(StringComparer.Ordinal);

    private static IReadOnlyList<string> GetAliases(
        LanguagePlan plan,
        LanguageSlotId slot,
        string metadataKey,
        string kind)
    {
        ValidateCanonicalPackageProvenance(plan);
        return plan.Contributions
            .Where(x => x.Contribution.Slot == slot)
            .Select(x => x.Contribution.Metadata.TryGetValue(metadataKey, out var alias) && !string.IsNullOrWhiteSpace(alias)
                ? alias
                : throw new InvalidOperationException(
                    $"Wist runtime cannot activate {kind} contribution '{x.Contribution.Id.Value}' without '{metadataKey}'."))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static alias => alias, StringComparer.Ordinal)
            .ToArray();
    }
}
