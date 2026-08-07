using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistDialectPlanFactory
{
    public static DialectDefinitionSlice Create(LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        WistModuleSelection.ValidateCanonicalPackageProvenance(plan);

        var trusted = plan.Features.Any(static feature => feature.Feature.Id == WistInternalFeatureIds.TrustedSecurity);
        var restricted = plan.Features.Any(static feature => feature.Feature.Id == WistInternalFeatureIds.RestrictedSecurity);
        if (trusted && restricted)
            throw new InvalidOperationException("Wist language plan contains conflicting typed security profiles.");
        var security = trusted
            ? DialectSecurityProfile.Trusted
            : restricted
                ? DialectSecurityProfile.Restricted
                : (DialectSecurityProfile?)null;

        var capabilities = new List<DialectCapabilityDirective>();
        if (plan.Features.Any(static feature => feature.Feature.Id == WistInternalFeatureIds.CompositionRestricted))
            capabilities.Add(new DialectCapabilityDirective("composition-restricted", true));
        if (plan.Definition.RuntimePolicy.AllowHostInterop &&
            plan.Contributions.Any(static contribution => contribution.Contribution.Id == WistContributionIds.CSharpInteropModule))
        {
            capabilities.Add(new DialectCapabilityDirective("unsafe-interop", true));
        }

        return new DialectDefinitionSlice(
            plan.Definition.Id.Value,
            WistModuleSelection.GetModuleAliases(plan),
            [],
            [],
            plan.Definition.Backends.Select(static backend =>
                new DialectBackendDirective(new DialectBackendId(backend.Value), enabled: true)),
            [],
            WistModuleSelection.GetOptimizerAliases(plan).Select(static alias =>
                new DialectOptimizerDirective(alias, enabled: true, DialectBackendSelector.Any)),
            security,
            capabilities,
            version: plan.Definition.Version.Value);
    }
}
