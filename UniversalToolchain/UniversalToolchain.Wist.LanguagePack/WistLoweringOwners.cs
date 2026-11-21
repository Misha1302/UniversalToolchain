using System.Collections.Frozen;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// The single declarative source of which Wist contributions have a native semantic-to-bytecode
/// implementation (UT-A046).
/// <para>
/// This was a thirteen-term <c>||</c> chain inside <see cref="WistSemanticBytecodeLowerer"/>. That
/// shape has two costs: adding a module means editing a predicate in the lowering path, and the
/// answer to "does this module lower?" existed in three places that could drift (the predicate, the
/// phase-ownership table and the component catalog). Here it is one list of data.
/// </para>
/// <para>
/// Everything else derives from it in one direction only:
/// </para>
/// <code>
/// WistLoweringOwners (data)
///   -> WistModuleDescriptors (module identity + phases)
///   -> LanguageModuleRegistry (ownership index)
///   -> WistSemanticBytecodeLowerer (dispatch and RequireModule)
/// </code>
/// <para>
/// There is deliberately no path back: the lowerer never decides what the data says, so a dispatch
/// question and an ownership question cannot disagree.
/// </para>
/// </summary>
internal static class WistLoweringOwners
{
    /// <summary>
    /// Contributions with a native lowering implementation, in canonical identifier order.
    /// </summary>
    public static IReadOnlyList<LanguageContributionId> Contributions { get; } =
    [
        WistContributionIds.ArithmeticModule,
        WistContributionIds.BooleanLogicModule,
        WistContributionIds.ComparisonsModule,
        WistContributionIds.ConditionalControlFlowModule,
        WistContributionIds.CSharpInteropModule,
        WistContributionIds.EqualityModule,
        WistContributionIds.FunctionCallsModule,
        WistContributionIds.LabelsModule,
        WistContributionIds.LoopsModule,
        WistContributionIds.NativeTypesModule,
        WistContributionIds.NumbersModule,
        WistContributionIds.ScopesModule,
        WistContributionIds.VariablesModule
    ];

    // Frozen rather than a mutable HashSet: the membership answer must not be changeable at runtime,
    // and an immutable set cannot be repopulated by a caller. The architecture guard for process-wide
    // mutable static collections exists precisely to force this choice.
    private static readonly FrozenSet<LanguageContributionId> Set = Contributions.ToFrozenSet();

    private static readonly Lazy<LanguageModuleRegistry> Registry = new(() =>
        new LanguageModuleRegistry().AddRange(WistModuleDescriptors.Create()));

    /// <summary>
    /// Ownership index built from the module descriptors. Both the lowering dispatch and diagnostics
    /// read this one instance, so they cannot answer differently.
    /// </summary>
    public static LanguageModuleRegistry ModuleRegistry => Registry.Value;

    /// <summary>
    /// True when <paramref name="contributionId"/> has a native lowering implementation.
    /// </summary>
    public static bool OwnsLowering(LanguageContributionId contributionId) =>
        Set.Contains(contributionId);

    /// <summary>
    /// Resolves the module that owns the lowering phase of <paramref name="contributionId"/>.
    /// Returns false when the contribution is not lowered at all, which is not an error by itself.
    /// </summary>
    public static bool TryGetLoweringOwner(
        LanguageContributionId contributionId,
        out LanguageModuleDescriptor? descriptor) =>
        ModuleRegistry.TryGetOwner(LanguageModulePhase.Lowering, contributionId, out descriptor);
}
