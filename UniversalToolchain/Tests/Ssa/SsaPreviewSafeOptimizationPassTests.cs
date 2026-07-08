using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

public sealed class SsaPreviewSafeOptimizationPassTests
{
    [Test]
    public void ConstantMaterializerAndReaderRoundTripFloat64()
    {
        var result = new SsaValue(new SsaValueId("v0"), SsaTypes.Float64);
        var operation = SsaConstantMaterializer.Float64(new SsaOperationId("op0"), result, 1.25d);

        Assert.That(SsaConstantReader.TryRead(operation, out var constant), Is.True);
        Assert.That(constant.Type, Is.EqualTo(SsaPreviewSemanticTypes.Float64));
        Assert.That(constant.CanonicalValue, Is.EqualTo("1.25"));
    }

    [Test]
    public void DeadPureInstructionEliminationRemovesUnusedConstants()
    {
        var dead = new SsaValue(new SsaValueId("dead"), SsaTypes.Int32);
        var live = new SsaValue(new SsaValueId("live"), SsaTypes.Int32);
        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                SsaConstantMaterializer.Int32(new SsaOperationId("dead.op"), dead, 10),
                SsaConstantMaterializer.Int32(new SsaOperationId("live.op"), live, 20)
            ],
            terminator: SsaTerminator.Return([live.Id]));

        var optimized = Run(
            new SsaDeadPureInstructionEliminationPass(),
            ModuleWith(block));

        var optimizedBlock = optimized.Functions.Single().Blocks.Single();
        Assert.That(optimizedBlock.Instructions.Select(static x => x.Id.Value), Is.EqualTo(new[] { "live.op" }));
    }

    [Test]
    public void DeadPureInstructionEliminationKeepsValuesUsedAcrossBlocks()
    {
        var transferred = new SsaValue(new SsaValueId("transferred"), SsaTypes.Int32);
        var entry = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                SsaConstantMaterializer.Int32(new SsaOperationId("transferred.op"), transferred, 42)
            ],
            terminator: SsaTerminator.Jump(new SsaBlockId("exit"), [transferred.Id]));
        var exitParameter = new SsaBlockParameter(new SsaValue(new SsaValueId("exit.arg"), SsaTypes.Int32));
        var exit = new SsaBlock(
            new SsaBlockId("exit"),
            parameters: [exitParameter],
            terminator: SsaTerminator.Return([exitParameter.Value.Id]));

        var optimized = Run(
            new SsaDeadPureInstructionEliminationPass(),
            ModuleWith(entry, exit));

        var optimizedEntry = optimized.Functions.Single().Blocks.Single(block => block.Id.Value == "entry");
        Assert.That(optimizedEntry.Instructions.Select(static x => x.Id.Value), Is.EqualTo(new[] { "transferred.op" }));
    }

    [Test]
    public void DeadPureInstructionEliminationKeepsUnknownCalls()
    {
        var result = new SsaValue(new SsaValueId("result"), SsaTypes.Int32);
        var call = new SsaCall(
            new SsaOperationId("call"),
            new CallableId("unknown.call"),
            results: [result]);
        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions: [call],
            terminator: SsaTerminator.Return());

        var optimized = Run(
            new SsaDeadPureInstructionEliminationPass(),
            ModuleWith(block));

        Assert.That(optimized.Functions.Single().Blocks.Single().Instructions, Has.Count.EqualTo(1));
    }

    [Test]
    public void DeadPureInstructionEliminationKeepsMayThrowCalls()
    {
        var descriptors = new SemanticDescriptorSet(
            types:
            [
                new SemanticTypeDescriptor(SsaPreviewSemanticTypes.Int32)
            ],
            callables:
            [
                new CallableDescriptor(
                    new CallableId("throwing.call"),
                    new CallableSignature([], [SsaPreviewSemanticTypes.Int32]),
                    effects: new SemanticEffectSummary([SemanticEffectKind.MayThrow]),
                    determinism: Determinism.Deterministic,
                    trustLevel: SemanticTrustLevel.BuiltInTrusted)
            ]);
        var result = new SsaValue(new SsaValueId("result"), SsaTypes.Int32);
        var call = new SsaCall(
            new SsaOperationId("call"),
            new CallableId("throwing.call"),
            results: [result]);
        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions: [call],
            terminator: SsaTerminator.Return());

        var optimized = Run(
            new SsaDeadPureInstructionEliminationPass(SsaCoreDescriptors.ConstantMaterialization, descriptors),
            ModuleWith(block));

        Assert.That(optimized.Functions.Single().Blocks.Single().Instructions, Has.Count.EqualTo(1));
    }

    [Test]
    public void BranchFoldingConvertsConstantBranchToJumpAndDropsUnreachableBlock()
    {
        var condition = new SsaValue(new SsaValueId("condition"), SsaTypes.Bool);
        var result = new SsaValue(new SsaValueId("result"), SsaTypes.Int32);
        var entry = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                SsaConstantMaterializer.Bool(new SsaOperationId("condition.op"), condition, true)
            ],
            terminator: SsaTerminator.Branch(
                condition.Id,
                new SsaBlockId("then"),
                [],
                new SsaBlockId("else"),
                []));
        var thenBlock = new SsaBlock(
            new SsaBlockId("then"),
            instructions:
            [
                SsaConstantMaterializer.Int32(new SsaOperationId("then.value"), result, 1)
            ],
            terminator: SsaTerminator.Return([result.Id]));
        var elseBlock = new SsaBlock(
            new SsaBlockId("else"),
            terminator: SsaTerminator.Unreachable());

        var optimized = Run(
            new SsaBranchFoldingAndCleanupPass(),
            ModuleWith(entry, thenBlock, elseBlock));

        var blocks = optimized.Functions.Single().Blocks;
        Assert.That(blocks.Select(static x => x.Id.Value), Is.EqualTo(new[] { "entry", "then" }));
        Assert.That(blocks[0].Terminator?.Kind, Is.EqualTo(SsaTerminatorKind.Jump));
        Assert.That(blocks[0].Terminator?.Transfers.Single().Target.Value, Is.EqualTo("then"));
    }

    private static SsaModule Run(IIrOptimizationPass pass, SsaModule module)
    {
        var result = pass.Run(new SsaArtifact(module), new IrPipelineContext());
        return result.Artifact.As<SsaArtifact>().Module;
    }

    private static SsaModule ModuleWith(params SsaBlock[] blocks) =>
        new(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("test.function"),
                    new SsaBlockId("entry"),
                    blocks,
                    returnType: SsaTypes.Int32)
            ]);
}
