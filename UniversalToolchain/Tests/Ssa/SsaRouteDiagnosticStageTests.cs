using System.Collections.ObjectModel;
using BasicCore.Builtins;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Emission;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaRouteDiagnosticStageTests
{
    [Test]
    public void Run_WhenLoweringFails_ReportsLoweringStage()
    {
        var source = new AbstractIR();
        source.Intrinsic("custom.intrinsic");

        var result = new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Prefer);

        Assert.That(
            result.Diagnostics.Single(diagnostic => diagnostic.Code == "air.stack.invalid").Stage,
            Is.EqualTo("lowering"));
    }

    [Test]
    public void Run_WhenOptimizerOutputFailsVerification_ReportsOptimizationStage()
    {
        var profile = SsaRouteProfileBuilder
            .Create(SsaRoutePolicy.Prefer)
            .WithId("diagnostic-stage-optimization")
            .AddPack(new InvalidOptimizerPack())
            .Build();
        var source = new AbstractIR();
        source.Push(42);

        var result = SsaRouteFactory.CreateRoundtripRoute(profile).Run(source);

        Assert.That(
            result.Diagnostics.Single(diagnostic => diagnostic.Code == "ssa.optimization.output.invalid").Stage,
            Is.EqualTo("optimization"));
    }

    [Test]
    public void Run_WhenEmissionRequiresRepeatedValueScheduling_ReportsEmissionStage()
    {
        var profile = SsaRouteProfileBuilder
            .Create(SsaRoutePolicy.Require)
            .WithId("diagnostic-stage-emission")
            .AddPack(new CseArithmeticPack())
            .Build();
        var source = new AbstractIR();
        source.Push(2);
        source.Push(3);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);
        source.Push(2);
        source.Push(3);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);
        source.Intrinsic(AirIntrinsicIds.MultiplyInt32Unchecked);

        var exception = Assert.Throws<SsaRouteException>(() =>
            SsaRouteFactory.CreateRoundtripRoute(profile).Run(source));

        Assert.That(
            exception!.Diagnostics.Single(diagnostic =>
                diagnostic.Code == "ssa.to-air.value-reuse.unsupported").Stage,
            Is.EqualTo("emission"));
    }

    [Test]
    public void Diagnostic_TwoArgumentConstructorRemainsCompatible()
    {
        var diagnostic = new SsaRouteDiagnostic("test.code", "message");
        var (code, message) = diagnostic;

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo("test.code"));
            Assert.That(message, Is.EqualTo("message"));
            Assert.That(diagnostic.Stage, Is.Null);
        });
    }

    private sealed class InvalidOptimizerPack : ISsaSemanticExtensionPack
    {
        public string Id => "invalid-optimizer-pack";

        public SemanticDescriptorSet SemanticDescriptors => SemanticDescriptorSet.Empty;

        public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

        public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsicDescriptorSet.Empty;

        public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
            new ReadOnlyDictionary<string, CallableId>(
                new Dictionary<string, CallableId>(StringComparer.Ordinal));

        public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

        public bool EnablesManagedCallables => false;

        public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() =>
            [new InvalidEntryPass()];
    }

    private sealed class InvalidEntryPass : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new("test.invalid-entry");

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } =
            new(preservesFacts: [SsaFacts.StructuralVerification]);

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
        {
            var artifact = input.As<SsaArtifact>();
            var invalid = new SsaModule(
                artifact.Module.Id,
                artifact.Module.Functions.Select(static function => new SsaFunction(
                    function.Id,
                    new SsaBlockId("missing.entry"),
                    function.Blocks,
                    function.Parameters,
                    function.ReturnType)));
            return new IrStageResult(new SsaArtifact(invalid), context.Facts);
        }
    }

    private sealed class CseArithmeticPack : ISsaSemanticExtensionPack
    {
        public string Id => "cse-arithmetic-pack";

        public SemanticDescriptorSet SemanticDescriptors => SsaSemanticDescriptors.ArithmeticInt32;

        public AirIntrinsicDescriptorSet AirIntrinsics => AirCoreIntrinsicDescriptors.ArithmeticInt32;

        public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsics;

        public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
            new ReadOnlyDictionary<string, CallableId>(
                new Dictionary<string, CallableId>(StringComparer.Ordinal)
                {
                    [AirIntrinsicIds.AddInt32Unchecked] = SsaCallables.AddInt32Unchecked,
                    [AirIntrinsicIds.SubtractInt32Unchecked] = SsaCallables.SubtractInt32Unchecked,
                    [AirIntrinsicIds.MultiplyInt32Unchecked] = SsaCallables.MultiplyInt32Unchecked,
                    [AirIntrinsicIds.EqualInt32] = SsaCallables.EqualInt32
                });

        public SsaCallableLoweringTargetSet AirLoweringTargets =>
            SsaAirIntrinsicLowerings.ArithmeticInt32.ToTargetSet();

        public bool EnablesManagedCallables => false;

        public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() =>
            [new SsaLocalCommonSubexpressionEliminationPass(SemanticDescriptors)];
    }
}
