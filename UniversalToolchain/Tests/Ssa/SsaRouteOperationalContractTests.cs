using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Emission;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaRouteOperationalContractTests
{
    [Test]
    public void Run_WhenOptimizerOutputIsInvalid_PreferFallsBackAndReportsOnlyReturnedPasses()
    {
        var neverRun = new PassRunCounter();
        var profile = CreateFailureProfile(SsaRoutePolicy.Prefer, neverRun);
        var source = new AbstractIR();
        source.Push(42);

        var result = SsaRouteFactory.CreateRoundtripRoute(profile).Run(source);

        Assert.Multiple(() =>
        {
  Assert.That(result.Program, Is.SameAs(source));
  Assert.That(result.FellBackToInput, Is.True);
  Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Code),
      Does.Contain("ssa.optimization.output.invalid"));
  Assert.That(result.Report.ExecutedPasses,
      Is.EqualTo(new[] { "first.pass", "invalid.pass" }));
  Assert.That(neverRun.Count, Is.Zero);
        });
    }

    [Test]
    public void Run_WhenOptimizerOutputIsInvalid_RequireThrowsAndReportsOnlyReturnedPasses()
    {
        var neverRun = new PassRunCounter();
        var profile = CreateFailureProfile(SsaRoutePolicy.Require, neverRun);
        var source = new AbstractIR();
        source.Push(42);

        var exception = Assert.Throws<SsaRouteException>(() =>
  SsaRouteFactory.CreateRoundtripRoute(profile).Run(source));

        Assert.Multiple(() =>
        {
  Assert.That(exception!.Diagnostics.Select(static diagnostic => diagnostic.Code),
      Does.Contain("ssa.optimization.output.invalid"));
  Assert.That(exception.Report.ExecutedPasses,
      Is.EqualTo(new[] { "first.pass", "invalid.pass" }));
  Assert.That(neverRun.Count, Is.Zero);
        });
    }

    [Test]
    public void CreateOptimizationPasses_CreatesFreshInstancesForEachPipeline()
    {
        var profile = SsaRouteProfileBuilder
  .Create(SsaRoutePolicy.Require)
  .WithId("fresh-pass-profile")
  .AddPack(new FactoryPack(
      "fresh-pass-pack",
      static () => [new IdentityPass("fresh.pass")]))
  .Build();

        var first = profile.CreateOptimizationPasses().Single();
        var second = profile.CreateOptimizationPasses().Single();

        Assert.That(second, Is.Not.SameAs(first));
    }

    [Test]
    public void Run_WhenSccpPropagatesAcrossAirBlockTransfer_CompletesFullRoundtrip()
    {
        var source = SccpCrossBlockAirProgram();

        var result = SsaRouteFactory
  .CreateRoundtripRoute(SsaRouteProfiles.Create(SsaRoutePolicy.Debug))
  .Run(source);

        var opcodes = result.Program.Instructions.Select(static instruction => instruction.UOpCode).ToArray();
        var pushes = result.Program.Instructions
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
  Assert.That(pushes, Does.Not.Contain(20));
  Assert.That(pushes, Does.Contain(10));
  Assert.That(verification.IsSuccess, Is.True,
      string.Join("; ", verification.Diagnostics.Select(static diagnostic =>
$"{diagnostic.Code}: {diagnostic.Message}")));
        });
    }

    private static SsaRouteProfile CreateFailureProfile(
        SsaRoutePolicy policy,
        PassRunCounter neverRun) =>
        SsaRouteProfileBuilder
  .Create(policy)
  .WithId($"optimizer-failure-{policy}")
  .AddPack(new FactoryPack(
      "optimizer-failure-pack",
      () =>
      [
new IdentityPass("first.pass"),
new InvalidEntryPass("invalid.pass"),
new IdentityPass("never.pass", neverRun)
      ]))
  .Build();

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

    private sealed class FactoryPack(
        string id,
        Func<IReadOnlyList<IIrOptimizationPass>> passFactory) : ISsaSemanticExtensionPack
    {
        public string Id { get; } = id;

        public SemanticDescriptorSet SemanticDescriptors => SemanticDescriptorSet.Empty;

        public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

        public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsicDescriptorSet.Empty;

        public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
  new Dictionary<string, CallableId>(StringComparer.Ordinal);

        public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

        public bool EnablesManagedCallables => false;

        public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() => passFactory();
    }

    private sealed class PassRunCounter
    {
        public int Count { get; set; }
    }

    private sealed class IdentityPass(
        string id,
        PassRunCounter? counter = null) : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new(id);

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } =
  new(preservesFacts: [SsaFacts.StructuralVerification]);

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
        {
  if (counter is not null)
      counter.Count++;
  return new IrStageResult(input, context.Facts);
        }
    }

    private sealed class InvalidEntryPass(string id) : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new(id);

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
}
