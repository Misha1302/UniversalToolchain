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
    public void DeadPureInstructionEliminationKeepsOperandProducersForLivePureCall()
    {
        var callable = new CallableId("test.pure.add");
        var left = new SsaValue(new SsaValueId("left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("right"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("result"), SsaTypes.Int32);
        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                SsaConstantMaterializer.Int32(new SsaOperationId("left.op"), left, 2),
                SsaConstantMaterializer.Int32(new SsaOperationId("right.op"), right, 3),
                new SsaCall(new SsaOperationId("call.add"), callable, [left.Id, right.Id], [result])
            ],
            terminator: SsaTerminator.Return([result.Id]));

        var optimized = Run(
            new SsaDeadPureInstructionEliminationPass(SsaCoreDescriptors.ConstantMaterialization, PureCallableDescriptors(callable)),
            ModuleWith(block));

        Assert.That(
            optimized.Functions.Single().Blocks.Single().Instructions.Select(static x => x.Id.Value),
            Is.EqualTo(new[] { "left.op", "right.op", "call.add" }));
    }

    [Test]
    public void DeadPureInstructionEliminationRemovesDeadTrustedPureCallAndItsOperands()
    {
        var callable = new CallableId("test.pure.dead");
        var left = new SsaValue(new SsaValueId("left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("right"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("result"), SsaTypes.Int32);
        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                SsaConstantMaterializer.Int32(new SsaOperationId("left.op"), left, 2),
                SsaConstantMaterializer.Int32(new SsaOperationId("right.op"), right, 3),
                new SsaCall(new SsaOperationId("call.dead"), callable, [left.Id, right.Id], [result])
            ],
            terminator: SsaTerminator.Return());

        var optimized = Run(
            new SsaDeadPureInstructionEliminationPass(SsaCoreDescriptors.ConstantMaterialization, PureCallableDescriptors(callable)),
            ModuleWith(block));

        Assert.That(optimized.Functions.Single().Blocks.Single().Instructions, Is.Empty);
    }

    [Test]
    public void DeadPureInstructionEliminationKeepsUntrustedPureCalls()
    {
        var callable = new CallableId("plugin.untrusted.pure");
        var result = new SsaValue(new SsaValueId("result"), SsaTypes.Int32);
        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                new SsaCall(new SsaOperationId("call.untrusted"), callable, results: [result])
            ],
            terminator: SsaTerminator.Return());

        var optimized = Run(
            new SsaDeadPureInstructionEliminationPass(
                SsaCoreDescriptors.ConstantMaterialization,
                PureCallableDescriptors(callable, SemanticTrustLevel.UserProvidedUnchecked)),
            ModuleWith(block));

        Assert.That(optimized.Functions.Single().Blocks.Single().Instructions.Select(static x => x.Id.Value), Is.EqualTo(new[] { "call.untrusted" }));
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

    [Test]
    public void BranchFoldingPreservesSelectedFalseTransferArguments()
    {
        var condition = new SsaValue(new SsaValueId("condition"), SsaTypes.Bool);
        var payload = new SsaValue(new SsaValueId("payload"), SsaTypes.Int32);
        var exitArgument = new SsaBlockParameter(new SsaValue(new SsaValueId("exit.arg"), SsaTypes.Int32));
        var entry = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                SsaConstantMaterializer.Bool(new SsaOperationId("condition.op"), condition, false),
                SsaConstantMaterializer.Int32(new SsaOperationId("payload.op"), payload, 11)
            ],
            terminator: SsaTerminator.Branch(
                condition.Id,
                new SsaBlockId("then"),
                [],
                new SsaBlockId("else"),
                [payload.Id]));
        var thenBlock = new SsaBlock(new SsaBlockId("then"), terminator: SsaTerminator.Unreachable());
        var elseBlock = new SsaBlock(
            new SsaBlockId("else"),
            parameters: [exitArgument],
            terminator: SsaTerminator.Return([exitArgument.Value.Id]));

        var optimized = Run(new SsaBranchFoldingAndCleanupPass(), ModuleWith(entry, thenBlock, elseBlock));
        var optimizedEntry = optimized.Functions.Single().Blocks.Single(block => block.Id.Value == "entry");
        var transfer = optimizedEntry.Terminator!.Transfers.Single();

        Assert.Multiple(() =>
        {
            Assert.That(optimized.Functions.Single().Blocks.Select(static x => x.Id.Value), Is.EqualTo(new[] { "entry", "else" }));
            Assert.That(optimizedEntry.Terminator.Kind, Is.EqualTo(SsaTerminatorKind.Jump));
            Assert.That(transfer.Target.Value, Is.EqualTo("else"));
            Assert.That(transfer.Arguments, Is.EqualTo(new[] { payload.Id }));
        });
    }

    [Test]
    public void BranchFoldingDoesNotFoldNonLocalBoolConditions()
    {
        var conditionParameter = new SsaBlockParameter(new SsaValue(new SsaValueId("condition.arg"), SsaTypes.Bool));
        var result = new SsaValue(new SsaValueId("result"), SsaTypes.Int32);
        var entry = new SsaBlock(
            new SsaBlockId("entry"),
            parameters: [conditionParameter],
            terminator: SsaTerminator.Branch(
                conditionParameter.Value.Id,
                new SsaBlockId("then"),
                [],
                new SsaBlockId("else"),
                []));
        var thenBlock = new SsaBlock(
            new SsaBlockId("then"),
            instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("then.value"), result, 1)],
            terminator: SsaTerminator.Return([result.Id]));
        var elseBlock = new SsaBlock(new SsaBlockId("else"), terminator: SsaTerminator.Unreachable());

        var optimized = Run(new SsaBranchFoldingAndCleanupPass(), ModuleWith(entry, thenBlock, elseBlock));
        var optimizedEntry = optimized.Functions.Single().Blocks.Single(block => block.Id.Value == "entry");

        Assert.Multiple(() =>
        {
            Assert.That(optimized.Functions.Single().Blocks.Select(static x => x.Id.Value), Is.EqualTo(new[] { "entry", "then", "else" }));
            Assert.That(optimizedEntry.Terminator?.Kind, Is.EqualTo(SsaTerminatorKind.Branch));
        });
    }

    [Test]
    public void BranchFoldingDropsUnreachableChainsAfterSelectingConstantBranch()
    {
        var condition = new SsaValue(new SsaValueId("condition"), SsaTypes.Bool);
        var result = new SsaValue(new SsaValueId("result"), SsaTypes.Int32);
        var entry = new SsaBlock(
            new SsaBlockId("entry"),
            instructions: [SsaConstantMaterializer.Bool(new SsaOperationId("condition.op"), condition, true)],
            terminator: SsaTerminator.Branch(
                condition.Id,
                new SsaBlockId("then"),
                [],
                new SsaBlockId("dead.1"),
                []));
        var thenBlock = new SsaBlock(
            new SsaBlockId("then"),
            instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("then.value"), result, 1)],
            terminator: SsaTerminator.Return([result.Id]));
        var dead1 = new SsaBlock(
            new SsaBlockId("dead.1"),
            terminator: SsaTerminator.Jump(new SsaBlockId("dead.2"), []));
        var dead2 = new SsaBlock(
            new SsaBlockId("dead.2"),
            terminator: SsaTerminator.Unreachable());

        var optimized = Run(new SsaBranchFoldingAndCleanupPass(), ModuleWith(entry, thenBlock, dead1, dead2));

        Assert.That(optimized.Functions.Single().Blocks.Select(static x => x.Id.Value), Is.EqualTo(new[] { "entry", "then" }));
    }

    private static SsaModule Run(IIrOptimizationPass pass, SsaModule module)
    {
        var result = pass.Run(new SsaArtifact(module), new IrPipelineContext());
        return result.Artifact.As<SsaArtifact>().Module;
    }

    private static SemanticDescriptorSet PureCallableDescriptors(
        CallableId callable,
        SemanticTrustLevel trustLevel = SemanticTrustLevel.BuiltInTrusted) =>
        new(
            types: [new SemanticTypeDescriptor(SsaPreviewSemanticTypes.Int32)],
            callables:
            [
                new CallableDescriptor(
                    callable,
                    new CallableSignature([SsaPreviewSemanticTypes.Int32, SsaPreviewSemanticTypes.Int32], [SsaPreviewSemanticTypes.Int32]),
                    effects: SemanticEffectSummary.Pure,
                    determinism: Determinism.Deterministic,
                    trustLevel: trustLevel)
            ]);

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
