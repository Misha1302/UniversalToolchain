using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistModuleSelection
{
    private static readonly LanguagePackageDescriptor CanonicalDescriptor =
        new WistLanguageFeaturePackage().Descriptor;

    private static readonly string CanonicalManifestSha256 =
        LanguageFeatureManifestSerializer.ComputeSha256(CanonicalDescriptor);

    private static readonly IReadOnlySet<LanguageContributionId> CanonicalContributionIds =
        CanonicalDescriptor.Contributions.Select(static contribution => contribution.Id).ToHashSet();

    private static readonly IReadOnlySet<LanguageFeatureId> CanonicalFeatureIds =
        CanonicalDescriptor.Features.Select(static feature => feature.Id).ToHashSet();

    private static readonly IReadOnlyDictionary<LanguageContributionId, string> ModuleAliases =
        new Dictionary<LanguageContributionId, string>
        {
            [WistContributionIds.WhitespacesModule] = "Whitespaces",
            [WistContributionIds.ScopesModule] = "Scopes",
            [WistContributionIds.NumbersModule] = "Numbers",
            [WistContributionIds.ArithmeticModule] = "Arithmetic",
            [WistContributionIds.IdentifiersModule] = "Identifier",
            [WistContributionIds.VariablesModule] = "Variables",
            [WistContributionIds.ComparisonsModule] = "ComparisonConditions",
            [WistContributionIds.BooleanLogicModule] = "BooleanConditions",
            [WistContributionIds.ConditionalControlFlowModule] = "Conditions"
        };

    public static IReadOnlyList<string> GetModuleAliases(LanguagePlan plan)
    {
        ValidateCanonicalPackageProvenance(plan);
        return plan.Contributions
            .Where(static contribution => contribution.Contribution.Slot == LanguageSlots.FrontendSyntax)
            .Select(contribution => ModuleAliases.TryGetValue(contribution.Contribution.Id, out var alias)
                ? alias
                : throw new InvalidOperationException(
                    $"Wist runtime cannot activate unknown frontend contribution '{contribution.Contribution.Id.Value}'."))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static alias => alias, StringComparer.Ordinal)
            .ToArray();
    }

    public static void ValidateCanonicalPackageProvenance(LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        LanguagePlanVerifier.Verify(plan);

        var runtimeRelevantContributionIds = plan.Routes.Values
            .SelectMany(static route => route.Steps)
            .Select(static step => step.ContributionId)
            .Concat(plan.Contributions
                .Where(static contribution => contribution.Contribution.Slot == LanguageSlots.FrontendSyntax)
                .Select(static contribution => contribution.Contribution.Id))
            .ToHashSet();
        if (plan.RuntimeProviderContribution != null)
            runtimeRelevantContributionIds.Add(plan.RuntimeProviderContribution.Contribution.Id);

        foreach (var contribution in plan.Contributions
                     .Where(contribution => runtimeRelevantContributionIds.Contains(contribution.Contribution.Id)))
        {
            if (!CanonicalContributionIds.Contains(contribution.Contribution.Id) ||
                !contribution.PackageIdentity.IsImplementation(typeof(WistLanguageFeaturePackage)) ||
                contribution.PackageId != WistLanguageFeaturePackage.PackageId ||
                contribution.PackageVersion != WistLanguageFeaturePackage.PackageVersion ||
                !StringComparer.Ordinal.Equals(contribution.ManifestSha256, CanonicalManifestSha256))
            {
                throw new InvalidOperationException(
                    $"Runtime-relevant contribution '{contribution.Contribution.Id.Value}' is not owned by the canonical " +
                    $"Wist package '{WistLanguageFeaturePackage.PackageId.Value}' " +
                    $"version '{WistLanguageFeaturePackage.PackageVersion.Value}' with the expected manifest digest.");
            }
        }

        foreach (var feature in plan.Features.Where(feature => CanonicalFeatureIds.Contains(feature.Feature.Id)))
        {
            if (!feature.PackageIdentity.IsImplementation(typeof(WistLanguageFeaturePackage)) ||
                feature.PackageId != WistLanguageFeaturePackage.PackageId ||
                feature.PackageVersion != WistLanguageFeaturePackage.PackageVersion ||
                !StringComparer.Ordinal.Equals(feature.ManifestSha256, CanonicalManifestSha256))
            {
                throw new InvalidOperationException(
                    $"Feature '{feature.Feature.Id.Value}' is not owned by the canonical Wist package provenance.");
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
}
