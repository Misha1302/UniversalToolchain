using BasicCore.Contracts;
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
/// Declares which phase responsibilities are actually implemented by the historical combined Wist modules.
/// The mapping is package metadata only: runtime stages consume the explicit phase contributions captured by
/// <see cref="LanguagePlan"/> and never infer lowering from a syntax artifact or syntax contribution identity.
/// </summary>
internal static class WistModulePhaseOwnership
{
    private const string ModulePrefix = "wist.module.";

    private static readonly IReadOnlySet<LanguageContributionId> SemanticModules =
        new HashSet<LanguageContributionId>
        {
            WistContributionIds.VariablesModule
        };

    private static readonly IReadOnlySet<LanguageContributionId> LoweringModules =
        new HashSet<LanguageContributionId>
        {
            WistContributionIds.ArithmeticModule,
            WistContributionIds.BooleanLogicModule,
            WistContributionIds.CSharpInteropModule,
            WistContributionIds.ComparisonsModule,
            WistContributionIds.ConditionalControlFlowModule,
            WistContributionIds.EqualityModule,
            WistContributionIds.FunctionCallsModule,
            WistContributionIds.LabelsModule,
            WistContributionIds.LoopsModule,
            WistContributionIds.NativeTypesModule,
            WistContributionIds.NumbersModule,
            WistContributionIds.ScopesModule,
            WistContributionIds.VariablesModule
        };

    private static readonly IReadOnlyDictionary<LanguageContributionId, WistRuntimeComponentDescriptor> ModulesBySyntaxId =
        WistRuntimeComponentCatalog.Modules.ToDictionary(static component => component.ContributionId);

    private static readonly IReadOnlyDictionary<LanguageContributionId, WistRuntimeComponentDescriptor> ModulesBySemanticId =
        WistRuntimeComponentCatalog.Modules
            .Where(component => SemanticModules.Contains(component.ContributionId))
            .ToDictionary(component => SemanticContributionId(component.ContributionId));

    private static readonly IReadOnlyDictionary<LanguageContributionId, WistRuntimeComponentDescriptor> ModulesByLoweringId =
        WistRuntimeComponentCatalog.Modules
            .Where(component => LoweringModules.Contains(component.ContributionId))
            .ToDictionary(component => LoweringContributionId(component.ContributionId));

    public static IReadOnlyList<LanguageContributionId> ExpandFeatureContributions(
        IEnumerable<LanguageContributionId> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        var expanded = new List<LanguageContributionId>();
        foreach (var contribution in contributions)
        {
            expanded.Add(contribution);
            if (!ModulesBySyntaxId.ContainsKey(contribution))
                continue;
            if (SemanticModules.Contains(contribution))
                expanded.Add(SemanticContributionId(contribution));
            if (LoweringModules.Contains(contribution))
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

        if (SemanticModules.Contains(component.ContributionId))
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

        if (LoweringModules.Contains(component.ContributionId))
        {
            var requires = SemanticModules.Contains(component.ContributionId)
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

internal enum WistPlannedModulePhase
{
    Semantics,
    Lowering
}

internal static class WistPlannedModulePhaseActivation
{
    public static IReadOnlyList<Func<IFrontendCoreModule>> CreateOrderedFactories(
        WistLanguageFeaturePackage package,
        LanguagePlan plan,
        IServiceProvider services,
        WistPlannedModulePhase phase)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(services);

        var slot = phase == WistPlannedModulePhase.Semantics
            ? WistModulePhaseSlots.Semantics
            : WistModulePhaseSlots.Lowering;
        var selected = plan.Contributions
            .Where(contribution => contribution.Contribution.Slot == slot)
            .ToArray();
        var factories = new List<Func<IFrontendCoreModule>>(selected.Length);

        foreach (var contribution in selected)
        {
            if (IsCanonicalPhaseContribution(contribution.Contribution.Id, phase))
            {
                ValidatePackageBinding(package, contribution);
                continue;
            }

            var found = phase == WistPlannedModulePhase.Semantics
                ? WistModulePhaseOwnership.TryGetSemanticComponent(contribution.Contribution.Id, out var component)
                : WistModulePhaseOwnership.TryGetLoweringComponent(contribution.Contribution.Id, out component);
            if (!found || component == null)
            {
                throw new InvalidOperationException(
                    $"Planned Wist {phase.ToString().ToLowerInvariant()} contribution '{contribution.Contribution.Id.Value}' " +
                    "has no exact phase-owned module implementation.");
            }

            ValidatePackageBinding(package, contribution);
            var moduleFactory = component.ModuleFactory ?? throw new InvalidOperationException(
                $"Canonical Wist module '{component.ContributionId.Value}' has no activation factory.");
            factories.Add(() =>
            {
                var instance = moduleFactory(services);
                if (instance is not IFrontendCoreModule module)
                {
                    throw new InvalidOperationException(
                        $"Wist {phase.ToString().ToLowerInvariant()} module factory '{contribution.Contribution.Id.Value}' returned " +
                        $"'{instance.GetType().FullName}', which does not implement '{typeof(IFrontendCoreModule).FullName}'.");
                }
                return module;
            });
        }

        return factories;
    }

    private static bool IsCanonicalPhaseContribution(LanguageContributionId contributionId, WistPlannedModulePhase phase) =>
        phase == WistPlannedModulePhase.Semantics
            ? contributionId == WistContributionIds.CanonicalAddSemantics
            : contributionId == WistContributionIds.CanonicalAddLowering;

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
