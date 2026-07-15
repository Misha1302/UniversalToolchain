using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaLocalCommonSubexpressionEliminationPassTests
{
    [Test]
    public void Run_WhenTrustedPureCallRepeats_EliminatesDuplicateAndRewritesUse()
    {
        var left = Value("left");
        var right = Value("right");
        var first = Value("first");
        var duplicate = Value("duplicate");
        var product = Value("product");
        var artifact = Artifact(new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Const("left", left, 2),
                Const("right", right, 3),
                Call("add.first", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], first),
                Call("add.duplicate", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], duplicate),
                Call("multiply", SsaCallables.MultiplyInt32Unchecked, [first.Id, duplicate.Id], product)
            ],
            terminator: SsaTerminator.Return([product.Id])));

        var optimized = Run(artifact, SsaSemanticDescriptors.ArithmeticInt32);
        var block = optimized.Module.Functions.Single().Blocks.Single();
        var multiply = (SsaCall)block.Instructions.Single(instruction => instruction.Id.Value == "multiply");

        Assert.Multiple(() =>
        {
            Assert.That(
                block.Instructions.Select(static instruction => instruction.Id.Value),
                Is.EqualTo(new[] { "left", "right", "add.first", "multiply" }));
            Assert.That(multiply.Operands, Is.EqualTo(new[] { first.Id, first.Id }));
            AssertVerified(optimized, SsaSemanticDescriptors.ArithmeticInt32);
        });
    }

    [Test]
    public void Run_WhenTrustedCallableIsCommutative_EliminatesReversedOperands()
    {
        var left = Value("left");
        var right = Value("right");
        var first = Value("first");
        var duplicate = Value("duplicate");
        var artifact = Artifact(new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Const("left", left, 2),
                Const("right", right, 3),
                Call("add.first", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], first),
                Call("add.duplicate", SsaCallables.AddInt32Unchecked, [right.Id, left.Id], duplicate)
            ],
            terminator: SsaTerminator.Return([duplicate.Id])));

        var optimized = Run(artifact, SsaSemanticDescriptors.ArithmeticInt32);
        var block = optimized.Module.Functions.Single().Blocks.Single();

        Assert.Multiple(() =>
        {
            Assert.That(
                block.Instructions.Select(static instruction => instruction.Id.Value),
                Is.EqualTo(new[] { "left", "right", "add.first" }));
            Assert.That(block.Terminator!.Operands, Is.EqualTo(new[] { first.Id }));
            AssertVerified(optimized, SsaSemanticDescriptors.ArithmeticInt32);
        });
    }

    [Test]
    public void Run_WhenTrustedCallableIsNotCommutative_PreservesReversedOperands()
    {
        var left = Value("left");
        var right = Value("right");
        var first = Value("first");
        var second = Value("second");
        var artifact = Artifact(new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Const("left", left, 2),
                Const("right", right, 3),
                Call("subtract.first", SsaCallables.SubtractInt32Unchecked, [left.Id, right.Id], first),
                Call("subtract.second", SsaCallables.SubtractInt32Unchecked, [right.Id, left.Id], second)
            ],
            terminator: SsaTerminator.Return([second.Id])));

        var optimized = Run(artifact, SsaSemanticDescriptors.ArithmeticInt32);

        Assert.That(
            optimized.Module.Functions.Single().Blocks.Single().Instructions
                .Select(static instruction => instruction.Id.Value),
            Is.EqualTo(new[] { "left", "right", "subtract.first", "subtract.second" }));
    }

    [Test]
    public void Run_WhenCallableIsUntrustedOrEffectful_PreservesRepeatedCalls()
    {
        var untrusted = new CallableId("test.untrusted");
        var throwing = new CallableId("test.throwing");
        var descriptors = DescriptorSet(
            new CallableDescriptor(
                untrusted,
                new CallableSignature([], [SsaSemanticTypes.Int32]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                trustLevel: SemanticTrustLevel.UserProvidedUnchecked),
            new CallableDescriptor(
                throwing,
                new CallableSignature([], [SsaSemanticTypes.Int32]),
                effects: new SemanticEffectSummary([SemanticEffectKind.MayThrow]),
                determinism: Determinism.Deterministic,
                trustLevel: SemanticTrustLevel.BuiltInTrusted));
        var first = Value("first");
        var second = Value("second");
        var third = Value("third");
        var fourth = Value("fourth");
        var artifact = Artifact(new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Call("untrusted.first", untrusted, [], first),
                Call("untrusted.second", untrusted, [], second),
                Call("throwing.first", throwing, [], third),
                Call("throwing.second", throwing, [], fourth)
            ],
            terminator: SsaTerminator.Return([fourth.Id])));

        var optimized = Run(artifact, descriptors);

        Assert.That(
            optimized.Module.Functions.Single().Blocks.Single().Instructions,
            Has.Count.EqualTo(4));
    }

    [Test]
    public void Run_DoesNotShareExpressionAvailabilityAcrossBlocks()
    {
        var left = Value("left");
        var right = Value("right");
        var first = Value("first");
        var second = Value("second");
        var entry = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Const("left", left, 2),
                Const("right", right, 3),
                Call("add.entry", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], first)
            ],
            terminator: SsaTerminator.Jump(new SsaBlockId("exit")));
        var exit = new SsaBlock(
            new SsaBlockId("exit"),
            instructions:
            [
                Call("add.exit", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], second)
            ],
            terminator: SsaTerminator.Return([second.Id]));

        var optimized = Run(Artifact(entry, exit), SsaSemanticDescriptors.ArithmeticInt32);

        Assert.Multiple(() =>
        {
            Assert.That(
                optimized.Module.Functions.Single().Blocks
                    .SelectMany(static block => block.Instructions)
                    .OfType<SsaCall>()
                    .Select(static call => call.Id.Value),
                Is.EqualTo(new[] { "add.entry", "add.exit" }));
            AssertVerified(optimized, SsaSemanticDescriptors.ArithmeticInt32);
        });
    }

    [Test]
    public void Run_WhenEliminatedResultHasDominatedUse_RewritesUseInLaterBlock()
    {
        var left = Value("left");
        var right = Value("right");
        var first = Value("first");
        var duplicate = Value("duplicate");
        var entry = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Const("left", left, 2),
                Const("right", right, 3),
                Call("add.first", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], first),
                Call("add.duplicate", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], duplicate)
            ],
            terminator: SsaTerminator.Jump(new SsaBlockId("exit")));
        var exit = new SsaBlock(
            new SsaBlockId("exit"),
            terminator: SsaTerminator.Return([duplicate.Id]));

        var optimized = Run(Artifact(entry, exit), SsaSemanticDescriptors.ArithmeticInt32);
        var optimizedExit = optimized.Module.Functions.Single().Blocks.Single(block => block.Id.Value == "exit");

        Assert.Multiple(() =>
        {
            Assert.That(optimizedExit.Terminator!.Operands, Is.EqualTo(new[] { first.Id }));
            AssertVerified(optimized, SsaSemanticDescriptors.ArithmeticInt32);
        });
    }

    [Test]
    public void Run_WhenFunctionContainsUnknownInstructionShape_LeavesFunctionUntouched()
    {
        var left = Value("left");
        var right = Value("right");
        var first = Value("first");
        var duplicate = Value("duplicate");
        var opaqueResult = Value("opaque");
        var artifact = Artifact(new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Const("left", left, 2),
                Const("right", right, 3),
                Call("add.first", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], first),
                Call("add.duplicate", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], duplicate),
                new OpaqueInstruction(new SsaOperationId("opaque"), opaqueResult)
            ],
            terminator: SsaTerminator.Return([duplicate.Id])));

        var optimized = Run(artifact, SsaSemanticDescriptors.ArithmeticInt32);

        Assert.That(
            optimized.Module.Functions.Single().Blocks.Single().Instructions,
            Has.Count.EqualTo(5));
    }

    private static SsaArtifact Run(
        SsaArtifact artifact,
        SemanticDescriptorSet descriptors) =>
        new SsaLocalCommonSubexpressionEliminationPass(descriptors)
            .Run(artifact, new IrPipelineContext())
            .Artifact
            .As<SsaArtifact>();

    private static void AssertVerified(
        SsaArtifact artifact,
        SemanticDescriptorSet descriptors)
    {
        var verification = new StructuralSsaVerifier(
                SsaCoreDescriptors.CoreOperations,
                descriptors)
            .Verify(artifact, new IrPipelineContext());
        Assert.That(
            verification.IsSuccess,
            Is.True,
            string.Join("; ", verification.Diagnostics.Select(static diagnostic =>
                $"{diagnostic.Code}: {diagnostic.Message}")));
    }

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

    private static SsaValue Value(string id) =>
        new(new SsaValueId($"%{id}"), SsaTypes.Int32);

    private static SsaOperation Const(
        string id,
        SsaValue result,
        int value) =>
        SsaConstantMaterializer.Int32(new SsaOperationId(id), result, value);

    private static SsaCall Call(
        string id,
        CallableId callable,
        IEnumerable<SsaValueId> operands,
        SsaValue result) =>
        new(new SsaOperationId(id), callable, operands, [result]);

    private static SemanticDescriptorSet DescriptorSet(params CallableDescriptor[] callables) =>
        new(
            types:
            [
                new SemanticTypeDescriptor(
                    SsaSemanticTypes.Int32,
                    SemanticTypeTraits.Numeric |
                    SemanticTypeTraits.ValueObject |
                    SemanticTypeTraits.Immutable)
            ],
            callables: callables);

    private sealed class OpaqueInstruction(
        SsaOperationId id,
        SsaValue result) : ISsaInstruction
    {
        public SsaOperationId Id { get; } = id;

        public IReadOnlyList<SsaValueId> Operands { get; } = [];

        public IReadOnlyList<SsaValue> Results { get; } = [result];

        public SsaAttributeBag Attributes => SsaAttributeBag.Empty;
    }
}
