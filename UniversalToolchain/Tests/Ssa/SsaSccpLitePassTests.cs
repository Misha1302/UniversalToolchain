using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaSccpLitePassTests
{
    [Test]
    public void Run_PropagatesConstantThroughBlockArgumentAndFoldsReachableBranch()
    {
        var entryValue = new SsaValue(new SsaValueId("%entry.value"), SsaTypes.Int32);
        var testArgument = new SsaBlockParameter(new SsaValue(new SsaValueId("%test.arg"), SsaTypes.Int32));
        var one = new SsaValue(new SsaValueId("%one"), SsaTypes.Int32);
        var condition = new SsaValue(new SsaValueId("%condition"), SsaTypes.Bool);
        var thenValue = new SsaValue(new SsaValueId("%then.value"), SsaTypes.Int32);
        var elseValue = new SsaValue(new SsaValueId("%else.value"), SsaTypes.Int32);
        var entry = new SsaBlock(
            new SsaBlockId("entry"),
            instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("entry.value"), entryValue, 1)],
            terminator: SsaTerminator.Jump(new SsaBlockId("test"), [entryValue.Id]));
        var test = new SsaBlock(
            new SsaBlockId("test"),
            parameters: [testArgument],
            instructions:
            [
                SsaConstantMaterializer.Int32(new SsaOperationId("one"), one, 1),
                new SsaCall(
                    new SsaOperationId("equals"),
                    SsaPreviewCallables.EqualInt32,
                    [testArgument.Value.Id, one.Id],
                    [condition])
            ],
            terminator: SsaTerminator.Branch(
                condition.Id,
                new SsaBlockId("then"),
                [],
                new SsaBlockId("else"),
                []));
        var thenBlock = new SsaBlock(
            new SsaBlockId("then"),
            instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("then.value"), thenValue, 10)],
            terminator: SsaTerminator.Return([thenValue.Id]));
        var elseBlock = new SsaBlock(
            new SsaBlockId("else"),
            instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("else.value"), elseValue, 20)],
            terminator: SsaTerminator.Return([elseValue.Id]));

        var optimized = Run(ModuleWith(entry, test, thenBlock, elseBlock));
        var optimizedTest = optimized.Functions.Single().Blocks.Single(block => block.Id.Value == "test");

        Assert.Multiple(() =>
        {
            Assert.That(optimized.Functions.Single().Blocks.Select(static x => x.Id.Value), Is.EqualTo(new[] { "entry", "test", "then" }));
            Assert.That(optimizedTest.Instructions[1], Is.TypeOf<SsaOperation>());
            Assert.That(((SsaOperation)optimizedTest.Instructions[1]).OpId, Is.EqualTo(SsaOperations.ConstantBool));
            Assert.That(optimizedTest.Terminator?.Kind, Is.EqualTo(SsaTerminatorKind.Jump));
            Assert.That(optimizedTest.Terminator?.Transfers.Single().Target.Value, Is.EqualTo("then"));
        });
    }

    [Test]
    public void Run_WhenIncomingConstantsDisagree_MarksBlockArgumentOverdefinedAndDoesNotFoldCall()
    {
        var selector = new SsaBlockParameter(new SsaValue(new SsaValueId("%selector"), SsaTypes.Bool));
        var leftValue = new SsaValue(new SsaValueId("%left.value"), SsaTypes.Int32);
        var rightValue = new SsaValue(new SsaValueId("%right.value"), SsaTypes.Int32);
        var mergeArgument = new SsaBlockParameter(new SsaValue(new SsaValueId("%merge.arg"), SsaTypes.Int32));
        var one = new SsaValue(new SsaValueId("%one"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var entry = new SsaBlock(
            new SsaBlockId("entry"),
            parameters: [selector],
            terminator: SsaTerminator.Branch(
                selector.Value.Id,
                new SsaBlockId("left"),
                [],
                new SsaBlockId("right"),
                []));
        var left = new SsaBlock(
            new SsaBlockId("left"),
            instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("left.value"), leftValue, 1)],
            terminator: SsaTerminator.Jump(new SsaBlockId("merge"), [leftValue.Id]));
        var right = new SsaBlock(
            new SsaBlockId("right"),
            instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("right.value"), rightValue, 2)],
            terminator: SsaTerminator.Jump(new SsaBlockId("merge"), [rightValue.Id]));
        var merge = new SsaBlock(
            new SsaBlockId("merge"),
            parameters: [mergeArgument],
            instructions:
            [
                SsaConstantMaterializer.Int32(new SsaOperationId("one"), one, 1),
                new SsaCall(
                    new SsaOperationId("add"),
                    SsaPreviewCallables.AddInt32Unchecked,
                    [mergeArgument.Value.Id, one.Id],
                    [result])
            ],
            terminator: SsaTerminator.Return([result.Id]));

        var optimized = Run(new SsaModule(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("test.function"),
                    new SsaBlockId("entry"),
                    [entry, left, right, merge],
                    returnType: SsaTypes.Int32)
            ]));
        var optimizedMerge = optimized.Functions.Single().Blocks.Single(block => block.Id.Value == "merge");

        Assert.Multiple(() =>
        {
            Assert.That(optimized.Functions.Single().Blocks.Select(static x => x.Id.Value), Is.EqualTo(new[] { "entry", "left", "right", "merge" }));
            Assert.That(optimizedMerge.Instructions[1], Is.TypeOf<SsaCall>());
            Assert.That(((SsaCall)optimizedMerge.Instructions[1]).Callee, Is.EqualTo(SsaPreviewCallables.AddInt32Unchecked));
        });
    }

    [Test]
    public void Run_DoesNotEvaluateUntrustedPureCall()
    {
        var callable = new CallableId("test.untrusted.add");
        var left = new SsaValue(new SsaValueId("%left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("%right"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                SsaConstantMaterializer.Int32(new SsaOperationId("left"), left, 2),
                SsaConstantMaterializer.Int32(new SsaOperationId("right"), right, 3),
                new SsaCall(new SsaOperationId("call"), callable, [left.Id, right.Id], [result])
            ],
            terminator: SsaTerminator.Return([result.Id]));

        var optimized = Run(
            ModuleWith(block),
            new SemanticDescriptorSet(
                types: [new SemanticTypeDescriptor(SsaPreviewSemanticTypes.Int32)],
                callables:
                [
                    new CallableDescriptor(
                        callable,
                        new CallableSignature([SsaPreviewSemanticTypes.Int32, SsaPreviewSemanticTypes.Int32], [SsaPreviewSemanticTypes.Int32]),
                        effects: SemanticEffectSummary.Pure,
                        determinism: Determinism.Deterministic,
                        trustLevel: SemanticTrustLevel.UserProvidedUnchecked)
                ]));

        var optimizedBlock = optimized.Functions.Single().Blocks.Single();
        Assert.That(optimizedBlock.Instructions[2], Is.TypeOf<SsaCall>());
    }

    private static SsaModule Run(SsaModule module, SemanticDescriptorSet? descriptors = null)
    {
        var result = new SsaSparseConditionalConstantPropagationPass(
                descriptors ?? SsaPreviewSemanticDescriptors.ArithmeticInt32,
                new SsaPreviewInt32ConstantEvaluator())
            .Run(new SsaArtifact(module), new IrPipelineContext());

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
