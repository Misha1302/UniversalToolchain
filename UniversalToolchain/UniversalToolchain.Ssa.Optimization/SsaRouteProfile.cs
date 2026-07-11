using System.Collections.ObjectModel;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Emission;
using UniversalToolchain.Ssa.Lowering;

namespace UniversalToolchain.Ssa.Optimization;

public enum SsaDiagnosticMode
{
    Default,
    Verbose
}

public interface ISsaSemanticExtensionPack
{
    string Id { get; }

    SemanticDescriptorSet SemanticDescriptors { get; }

    AirIntrinsicDescriptorSet AirIntrinsics { get; }

    IAirIntrinsicDescriptorResolver AirIntrinsicResolver { get; }

    IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; }

    SsaCallableLoweringTargetSet AirLoweringTargets { get; }

    bool EnablesManagedCallables { get; }

    IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses();
}

public sealed class SsaRouteProfile
{
    private readonly ReadOnlyCollection<IIrOptimizationPass> _optimizationPasses;

    public SsaRouteProfile(
        SsaRoutePolicy policy,
        IEnumerable<ISsaSemanticExtensionPack>? extensionPacks = null,
        CapabilitySet? targetCapabilities = null,
        SsaDiagnosticMode diagnostics = SsaDiagnosticMode.Default,
        string id = SsaPreviewRouteProfiles.ProfileId)
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "SSA route policy is not defined.");
        if (!Enum.IsDefined(diagnostics))
            throw new ArgumentOutOfRangeException(nameof(diagnostics), diagnostics, "SSA diagnostic mode is not defined.");
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("SSA route profile identifier must not be empty.", nameof(id));

        Policy = policy;
        Id = id.Trim();
        var packs = (extensionPacks ?? []).ToArray();
        var duplicatePack = packs
            .GroupBy(static pack => pack.Id, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicatePack is not null)
            throw new ArgumentException($"SSA profile '{Id}' contains duplicate extension pack id '{duplicatePack.Key}'.", nameof(extensionPacks));

        ExtensionPacks = new ReadOnlyCollection<ISsaSemanticExtensionPack>(packs);
        TargetCapabilities = new CapabilitySet((targetCapabilities ?? CapabilitySet.Empty).Values);
        Diagnostics = diagnostics;

        SemanticDescriptors = MergeSemanticDescriptors(ExtensionPacks.Select(static x => x.SemanticDescriptors));
        AirIntrinsics = MergeAirIntrinsics(ExtensionPacks.Select(static x => x.AirIntrinsics));
        AirIntrinsicResolver = new AirIntrinsicDescriptorResolverSet(
            ExtensionPacks.Select(static x => x.AirIntrinsicResolver));
        AirIntrinsicCallables = MergeAirIntrinsicCallables(ExtensionPacks);
        AirLoweringTargets = MergeAirLoweringTargets(ExtensionPacks.Select(static x => x.AirLoweringTargets));
        EnablesManagedCallables = ExtensionPacks.Any(static x => x.EnablesManagedCallables);

        var passes = ExtensionPacks.SelectMany(static pack => pack.CreateOptimizationPasses()).ToArray();
        var duplicatePass = passes
            .GroupBy(static pass => pass.Id)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicatePass is not null)
            throw new ArgumentException($"SSA profile '{Id}' contains duplicate optimizer pass id '{duplicatePass.Key}'.", nameof(extensionPacks));

        _optimizationPasses = new ReadOnlyCollection<IIrOptimizationPass>(passes);
    }

    public string Id { get; }

    public SsaRoutePolicy Policy { get; }

    public IReadOnlyList<ISsaSemanticExtensionPack> ExtensionPacks { get; }

    public CapabilitySet TargetCapabilities { get; }

    public SsaDiagnosticMode Diagnostics { get; }

    public SemanticDescriptorSet SemanticDescriptors { get; }

    public AirIntrinsicDescriptorSet AirIntrinsics { get; }

    public IAirIntrinsicDescriptorResolver AirIntrinsicResolver { get; }

    public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; }

    public SsaCallableLoweringTargetSet AirLoweringTargets { get; }

    public bool EnablesManagedCallables { get; }

    public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() => _optimizationPasses;

    private static SemanticDescriptorSet MergeSemanticDescriptors(IEnumerable<SemanticDescriptorSet> descriptorSets)
    {
        var sets = descriptorSets.ToArray();
        var types = MergeById(
            sets.SelectMany(static x => x.Types),
            static x => x.Id,
            SemanticTypeFingerprint,
            "semantic type");
        var callables = MergeById(
            sets.SelectMany(static x => x.Callables),
            static x => x.Id,
            CallableFingerprint,
            "callable");
        return new SemanticDescriptorSet(types, callables);
    }

    private static AirIntrinsicDescriptorSet MergeAirIntrinsics(IEnumerable<AirIntrinsicDescriptorSet> descriptorSets)
    {
        var values = MergeById(
            descriptorSets.SelectMany(static x => x.Values),
            static x => x.Id,
            AirIntrinsicFingerprint,
            "AIR intrinsic",
            StringComparer.Ordinal);
        return new AirIntrinsicDescriptorSet(values);
    }

    private static IReadOnlyDictionary<string, CallableId> MergeAirIntrinsicCallables(IEnumerable<ISsaSemanticExtensionPack> packs)
    {
        var map = new Dictionary<string, CallableId>(StringComparer.Ordinal);
        foreach (var pair in packs.SelectMany(static x => x.AirIntrinsicCallables))
        {
            if (map.TryGetValue(pair.Key, out var existing) && existing != pair.Value)
                throw new ArgumentException($"SSA profile maps AIR intrinsic '{pair.Key}' to both '{existing}' and '{pair.Value}'.");

            map[pair.Key] = pair.Value;
        }

        return new ReadOnlyDictionary<string, CallableId>(map);
    }

    private static SsaCallableLoweringTargetSet MergeAirLoweringTargets(IEnumerable<SsaCallableLoweringTargetSet> targetSets) =>
        new(targetSets.SelectMany(static x => x.Values));

    private static IReadOnlyList<T> MergeById<T, TId>(
        IEnumerable<T> values,
        Func<T, TId> idSelector,
        Func<T, string> fingerprint,
        string kind,
        IEqualityComparer<TId>? comparer = null)
        where TId : notnull
    {
        var byId = new Dictionary<TId, (T Value, string Fingerprint)>(comparer);
        foreach (var value in values)
        {
            var id = idSelector(value);
            var candidateFingerprint = fingerprint(value);
            if (byId.TryGetValue(id, out var existing))
            {
                if (!string.Equals(existing.Fingerprint, candidateFingerprint, StringComparison.Ordinal))
                    throw new ArgumentException($"SSA extension packs define conflicting {kind} descriptors for '{id}'.");

                continue;
            }

            byId.Add(id, (value, candidateFingerprint));
        }

        return byId.Values
            .Select(static pair => pair.Value)
            .ToArray();
    }

    private static string SemanticTypeFingerprint(SemanticTypeDescriptor descriptor) =>
        $"{descriptor.Traits}|{descriptor.DisplayName}";

    private static string CallableFingerprint(CallableDescriptor descriptor) =>
        string.Join(
            "|",
            string.Join(",", descriptor.Signature.ParameterTypes),
            string.Join(",", descriptor.Signature.ResultTypes),
            string.Join(",", descriptor.Effects.Effects),
            descriptor.Determinism,
            descriptor.AlgebraicTraits,
            descriptor.TrustLevel,
            descriptor.DisplayName,
            string.Join(",", descriptor.RequiredAttributes),
            string.Join(",", descriptor.AllowedAttributes));

    private static string AirIntrinsicFingerprint(AirIntrinsicDescriptor descriptor) =>
        string.Join(
            "|",
            string.Join(",", descriptor.ParameterTypes),
            string.Join(",", descriptor.ResultTypes),
            descriptor.DataOperandCount);
}

public sealed class SsaRouteProfileBuilder
{
    private readonly List<ISsaSemanticExtensionPack> _packs = [];
    private SsaRoutePolicy _policy;
    private CapabilitySet _capabilities = CapabilitySet.Empty;
    private SsaDiagnosticMode _diagnostics = SsaDiagnosticMode.Default;
    private string _id = SsaPreviewRouteProfiles.ProfileId;

    private SsaRouteProfileBuilder(SsaRoutePolicy policy)
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "SSA route policy is not defined.");

        _policy = policy;
    }

    public static SsaRouteProfileBuilder Create(SsaRoutePolicy policy = SsaRoutePolicy.Prefer) =>
        new(policy);

    public SsaRouteProfileBuilder WithId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("SSA route profile identifier must not be empty.", nameof(id));

        _id = id.Trim();
        return this;
    }

    public SsaRouteProfileBuilder WithPolicy(SsaRoutePolicy policy)
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "SSA route policy is not defined.");

        _policy = policy;
        return this;
    }

    public SsaRouteProfileBuilder AddPack(ISsaSemanticExtensionPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        if (string.IsNullOrWhiteSpace(pack.Id))
            throw new ArgumentException("SSA extension pack identifier must not be empty.", nameof(pack));
        if (_packs.Any(x => string.Equals(x.Id, pack.Id, StringComparison.Ordinal)))
            throw new ArgumentException($"SSA profile already contains extension pack '{pack.Id}'.", nameof(pack));

        _packs.Add(pack);
        return this;
    }

    public SsaRouteProfileBuilder RequireTargetCapabilities(CapabilitySet capabilities)
    {
        _capabilities = new CapabilitySet((capabilities ?? throw new ArgumentNullException(nameof(capabilities))).Values);
        return this;
    }

    public SsaRouteProfileBuilder WithDiagnostics(SsaDiagnosticMode diagnostics)
    {
        if (!Enum.IsDefined(diagnostics))
            throw new ArgumentOutOfRangeException(nameof(diagnostics), diagnostics, "SSA diagnostic mode is not defined.");

        _diagnostics = diagnostics;
        return this;
    }

    public SsaRouteProfile Build() =>
        new(_policy, _packs, _capabilities, _diagnostics, _id);
}

public sealed class SsaPreviewArithmeticInt32Pack : ISsaSemanticExtensionPack
{
    public static SsaPreviewArithmeticInt32Pack Instance { get; } = new();

    private SsaPreviewArithmeticInt32Pack()
    {
    }

    public string Id => "PreviewArithmeticInt32";

    public SemanticDescriptorSet SemanticDescriptors => SsaPreviewSemanticDescriptors.ArithmeticInt32;

    public AirIntrinsicDescriptorSet AirIntrinsics => AirCoreIntrinsicDescriptors.ArithmeticInt32;

    public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsics;

    public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
        new ReadOnlyDictionary<string, CallableId>(new Dictionary<string, CallableId>(StringComparer.Ordinal)
        {
            [AirIntrinsicIds.AddInt32Unchecked] = SsaPreviewCallables.AddInt32Unchecked,
            [AirIntrinsicIds.SubtractInt32Unchecked] = SsaPreviewCallables.SubtractInt32Unchecked,
            [AirIntrinsicIds.MultiplyInt32Unchecked] = SsaPreviewCallables.MultiplyInt32Unchecked,
            [AirIntrinsicIds.EqualInt32] = SsaPreviewCallables.EqualInt32
        });

    public SsaCallableLoweringTargetSet AirLoweringTargets =>
        SsaPreviewAirIntrinsicLowerings.ArithmeticInt32.ToTargetSet();

    public bool EnablesManagedCallables => false;

    public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() =>
    [
        new SsaConstantFoldingPass(SemanticDescriptors, new SsaPreviewInt32ConstantEvaluator()),
        new SsaSparseConditionalConstantPropagationPass(SemanticDescriptors, new SsaPreviewInt32ConstantEvaluator()),
        new SsaBranchFoldingAndCleanupPass(),
        new SsaDeadPureInstructionEliminationPass(SsaCoreDescriptors.CoreOperations, SemanticDescriptors)
    ];
}

public sealed class SsaManagedCallablePack : ISsaSemanticExtensionPack
{
    public static SsaManagedCallablePack Instance { get; } = new();

    private SsaManagedCallablePack()
    {
    }

    public string Id => "ManagedCallable";

    // Managed callable descriptors are execution-scoped and are carried by
    // SsaManagedCallableBindingSet. Core preview type descriptors have one
    // canonical owner: SsaPreviewArithmeticInt32Pack.
    public SemanticDescriptorSet SemanticDescriptors => SemanticDescriptorSet.Empty;

    public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

    public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => new AirIntrinsicDescriptorResolverSet(
    [
        AirManagedCallIntrinsicDescriptorResolver.Instance,
        AirExternalLoadIntrinsicDescriptorResolver.Instance
    ]);

    public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
        new ReadOnlyDictionary<string, CallableId>(new Dictionary<string, CallableId>(StringComparer.Ordinal));

    public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

    public bool EnablesManagedCallables => true;

    public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() => [];
}

public static class SsaPreviewRouteProfiles
{
    public const string ProfileId = "preview-int32-managed";

    public static SsaRouteProfile Create(
        SsaRoutePolicy policy = SsaRoutePolicy.Prefer,
        SsaDiagnosticMode diagnostics = SsaDiagnosticMode.Default,
        CapabilitySet? targetCapabilities = null,
        string profileId = ProfileId) =>
        SsaRouteProfileBuilder
            .Create(policy)
            .WithId(profileId)
            .WithDiagnostics(diagnostics)
            .RequireTargetCapabilities(targetCapabilities ?? CapabilitySet.Empty)
            .AddPack(SsaPreviewArithmeticInt32Pack.Instance)
            .AddPack(SsaManagedCallablePack.Instance)
            .Build();
}

public static class SsaRouteFactory
{
    public static AirToSsaConverter CreateLowerer(
        SsaRouteProfile profile,
        IEnumerable<ISsaManagedCallableProjection>? managedCallableProjections = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new AirToSsaConverter(
            new AirControlFlowGraphBuilder(),
            new AirStackAnalyzer(profile.AirIntrinsicResolver),
            new StructuralAirVerifier(
                new AirControlFlowGraphBuilder(),
                new AirStackAnalyzer(profile.AirIntrinsicResolver)),
            new StructuralSsaVerifier(SsaCoreDescriptors.CoreOperations, profile.SemanticDescriptors),
            profile.AirIntrinsics,
            profile.AirIntrinsicCallables,
            profile.SemanticDescriptors,
            profile.EnablesManagedCallables,
            managedCallableProjections);
    }

    public static SsaToAirConverter CreateEmitter(SsaRouteProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.CoreOperations, profile.SemanticDescriptors),
            new StructuralAirVerifier(
                new AirControlFlowGraphBuilder(),
                new AirStackAnalyzer(profile.AirIntrinsicResolver)),
            new SsaCallableLoweringPlanner(
                profile.SemanticDescriptors,
                profile.AirLoweringTargets,
                profile.AirIntrinsics),
            profile.SemanticDescriptors);
    }

    public static SsaOptimizerPipeline CreateOptimizer(SsaRouteProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new SsaOptimizerPipeline(
            profile.CreateOptimizationPasses(),
            SsaCoreDescriptors.CoreOperations,
            profile.SemanticDescriptors);
    }

    public static SsaRoundtripRoute CreateRoundtripRoute(
        SsaRouteProfile profile,
        IEnumerable<ISsaManagedCallableProjection>? managedCallableProjections = null) =>
        new(CreateLowerer(profile, managedCallableProjections), CreateEmitter(profile), profile);
}
