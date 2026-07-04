using System.Collections.ObjectModel;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Emission;
using UniversalToolchain.Ssa.Lowering;

namespace UniversalToolchain.Ssa.Optimization;

public sealed record SsaDiagnosticMode(string Id)
{
    public static SsaDiagnosticMode Default { get; } = new("default");
    public static SsaDiagnosticMode Verbose { get; } = new("verbose");
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
    public SsaRouteProfile(
        SsaRoutePolicy policy,
        IEnumerable<ISsaSemanticExtensionPack>? extensionPacks = null,
        CapabilitySet? targetCapabilities = null,
        SsaDiagnosticMode? diagnostics = null)
    {
        Policy = policy;
        ExtensionPacks = new ReadOnlyCollection<ISsaSemanticExtensionPack>((extensionPacks ?? []).ToArray());
        TargetCapabilities = targetCapabilities ?? CapabilitySet.Empty;
        Diagnostics = diagnostics ?? SsaDiagnosticMode.Default;

        SemanticDescriptors = MergeSemanticDescriptors(ExtensionPacks.Select(static x => x.SemanticDescriptors));
        AirIntrinsics = MergeAirIntrinsics(ExtensionPacks.Select(static x => x.AirIntrinsics));
        AirIntrinsicResolver = new AirIntrinsicDescriptorResolverSet(
            ExtensionPacks.Select(static x => x.AirIntrinsicResolver));
        AirIntrinsicCallables = MergeAirIntrinsicCallables(ExtensionPacks);
        AirLoweringTargets = MergeAirLoweringTargets(ExtensionPacks.Select(static x => x.AirLoweringTargets));
        EnablesManagedCallables = ExtensionPacks.Any(static x => x.EnablesManagedCallables);
    }

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

    public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() =>
        ExtensionPacks.SelectMany(static x => x.CreateOptimizationPasses()).ToArray();

    private static SemanticDescriptorSet MergeSemanticDescriptors(IEnumerable<SemanticDescriptorSet> descriptorSets)
    {
        var sets = descriptorSets.ToArray();
        return new SemanticDescriptorSet(
            sets.SelectMany(static x => x.Types)
                .GroupBy(static x => x.Id)
                .Select(static x => x.First()),
            sets.SelectMany(static x => x.Callables)
                .GroupBy(static x => x.Id)
                .Select(static x => x.First()));
    }

    private static AirIntrinsicDescriptorSet MergeAirIntrinsics(IEnumerable<AirIntrinsicDescriptorSet> descriptorSets) =>
        new(descriptorSets
            .SelectMany(static x => x.Values)
            .GroupBy(static x => x.Id, StringComparer.Ordinal)
            .Select(static x => x.First()));

    private static IReadOnlyDictionary<string, CallableId> MergeAirIntrinsicCallables(IEnumerable<ISsaSemanticExtensionPack> packs)
    {
        var map = new Dictionary<string, CallableId>(StringComparer.Ordinal);
        foreach (var pair in packs.SelectMany(static x => x.AirIntrinsicCallables))
        {
            if (map.TryGetValue(pair.Key, out var existing) && existing != pair.Value)
                throw new ArgumentException($"SSA profile maps AIR intrinsic '{pair.Key}' to both '{existing}' and '{pair.Value}'.");

            map[pair.Key] = pair.Value;
        }

        return map;
    }

    private static SsaCallableLoweringTargetSet MergeAirLoweringTargets(IEnumerable<SsaCallableLoweringTargetSet> targetSets) =>
        new(targetSets.SelectMany(static x => x.Values));
}

public sealed class SsaRouteProfileBuilder
{
    private readonly List<ISsaSemanticExtensionPack> _packs = [];
    private SsaRoutePolicy _policy;
    private CapabilitySet _capabilities = CapabilitySet.Empty;
    private SsaDiagnosticMode _diagnostics = SsaDiagnosticMode.Default;

    private SsaRouteProfileBuilder(SsaRoutePolicy policy)
    {
        _policy = policy;
    }

    public static SsaRouteProfileBuilder Create(SsaRoutePolicy policy = SsaRoutePolicy.Prefer) =>
        new(policy);

    public SsaRouteProfileBuilder WithPolicy(SsaRoutePolicy policy)
    {
        _policy = policy;
        return this;
    }

    public SsaRouteProfileBuilder AddPack(ISsaSemanticExtensionPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        if (_packs.Any(x => string.Equals(x.Id, pack.Id, StringComparison.Ordinal)))
            throw new ArgumentException($"SSA profile already contains extension pack '{pack.Id}'.", nameof(pack));

        _packs.Add(pack);
        return this;
    }

    public SsaRouteProfileBuilder RequireTargetCapabilities(CapabilitySet capabilities)
    {
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        return this;
    }

    public SsaRouteProfileBuilder WithDiagnostics(SsaDiagnosticMode diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        return this;
    }

    public SsaRouteProfile Build() =>
        new(_policy, _packs, _capabilities, _diagnostics);
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
        new Dictionary<string, CallableId>(StringComparer.Ordinal)
        {
            [AirIntrinsicIds.AddInt32Unchecked] = SsaPreviewCallables.AddInt32Unchecked,
            [AirIntrinsicIds.SubtractInt32Unchecked] = SsaPreviewCallables.SubtractInt32Unchecked,
            [AirIntrinsicIds.MultiplyInt32Unchecked] = SsaPreviewCallables.MultiplyInt32Unchecked,
            [AirIntrinsicIds.EqualInt32] = SsaPreviewCallables.EqualInt32
        };

    public SsaCallableLoweringTargetSet AirLoweringTargets =>
        SsaPreviewAirIntrinsicLowerings.ArithmeticInt32.ToTargetSet();

    public bool EnablesManagedCallables => false;

    public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() =>
        [new SsaConstantFoldingPass(SemanticDescriptors, new SsaPreviewInt32ConstantEvaluator())];
}

public sealed class SsaManagedCallablePack : ISsaSemanticExtensionPack
{
    public static SsaManagedCallablePack Instance { get; } = new();

    private SsaManagedCallablePack()
    {
    }

    public string Id => "ManagedCallable";

    public SemanticDescriptorSet SemanticDescriptors { get; } = new(
        types:
        [
            new SemanticTypeDescriptor(SsaPreviewSemanticTypes.Bool),
            new SemanticTypeDescriptor(SsaPreviewSemanticTypes.Int32),
            new SemanticTypeDescriptor(SsaPreviewSemanticTypes.Float64),
            new SemanticTypeDescriptor(SsaPreviewSemanticTypes.Object)
        ]);

    public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

    public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirManagedCallIntrinsicDescriptorResolver.Instance;

    public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
        new Dictionary<string, CallableId>(StringComparer.Ordinal);

    public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

    public bool EnablesManagedCallables => true;

    public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() => [];
}

public static class SsaPreviewRouteProfiles
{
    public static SsaRouteProfile Create(SsaRoutePolicy policy = SsaRoutePolicy.Prefer) =>
        SsaRouteProfileBuilder
            .Create(policy)
            .AddPack(SsaPreviewArithmeticInt32Pack.Instance)
            .AddPack(SsaManagedCallablePack.Instance)
            .Build();
}

public static class SsaRouteFactory
{
    public static AirToSsaConverter CreateLowerer(SsaRouteProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new AirToSsaConverter(
            new AirControlFlowGraphBuilder(),
            new AirStackAnalyzer(profile.AirIntrinsicResolver),
            new StructuralAirVerifier(
                new AirControlFlowGraphBuilder(),
                new AirStackAnalyzer(profile.AirIntrinsicResolver)),
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, profile.SemanticDescriptors),
            profile.AirIntrinsics,
            profile.AirIntrinsicCallables,
            profile.SemanticDescriptors,
            profile.EnablesManagedCallables);
    }

    public static SsaToAirConverter CreateEmitter(SsaRouteProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, profile.SemanticDescriptors),
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
            SsaCoreDescriptors.ConstantMaterialization,
            profile.SemanticDescriptors);
    }

    public static SsaRoundtripRoute CreateRoundtripRoute(SsaRouteProfile profile) =>
        new(CreateLowerer(profile), CreateEmitter(profile), profile);
}
