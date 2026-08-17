using BasicCore.Binding;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistModulePhaseSlots
{
    public static LanguageSlotId Semantics { get; } = new("wist.semantics.features");
    public static LanguageSlotId Lowering { get; } = new("wist.lowering.features");
}

/// <summary>
/// Derives Wist phase ownership from executable phase-specific implementations instead of manual role flags.
/// </summary>
internal static class WistModulePhaseOwnership
{
    private const string ModulePrefix = "wist.module.";

    private static readonly IReadOnlyDictionary<LanguageContributionId, WistRuntimeComponentDescriptor> ModulesBySyntaxId =
        WistRuntimeComponentCatalog.Modules.ToDictionary(static component => component.ContributionId);

    private static readonly IReadOnlyDictionary<LanguageContributionId, WistRuntimeComponentDescriptor> ModulesBySemanticId =
        WistRuntimeComponentCatalog.Modules
            .Where(static component => component.SemanticBindingRulesFactory != null)
            .ToDictionary(static component => SemanticContributionId(component.ContributionId));

    private static readonly IReadOnlyDictionary<LanguageContributionId, WistRuntimeComponentDescriptor> ModulesByLoweringId =
        WistRuntimeComponentCatalog.Modules
            .Where(static component => WistSemanticBytecodeLowerer.SupportsModuleContribution(component.ContributionId))
            .ToDictionary(static component => LoweringContributionId(component.ContributionId));

    public static IReadOnlyList<LanguageContributionId> ExpandFeatureContributions(
        IEnumerable<LanguageContributionId> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        var expanded = new List<LanguageContributionId>();
        foreach (var contribution in contributions)
        {
            expanded.Add(contribution);
            if (!ModulesBySyntaxId.TryGetValue(contribution, out var component))
                continue;
            if (component.SemanticBindingRulesFactory != null)
                expanded.Add(SemanticContributionId(contribution));
            if (WistSemanticBytecodeLowerer.SupportsModuleContribution(contribution))
                expanded.Add(LoweringContributionId(contribution));
        }
        return expanded.Distinct().ToArray();
    }

    public static IEnumerable<LanguageContributionDescriptor> CreatePhaseContributions(
        WistRuntimeComponentDescriptor component,
        IReadOnlyList<BackendId> supportedBackends)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(supportedBackends);

        var ownsSemantics = component.SemanticBindingRulesFactory != null;
        if (ownsSemantics)
        {
            yield return new LanguageContributionDescriptor(
                SemanticContributionId(component.ContributionId),
                WistModulePhaseSlots.Semantics,
                requiresContributions: [component.ContributionId],
                requiresCapabilities: [new LanguageCapabilityId("frontend:wist")],
                supportedBackends: supportedBackends,
                order: component.Order,
                metadata: PhaseMetadata(component, "semantics"));
        }

        if (WistSemanticBytecodeLowerer.SupportsModuleContribution(component.ContributionId))
        {
            var requires = ownsSemantics
                ? new[] { SemanticContributionId(component.ContributionId) }
                : new[] { component.ContributionId };
            yield return new LanguageContributionDescriptor(
                LoweringContributionId(component.ContributionId),
                WistModulePhaseSlots.Lowering,
                requiresContributions: requires,
                requiresCapabilities: [new LanguageCapabilityId("semantics:wist")],
                supportedBackends: supportedBackends,
                order: component.Order,
                metadata: PhaseMetadata(component, "lowering"));
        }
    }

    public static bool TryGetSemanticComponent(
        LanguageContributionId contributionId,
        out WistRuntimeComponentDescriptor? component) =>
        ModulesBySemanticId.TryGetValue(contributionId, out component);

    public static bool TryGetLoweringComponent(
        LanguageContributionId contributionId,
        out WistRuntimeComponentDescriptor? component) =>
        ModulesByLoweringId.TryGetValue(contributionId, out component);

    public static LanguageContributionId SemanticContributionId(LanguageContributionId syntaxContributionId) =>
        PhaseContributionId(syntaxContributionId, "semantics");

    public static LanguageContributionId LoweringContributionId(LanguageContributionId syntaxContributionId) =>
        PhaseContributionId(syntaxContributionId, "lowering");

    private static LanguageContributionId PhaseContributionId(LanguageContributionId syntaxContributionId, string phase)
    {
        if (!syntaxContributionId.Value.StartsWith(ModulePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Wist module contribution '{syntaxContributionId.Value}' does not use the canonical '{ModulePrefix}' identity prefix.");
        }
        return new LanguageContributionId($"wist.{phase}.module.{syntaxContributionId.Value[ModulePrefix.Length..]}");
    }

    private static IReadOnlyDictionary<string, string> PhaseMetadata(
        WistRuntimeComponentDescriptor component,
        string phase) =>
        new Dictionary<string, string>
        {
            ["wist.moduleAlias"] = component.Alias,
            ["wist.phase"] = phase,
            ["wist.owner"] = "language-plan"
        };
}

internal static class WistPlannedSemanticBindingActivation
{
    public static IReadOnlyList<IAstBindingRule> CreateOrderedRules(
        WistLanguageFeaturePackage package,
        LanguagePlan plan,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(services);

        var selected = plan.Contributions
            .Where(contribution => contribution.Contribution.Slot == WistModulePhaseSlots.Semantics)
            .ToArray();
        var rules = new List<IAstBindingRule>();

        foreach (var contribution in selected)
        {
            if (contribution.Contribution.Id == WistContributionIds.CanonicalAddSemantics)
            {
                ValidatePackageBinding(package, contribution);
                continue;
            }

            if (!WistModulePhaseOwnership.TryGetSemanticComponent(contribution.Contribution.Id, out var component)
                || component == null)
            {
                throw new InvalidOperationException(
                    $"Planned Wist semantics contribution '{contribution.Contribution.Id.Value}' " +
                    "has no phase-specific binding implementation.");
            }

            ValidatePackageBinding(package, contribution);
            var factory = component.SemanticBindingRulesFactory ?? throw new InvalidOperationException(
                $"Wist semantics contribution '{contribution.Contribution.Id.Value}' has no binding-rule factory.");
            var created = factory(services) ?? throw new InvalidOperationException(
                $"Wist semantics contribution '{contribution.Contribution.Id.Value}' returned null binding rules.");
            if (created.Any(static rule => rule == null))
            {
                throw new InvalidOperationException(
                    $"Wist semantics contribution '{contribution.Contribution.Id.Value}' returned a null binding rule.");
            }
            rules.AddRange(created);
        }

        return rules;
    }

    public static void ValidatePlannedLowering(
        WistLanguageFeaturePackage package,
        LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(plan);

        foreach (var contribution in plan.Contributions
                     .Where(contribution => contribution.Contribution.Slot == WistModulePhaseSlots.Lowering))
        {
            ValidatePackageBinding(package, contribution);
            if (contribution.Contribution.Id == WistContributionIds.CanonicalAddLowering)
                continue;
            if (!WistModulePhaseOwnership.TryGetLoweringComponent(contribution.Contribution.Id, out var component)
                || component == null
                || !WistSemanticBytecodeLowerer.SupportsModuleContribution(component.ContributionId))
            {
                throw new InvalidOperationException(
                    $"Planned Wist lowering contribution '{contribution.Contribution.Id.Value}' " +
                    "has no native semantic-to-bytecode implementation.");
            }
        }
    }

    private static void ValidatePackageBinding(
        WistLanguageFeaturePackage package,
        ResolvedLanguageContribution contribution)
    {
        var descriptor = package.Descriptor;
        if (descriptor.Id != contribution.PackageId || descriptor.Version != contribution.PackageVersion)
        {
            throw new InvalidOperationException(
                $"Planned Wist phase contribution '{contribution.Contribution.Id.Value}' is not owned by the exact Wist package version.");
        }

        var manifest = LanguageFeatureManifestSerializer.ComputeSha256(descriptor);
        if (!StringComparer.Ordinal.Equals(manifest, contribution.ManifestSha256))
        {
            throw new InvalidOperationException(
                $"Wist package manifest does not match the exact package captured by LanguagePlan for '{contribution.Contribution.Id.Value}'.");
        }

        if (!contribution.PackageIdentity.IsImplementationInstance(package))
        {
            throw new InvalidOperationException(
                $"Wist phase contribution '{contribution.Contribution.Id.Value}' is not bound to the exact package implementation registered during planning.");
        }

        if (!descriptor.Contributions.Any(item => item.Id == contribution.Contribution.Id))
        {
            throw new InvalidOperationException(
                $"Wist package does not declare planned phase contribution '{contribution.Contribution.Id.Value}'.");
        }
    }
}
