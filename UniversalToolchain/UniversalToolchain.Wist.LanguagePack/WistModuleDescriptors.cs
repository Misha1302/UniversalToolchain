using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// Wist's own module declarations (UT-A005 / ADR-008).
/// <para>
/// Each entry groups the contributions one mechanism owns and states the phases it takes part in.
/// The phases are derived from what the component actually provides — a semantic binding factory, a
/// lowering implementation — rather than from a hand-maintained flag, so a component cannot claim a
/// phase it does not implement.
/// </para>
/// <para>
/// These declarations are consumed only by lowering dispatch and by diagnostics. They are not
/// planned, so they cannot change a plan hash; <c>ModuleDescriptors_CannotChangeAnyWistPlanHash</c>
/// pins that.
/// </para>
/// </summary>
internal static class WistModuleDescriptors
{
    private const string WistPrefix = "wist.";

    public static IReadOnlyList<LanguageModuleDescriptor> Create()
    {
        var descriptors = new List<LanguageModuleDescriptor>();
        foreach (var component in WistRuntimeComponentCatalog.Modules)
        {
            var phases = new List<LanguageModulePhase> { LanguageModulePhase.Frontend };
            if (component.SemanticBindingRulesFactory != null)
                phases.Add(LanguageModulePhase.Binding);
            if (WistSemanticBytecodeLowerer.SupportsModuleContribution(component.ContributionId))
                phases.Add(LanguageModulePhase.Lowering);

            descriptors.Add(new LanguageModuleDescriptor(
                ModuleId(component.ContributionId),
                phases,
                [component.ContributionId],
                metadata: new Dictionary<string, string>
                {
                    ["wist.moduleAlias"] = component.Alias,
                    ["wist.phases"] = string.Join(",", phases.Select(static phase => PhaseName(phase)))
                }));
        }

        // Optimizers are mechanism groupings too, but they only occupy the optimization phase: they
        // consume and produce AIR and never bind or lower the language's own constructs.
        foreach (var component in WistRuntimeComponentCatalog.Optimizers)
        {
            descriptors.Add(new LanguageModuleDescriptor(
                ModuleId(component.ContributionId),
                [LanguageModulePhase.Optimization],
                [component.ContributionId],
                metadata: new Dictionary<string, string>
                {
                    ["wist.moduleAlias"] = component.Alias,
                    ["wist.phases"] = PhaseName(LanguageModulePhase.Optimization)
                }));
        }

        return descriptors;
    }

    /// <summary>
    /// The module identity keeps the catalog's own namespace segment (<c>module</c>, <c>optimizer</c>)
    /// so that a module and an optimizer that share a short name never collide.
    /// </summary>
    private static LanguageModuleId ModuleId(LanguageContributionId contributionId)
    {
        if (!contributionId.Value.StartsWith(WistPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Wist contribution '{contributionId.Value}' does not use the canonical '{WistPrefix}' identity prefix.");
        }

        return new LanguageModuleId(contributionId.Value[WistPrefix.Length..]);
    }

    private static string PhaseName(LanguageModulePhase phase) => phase switch
    {
        LanguageModulePhase.Frontend => "frontend",
        LanguageModulePhase.Binding => "binding",
        LanguageModulePhase.Lowering => "lowering",
        LanguageModulePhase.Optimization => "optimization",
        LanguageModulePhase.Emission => "emission",
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown module phase.")
    };
}
