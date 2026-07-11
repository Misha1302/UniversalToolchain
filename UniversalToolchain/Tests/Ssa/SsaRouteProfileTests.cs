using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Emission;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaRouteProfileTests
{
    private static readonly CapabilityId RequiredCapability = new("test.ssa.target-capability");

    [Test]
    public void Build_WhenPackIdIsRegisteredTwice_RejectsDuplicatePack()
    {
        var pack = new TestPack("duplicate-pack");
        var builder = SsaRouteProfileBuilder.Create().AddPack(pack);

        Assert.That(
            () => builder.AddPack(pack),
            Throws.ArgumentException.With.Message.Contains("already contains extension pack"));
    }

    [Test]
    public void Constructor_WhenPackIdIsRegisteredTwice_RejectsDuplicatePack()
    {
        var pack = new TestPack("duplicate-pack");

        Assert.That(
            () => new SsaRouteProfile(SsaRoutePolicy.Require, [pack, pack]),
            Throws.ArgumentException.With.Message.Contains("duplicate extension pack id"));
    }

    [Test]
    public void Build_WhenDifferentPacksDefineConflictingSemanticType_RejectsConflict()
    {
        var typeId = new SemanticTypeId("test.conflicting-type");
        var first = new TestPack(
            "first",
            semanticDescriptors: new SemanticDescriptorSet(
                types: [new SemanticTypeDescriptor(typeId, displayName: "First")]));
        var second = new TestPack(
            "second",
            semanticDescriptors: new SemanticDescriptorSet(
                types: [new SemanticTypeDescriptor(typeId, displayName: "Second")]));

        Assert.That(
            () => SsaRouteProfileBuilder.Create().AddPack(first).AddPack(second).Build(),
            Throws.ArgumentException.With.Message.Contains("conflicting semantic type descriptors"));
    }

    [Test]
    public void Build_WhenPacksCreateDuplicatePassIds_RejectsConflict()
    {
        var first = new TestPack("first", passes: [new CapabilityAwareIdentityPass("duplicate.pass")]);
        var second = new TestPack("second", passes: [new CapabilityAwareIdentityPass("duplicate.pass")]);

        Assert.That(
            () => SsaRouteProfileBuilder.Create().AddPack(first).AddPack(second).Build(),
            Throws.ArgumentException.With.Message.Contains("duplicate optimizer pass id"));
    }

    [Test]
    public void Run_WhenProfileSuppliesRequiredTargetCapability_PassExecutes()
    {
        var pass = new CapabilityAwareIdentityPass("capability.pass", RequiredCapability);
        var profile = SsaRouteProfileBuilder
            .Create(SsaRoutePolicy.Require)
            .WithId("capability-profile")
            .RequireTargetCapabilities(new CapabilitySet([RequiredCapability]))
            .AddPack(new TestPack("capability-pack", passes: [pass]))
            .Build();
        var source = new AbstractIR();
        source.Push(42);

        var result = SsaRouteFactory.CreateRoundtripRoute(profile).Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Report.ExecutedPasses, Does.Contain("capability.pass"));
        });
    }

    [Test]
    public void Run_WhenRequiredTargetCapabilityIsAbsent_ReportsCapabilityFailure()
    {
        var profile = SsaRouteProfileBuilder
            .Create(SsaRoutePolicy.Require)
            .WithId("missing-capability-profile")
            .AddPack(new TestPack(
                "missing-capability-pack",
                passes: [new CapabilityAwareIdentityPass("capability.pass", RequiredCapability)]))
            .Build();
        var source = new AbstractIR();
        source.Push(42);

        var exception = Assert.Throws<SsaRouteException>(() =>
            SsaRouteFactory.CreateRoundtripRoute(profile).Run(source));

        Assert.That(
            exception!.Diagnostics.Select(static diagnostic => diagnostic.Code),
            Does.Contain("ssa.optimization.capability.missing"));
    }

    [Test]
    public void Run_WhenDiagnosticsAreVerbose_EmitsTraceWithoutDebugPolicy()
    {
        var source = new AbstractIR();
        source.Push(42);
        var profile = SsaPreviewRouteProfiles.Create(
            SsaRoutePolicy.Require,
            diagnostics: SsaDiagnosticMode.Verbose);

        var result = SsaRouteFactory.CreateRoundtripRoute(profile).Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.Report.Trace, Is.Not.Empty);
            Assert.That(result.Report.Trace.Select(static entry => entry.Stage), Does.Contain("lowering"));
            Assert.That(result.Report.Trace.Select(static entry => entry.Stage), Does.Contain("emission"));
        });
    }

    [Test]
    public void Run_WhenPassThrowsUnexpectedException_DoesNotSilentlyPreferFallback()
    {
        var profile = SsaRouteProfileBuilder
            .Create(SsaRoutePolicy.Prefer)
            .WithId("unexpected-failure-profile")
            .AddPack(new TestPack("throwing-pack", passes: [new ThrowingPass()]))
            .Build();
        var source = new AbstractIR();
        source.Push(42);

        var exception = Assert.Throws<SsaRouteException>(() =>
            SsaRouteFactory.CreateRoundtripRoute(profile).Run(source));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("ssa.route.unexpected"));
            Assert.That(exception.Report.FellBackToInput, Is.False);
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        });
    }

    private sealed class TestPack : ISsaSemanticExtensionPack
    {
        private readonly IReadOnlyList<IIrOptimizationPass> _passes;

        public TestPack(
            string id,
            SemanticDescriptorSet? semanticDescriptors = null,
            IReadOnlyList<IIrOptimizationPass>? passes = null)
        {
            Id = id;
            SemanticDescriptors = semanticDescriptors ?? SemanticDescriptorSet.Empty;
            _passes = passes ?? [];
        }

        public string Id { get; }

        public SemanticDescriptorSet SemanticDescriptors { get; }

        public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

        public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsicDescriptorSet.Empty;

        public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
            new Dictionary<string, CallableId>(StringComparer.Ordinal);

        public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

        public bool EnablesManagedCallables => false;

        public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() => _passes.ToArray();
    }

    private sealed class CapabilityAwareIdentityPass : IIrOptimizationPass
    {
        public CapabilityAwareIdentityPass(string id, CapabilityId? requiredCapability = null)
        {
            Id = new IrStageId(id);
            Contract = requiredCapability is null
                ? new IrStageContract(preservesFacts: [SsaFacts.StructuralVerification])
                : new IrStageContract(
                    preservesFacts: [SsaFacts.StructuralVerification],
                    requiresCapabilities: [requiredCapability.Value]);
        }

        public IrStageId Id { get; }

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; }

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context) =>
            new(input, context.Facts);
    }

    private sealed class ThrowingPass : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new("throwing.pass");

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } = new(preservesFacts: [SsaFacts.StructuralVerification]);

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context) =>
            throw new InvalidOperationException("Synthetic optimizer defect.");
    }
}
