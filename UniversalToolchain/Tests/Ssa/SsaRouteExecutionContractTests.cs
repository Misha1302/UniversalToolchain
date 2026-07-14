using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaRouteExecutionContractTests
{
    [Test]
    public void Run_WhenControlledOptimizerFailureUsesPrefer_ReportsOnlyCompletedPassesAndFallsBack()
    {
        var profile = CreateProfile(
            SsaRoutePolicy.Prefer,
            new IdentityPass("test.first"),
            new ControlledFailurePass("test.fail"),
            new IdentityPass("test.after"));
        var source = SourceWithConstant();

        var result = SsaRouteFactory.CreateRoundtripRoute(profile).Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.True);
            Assert.That(result.Report.ExecutedPasses, Is.EqualTo(new[] { "test.first" }));
            Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("test.optimization.failed"));
        });
    }

    [Test]
    public void Run_WhenControlledOptimizerFailureUsesRequire_ReportsOnlyCompletedPassesAndThrows()
    {
        var profile = CreateProfile(
            SsaRoutePolicy.Require,
            new IdentityPass("test.first"),
            new ControlledFailurePass("test.fail"),
            new IdentityPass("test.after"));
        var source = SourceWithConstant();

        var exception = Assert.Throws<SsaRouteException>(() =>
            SsaRouteFactory.CreateRoundtripRoute(profile).Run(source));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Report.FellBackToInput, Is.False);
            Assert.That(exception.Report.ExecutedPasses, Is.EqualTo(new[] { "test.first" }));
            Assert.That(exception.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("test.optimization.failed"));
        });
    }

    [Test]
    public void Run_WhenOptimizerThrowsUnexpectedly_DoesNotReportUnfinishedPassesOrFallback()
    {
        var profile = CreateProfile(
            SsaRoutePolicy.Prefer,
            new IdentityPass("test.first"),
            new UnexpectedFailurePass("test.fail"),
            new IdentityPass("test.after"));
        var source = SourceWithConstant();

        var exception = Assert.Throws<SsaRouteException>(() =>
            SsaRouteFactory.CreateRoundtripRoute(profile).Run(source));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Report.FellBackToInput, Is.False);
            Assert.That(exception.Report.ExecutedPasses, Is.EqualTo(new[] { "test.first" }));
            Assert.That(exception.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("ssa.route.unexpected"));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        });
    }

    [Test]
    public void Run_WhenProfileIsReused_CreatesFreshOptimizerPassInstances()
    {
        var profile = SsaRouteProfileBuilder
            .Create(SsaRoutePolicy.Require)
            .WithId("fresh-pass-profile")
            .AddPack(new SingleUsePassPack())
            .Build();
        var source = SourceWithConstant();

        var first = SsaRouteFactory.CreateRoundtripRoute(profile).Run(source);
        var second = SsaRouteFactory.CreateRoundtripRoute(profile).Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(first.UsedSsa, Is.True);
            Assert.That(second.UsedSsa, Is.True);
            Assert.That(first.Report.ExecutedPasses, Is.EqualTo(new[] { "test.single-use" }));
            Assert.That(second.Report.ExecutedPasses, Is.EqualTo(new[] { "test.single-use" }));
        });
    }

    [Test]
    public void Run_WhenSccpPropagatesConstantThroughJumpStack_EmitsVerifiedAirWithoutUnselectedArm()
    {
        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(SccpCrossBlockAirProgram());
        var opcodes = result.Program.Instructions.Select(static instruction => instruction.UOpCode).ToArray();
        var pushOperands = result.Program.Instructions
            .Where(static instruction => instruction.UOpCode == UOpCode.Push)
            .SelectMany(static instruction => instruction.Operands)
            .ToArray();
        var verification = new StructuralAirVerifier()
            .Verify(new AirArtifact(result.Program), new IrPipelineContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(opcodes, Does.Not.Contain(UOpCode.JmpIf));
            Assert.That(pushOperands, Does.Not.Contain(20));
            Assert.That(pushOperands, Does.Contain(10));
            Assert.That(verification.IsSuccess, Is.True,
                string.Join("; ", verification.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        });
    }

    private static SsaRouteProfile CreateProfile(
        SsaRoutePolicy policy,
        params IIrOptimizationPass[] passes) =>
        SsaRouteProfileBuilder
            .Create(policy)
            .WithId("execution-contract-profile")
            .AddPack(new TestPassPack(passes))
            .Build();

    private static AbstractIR SourceWithConstant()
    {
        var source = new AbstractIR();
        source.Push(42);
        return source;
    }

    private static AbstractIR SccpCrossBlockAirProgram()
    {
        var test = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var then = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var merge = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var source = new AbstractIR();
        source.Push(1);
        source.Jmp(test);
        source.SetLabel(test);
        source.Push(1);
        source.Intrinsic(AirIntrinsicIds.EqualInt32);
        source.JmpIf(then);
        source.Push(20);
        source.Jmp(merge);
        source.SetLabel(then);
        source.Push(10);
        source.SetLabel(merge);
        return source;
    }

    private sealed class TestPassPack(IReadOnlyList<IIrOptimizationPass> passes) : ISsaSemanticExtensionPack
    {
        public string Id => "test.execution-contract";

        public SemanticDescriptorSet SemanticDescriptors => SemanticDescriptorSet.Empty;

        public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

        public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsicDescriptorSet.Empty;

        public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
            new Dictionary<string, CallableId>(StringComparer.Ordinal);

        public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

        public bool EnablesManagedCallables => false;

        public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() => passes;
    }

    private sealed class SingleUsePassPack : ISsaSemanticExtensionPack
    {
        public string Id => "test.single-use-pack";

        public SemanticDescriptorSet SemanticDescriptors => SemanticDescriptorSet.Empty;

        public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

        public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsicDescriptorSet.Empty;

        public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
            new Dictionary<string, CallableId>(StringComparer.Ordinal);

        public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

        public bool EnablesManagedCallables => false;

        public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() => [new SingleUseIdentityPass()];
    }

    private class IdentityPass(string id) : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new(id);

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } =
            new(preservesFacts: [SsaFacts.StructuralVerification]);

        public virtual IrStageResult Run(IIrArtifact input, IrPipelineContext context) =>
            new(input, context.Facts);
    }

    private sealed class ControlledFailurePass(string id) : IdentityPass(id)
    {
        public override IrStageResult Run(IIrArtifact input, IrPipelineContext context) =>
            throw new SsaOptimizationException(
                "Synthetic controlled optimizer failure",
                [
                    new IrDiagnostic(
                        IrDiagnosticSeverity.Error,
                        "test.optimization.failed",
                        "Synthetic controlled optimizer failure.")
                ]);
    }

    private sealed class UnexpectedFailurePass(string id) : IdentityPass(id)
    {
        public override IrStageResult Run(IIrArtifact input, IrPipelineContext context) =>
            throw new InvalidOperationException("Synthetic unexpected optimizer failure.");
    }

    private sealed class SingleUseIdentityPass : IdentityPass
    {
        private bool _hasRun;

        public SingleUseIdentityPass()
            : base("test.single-use")
        {
        }

        public override IrStageResult Run(IIrArtifact input, IrPipelineContext context)
        {
            if (_hasRun)
                throw new InvalidOperationException("Optimizer pass instance was reused.");

            _hasRun = true;
            return base.Run(input, context);
        }
    }
}
