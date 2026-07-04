using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaModelTests
{
    [Test]
    public void SsaArtifact_ShouldExposeSsaIrKind()
    {
        var module = new SsaModule(
            new SsaModuleId("test.module"),
            [new SsaFunction(new SsaFunctionId("main"), new SsaBlockId("entry"), [new SsaBlock(new SsaBlockId("entry"), terminator: SsaTerminator.Return())])]);

        var artifact = new SsaArtifact(module);

        Assert.That(artifact.Kind, Is.EqualTo(SsaIrKinds.Ssa));
    }

    [Test]
    public void DescriptorSet_ShouldBeDeterministicAndRejectDuplicates()
    {
        var add = new SsaOpDescriptor(TestOperations.Add, [SsaTypes.Int32, SsaTypes.Int32], [SsaTypes.Int32]);
        var equal = new SsaOpDescriptor(TestOperations.Equal, [SsaTypes.Int32, SsaTypes.Int32], [SsaTypes.Bool]);

        var set = new SsaDescriptorSet([equal, add]);

        Assert.That(set.Values.Select(static x => x.Id), Is.EqualTo(new[] { TestOperations.Add, TestOperations.Equal }));
        Assert.That(
            () => new SsaDescriptorSet([add, add]),
            Throws.ArgumentException.With.Message.Contains("Duplicate SSA operation descriptor"));
    }

    [Test]
    public void AttributeBag_ShouldSnapshotInDeterministicOrder()
    {
        var first = new SsaAttribute(new SsaAttributeKey("ssa.semantic.a"), "1");
        var second = new SsaAttribute(new SsaAttributeKey("ssa.semantic.b"), "2");

        var bag = new SsaAttributeBag([second, first]);

        Assert.That(bag.Values.Select(static x => x.Key), Is.EqualTo(new[] { first.Key, second.Key }));
        Assert.That(bag.Contains(first.Key), Is.True);
    }

    [Test]
    public void Block_WithMixedInstructions_PreservesInstructionOrderAndCompatibilityViews()
    {
        var input = new SsaValue(new SsaValueId("%input"), SsaTypes.Int32);
        var callResult = new SsaValue(new SsaValueId("%call"), SsaTypes.Int32);
        var operationResult = new SsaValue(new SsaValueId("%operation"), SsaTypes.Int32);
        var call = new SsaCall(
            new SsaOperationId("call"),
            new CallableId("test.core.identity"),
            [input.Id],
            [callResult]);
        var operation = new SsaOperation(
            new SsaOperationId("operation"),
            TestOperations.Add,
            [callResult.Id, input.Id],
            [operationResult]);

        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions: [call, operation],
            terminator: SsaTerminator.Return([operationResult.Id]));

        Assert.Multiple(() =>
        {
            Assert.That(block.Instructions, Is.EqualTo(new ISsaInstruction[] { call, operation }));
            Assert.That(block.Calls, Is.EqualTo(new[] { call }));
            Assert.That(block.Operations, Is.EqualTo(new[] { operation }));
        });
    }

    private static class TestOperations
    {
        public static SsaOpId Add { get; } = new("test.add");

        public static SsaOpId Equal { get; } = new("test.eq");
    }
}
