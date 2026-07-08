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
public sealed class SsaRoundtripRouteTests
{
    [Test]
    public void Run_WhenPolicyIsOff_ReturnsInputWithoutUsingSsa()
    {
        var source = new AbstractIR();
        source.Push(42);

        var result = new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Off);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Run_WhenPolicyIsPreferAndRouteIsUnsupported_FallsBackWithDiagnostics()
    {
        var source = new AbstractIR();
        source.Intrinsic("custom.intrinsic");

        var result = new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Prefer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.True);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("air.stack.invalid"));
        });
    }

    [Test]
    public void Run_WhenPolicyIsRequireAndRouteIsUnsupported_ThrowsDiagnosticException()
    {
        var source = new AbstractIR();
        source.Intrinsic("custom.intrinsic");

        var exception = Assert.Throws<SsaRouteException>(() =>
            new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Require));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("air.stack.invalid"));
    }

    [Test]
    public void Run_WhenPolicyIsDebugAndRouteIsSupported_ReturnsOptimizedRoundtrippedAir()
    {
        var source = new AbstractIR();
        source.Push(2);
        source.Push(3);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Program.Instructions.Select(static x => x.UOpCode), Is.EqualTo(new[]
            {
                UOpCode.Label,
                UOpCode.Push
            }));
            Assert.That(result.Program.Instructions[1].Operands.Single(), Is.EqualTo(5));
        });
    }

    [Test]
    public void Run_WhenProfileOptimizerFailsAndPolicyIsPrefer_FallsBackToInputWithDiagnostics()
    {
        var source = new AbstractIR();
        source.Push(1);

        var result = SsaRouteFactory
            .CreateRoundtripRoute(ProfileWithInvalidOptimizer(SsaRoutePolicy.Prefer))
            .Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.True);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.optimization.output.invalid"));
        });
    }

    [Test]
    public void Run_WhenProfileOptimizerFailsAndPolicyIsRequire_ThrowsDiagnosticException()
    {
        var source = new AbstractIR();
        source.Push(1);

        var exception = Assert.Throws<SsaRouteException>(() =>
            SsaRouteFactory
                .CreateRoundtripRoute(ProfileWithInvalidOptimizer(SsaRoutePolicy.Require))
                .Run(source));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.optimization.output.invalid"));
    }

    [Test]
    public void Run_WhenUsingRawConvertersWithoutProfile_DoesNotRunOptimizationPasses()
    {
        var source = new AbstractIR();
        source.Push(2);
        source.Push(3);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var result = new SsaRoundtripRoute(
                SsaRouteFactory.CreateLowerer(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug)),
                SsaRouteFactory.CreateEmitter(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug)))
            .Run(source, SsaRoutePolicy.Debug);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Program.Instructions.Select(static x => x.UOpCode), Is.EqualTo(new[]
            {
                UOpCode.Label,
                UOpCode.Push,
                UOpCode.Push,
                UOpCode.Intrinsic
            }));
        });
    }

    private static SsaRouteProfile ProfileWithInvalidOptimizer(SsaRoutePolicy policy) =>
        new(
            policy,
            new ISsaSemanticExtensionPack[]
            {
                SsaPreviewArithmeticInt32Pack.Instance,
                InvalidOptimizerPack.Instance
            });

    private sealed class InvalidOptimizerPack : ISsaSemanticExtensionPack
    {
        public static InvalidOptimizerPack Instance { get; } = new();

        public string Id => "InvalidOptimizer";

        public SemanticDescriptorSet SemanticDescriptors => SemanticDescriptorSet.Empty;

        public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

        public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsicDescriptorSet.Empty;

        public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
            new Dictionary<string, CallableId>(StringComparer.Ordinal);

        public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

        public bool EnablesManagedCallables => false;

        public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() =>
            new IIrOptimizationPass[] { new InvalidReturnOperandPass() };
    }

    private sealed class InvalidReturnOperandPass : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new("ssa.test.invalid-return-operand");

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } = IrStageContract.Empty;

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
        {
            var artifact = input.As<SsaArtifact>();
            var invalid = new SsaModule(
                artifact.Module.Id,
                artifact.Module.Functions.Select(static function => new SsaFunction(
                    function.Id,
                    function.EntryBlockId,
                    function.Blocks.Select(static block => block.Id == function.EntryBlockId
                        ? new SsaBlock(
                            block.Id,
                            block.Parameters,
                            instructions: block.Instructions,
                            terminator: SsaTerminator.Return([new SsaValueId("%missing.after.pass")]))
                        : block),
                    function.Parameters,
                    function.ReturnType)));

            return new IrStageResult(new SsaArtifact(invalid));
        }
    }
}
