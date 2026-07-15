using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaDeadBlockArgumentEliminationTests
{
    [Test]
    public void Run_WhenTransferredParametersBecomeDead_RemovesParametersArgumentsAndProducerAtFixedPoint()
    {
        var payload = new SsaValue(new SsaValueId("%payload"), SsaTypes.Int32);
        var middleArgument = new SsaBlockParameter(
            new SsaValue(new SsaValueId("%middle.arg"), SsaTypes.Int32));
        var exitArgument = new SsaBlockParameter(
            new SsaValue(new SsaValueId("%exit.arg"), SsaTypes.Int32));
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(
            new SsaBlock(
                new SsaBlockId("entry"),
                instructions:
                [
                    SsaConstantMaterializer.Int32(
                        new SsaOperationId("payload"),
                        payload,
                        1)
                ],
                terminator: SsaTerminator.Jump(
                    new SsaBlockId("middle"),
                    [payload.Id])),
            new SsaBlock(
                new SsaBlockId("middle"),
                parameters: [middleArgument],
                terminator: SsaTerminator.Jump(
                    new SsaBlockId("exit"),
                    [middleArgument.Value.Id])),
            new SsaBlock(
                new SsaBlockId("exit"),
                parameters: [exitArgument],
                instructions:
                [
                    SsaConstantMaterializer.Int32(
                        new SsaOperationId("result"),
                        result,
                        10)
                ],
                terminator: SsaTerminator.Return([result.Id])));

        var optimized = Run(artifact);
        var function = optimized.Module.Functions.Single();
        var entry = function.Blocks.Single(block => block.Id.Value == "entry");
        var middle = function.Blocks.Single(block => block.Id.Value == "middle");
        var exit = function.Blocks.Single(block => block.Id.Value == "exit");
        var verification = new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization)
            .Verify(optimized, new IrPipelineContext());

        Assert.Multiple(() =>
        {
            Assert.That(entry.Instructions, Is.Empty);
            Assert.That(entry.Terminator!.Transfers.Single().Arguments, Is.Empty);
            Assert.That(middle.Parameters, Is.Empty);
            Assert.That(middle.Terminator!.Transfers.Single().Arguments, Is.Empty);
            Assert.That(exit.Parameters, Is.Empty);
            Assert.That(exit.Instructions.Select(static instruction => instruction.Id.Value),
                Is.EqualTo(new[] { "result" }));
            Assert.That(verification.IsSuccess, Is.True,
                string.Join("; ", verification.Diagnostics.Select(static diagnostic =>
                    $"{diagnostic.Code}: {diagnostic.Message}")));
        });
    }

    [Test]
    public void Run_WhenBlockParameterIsObserved_PreservesParameterArgumentAndProducer()
    {
        var payload = new SsaValue(new SsaValueId("%payload"), SsaTypes.Int32);
        var exitArgument = new SsaBlockParameter(
            new SsaValue(new SsaValueId("%exit.arg"), SsaTypes.Int32));
        var artifact = Artifact(
            new SsaBlock(
                new SsaBlockId("entry"),
                instructions:
                [
                    SsaConstantMaterializer.Int32(
                        new SsaOperationId("payload"),
                        payload,
                        42)
                ],
                terminator: SsaTerminator.Jump(
                    new SsaBlockId("exit"),
                    [payload.Id])),
            new SsaBlock(
                new SsaBlockId("exit"),
                parameters: [exitArgument],
                terminator: SsaTerminator.Return([exitArgument.Value.Id])));

        var optimized = Run(artifact);
        var function = optimized.Module.Functions.Single();
        var entry = function.Blocks.Single(block => block.Id.Value == "entry");
        var exit = function.Blocks.Single(block => block.Id.Value == "exit");

        Assert.Multiple(() =>
        {
            Assert.That(entry.Instructions.Select(static instruction => instruction.Id.Value),
                Is.EqualTo(new[] { "payload" }));
            Assert.That(entry.Terminator!.Transfers.Single().Arguments,
                Is.EqualTo(new[] { payload.Id }));
            Assert.That(exit.Parameters.Select(static parameter => parameter.Value.Id),
                Is.EqualTo(new[] { exitArgument.Value.Id }));
        });
    }

    [Test]
    public void Run_WhenSccpMakesBlockArgumentDead_FullRouteEmitsNoTransferPushOrDrop()
    {
        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(SccpCrossBlockAirProgram());
        var opcodes = result.Program.Instructions
            .Select(static instruction => instruction.UOpCode)
            .ToArray();
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
            Assert.That(opcodes, Does.Not.Contain(UOpCode.Drop));
            Assert.That(pushes, Does.Not.Contain(1));
            Assert.That(pushes, Does.Not.Contain(20));
            Assert.That(pushes, Does.Contain(10));
            Assert.That(verification.IsSuccess, Is.True,
                string.Join("; ", verification.Diagnostics.Select(static diagnostic =>
                    $"{diagnostic.Code}: {diagnostic.Message}")));
        });
    }

    private static SsaArtifact Run(SsaArtifact artifact) =>
        new SsaDeadPureInstructionEliminationPass()
            .Run(artifact, new IrPipelineContext())
            .Artifact
            .As<SsaArtifact>();

    private static SsaArtifact Artifact(params SsaBlock[] blocks) =>
        new(new SsaModule(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("test.function"),
                    new SsaBlockId("entry"),
                    blocks,
                    returnType: SsaTypes.Int32)
            ]));

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
}
