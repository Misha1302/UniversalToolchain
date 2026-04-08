using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Legacy;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class InstructionIntrinsicExtensionsTests
{
    [Test]
    public void TypedIntrinsicInstruction_ShouldRoundtripInvocation()
    {
        var invocation = CreateInvocation();
        var instruction = IntrinsicInstructionFactory.Create(invocation);

        var success = instruction.TryGetTypedIntrinsicInvocation(out var decodedInvocation);

        Assert.That(success, Is.True);
        Assert.That(instruction.IsTypedIntrinsicInvocation(), Is.True);
        Assert.That(decodedInvocation, Is.EqualTo(invocation));
    }

    [Test]
    public void TryGetTypedIntrinsicInvocation_ShouldReturnFalse_ForNonIntrinsicOpcode()
    {
        var instruction = new Instruction(UOpCode.Push, [CreateInvocation()]);

        var success = instruction.TryGetTypedIntrinsicInvocation(out var invocation);

        Assert.That(success, Is.False);
        Assert.That(invocation, Is.EqualTo(default(IntrinsicInvocation)));
        Assert.That(instruction.IsTypedIntrinsicInvocation(), Is.False);
    }

    [Test]
    public void TryGetTypedIntrinsicInvocation_ShouldReturnFalse_ForUnexpectedOperandCount()
    {
        var invocation = CreateInvocation();
        var instruction = new Instruction(UOpCode.Intrinsic, [invocation, "extra"]);

        var success = instruction.TryGetTypedIntrinsicInvocation(out var decodedInvocation);

        Assert.That(success, Is.False);
        Assert.That(decodedInvocation, Is.EqualTo(default(IntrinsicInvocation)));
    }

    [Test]
    public void TryGetTypedIntrinsicInvocation_ShouldReturnFalse_ForLegacyStringOperands()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["boolean_not"]);

        var success = instruction.TryGetTypedIntrinsicInvocation(out var invocation);

        Assert.That(success, Is.False);
        Assert.That(invocation, Is.EqualTo(default(IntrinsicInvocation)));
        Assert.That(instruction.IsTypedIntrinsicInvocation(), Is.False);
    }

    [Test]
    public void IntrinsicInstructionFactory_ShouldCreateCanonicalTypedShape()
    {
        var invocation = CreateInvocation();

        var instruction = IntrinsicInstructionFactory.Create(invocation);

        Assert.That(instruction.UOpCode, Is.EqualTo(UOpCode.Intrinsic));
        Assert.That(instruction.Operands.Count, Is.EqualTo(1));
        Assert.That(instruction.Operands[0], Is.SameAs(invocation));
    }

    [Test]
    public void InstructionTypeStackApplier_ShouldUseTypedIntrinsicInvocation_WithoutLegacyDecoding()
    {
        var invocation = CreateInvocation(
            symbol: BuiltinIntrinsicSymbols.Core.LoadConst,
            typeArguments: [IntrinsicTypeArgument.From(typeof(int))],
            dataOperands: [42]);
        var instruction = IntrinsicInstructionFactory.Create(invocation);
        var stack = new List<Type>();
        var processor = new IntrinsicTypeStackProcessor(
            new IntrinsicCatalogBuilder()
                .AddProvider(new CoreIntrinsicDescriptorProvider())
                .Build(),
            new IntrinsicTypeResolutionContext());

        InstructionTypeStackApplier.Apply(
            [instruction],
            stack,
            new ThrowingLegacyIntrinsicDecoder(),
            processor);

        Assert.That(stack, Is.EqualTo(new[] { typeof(int) }));
    }

    private static IntrinsicInvocation CreateInvocation(
        IntrinsicSymbol? symbol = null,
        IReadOnlyList<IntrinsicTypeArgument>? typeArguments = null,
        IReadOnlyList<object?>? dataOperands = null)
    {
        return new IntrinsicInvocation(
            symbol ?? BuiltinIntrinsicSymbols.Boolean.Not,
            typeArguments ?? [],
            dataOperands ?? []);
    }

    private sealed class ThrowingLegacyIntrinsicDecoder : ILegacyIntrinsicDecoder
    {
        public bool TryDecode(Instruction instruction, out IntrinsicInvocation invocation)
        {
            throw new AssertionException("Legacy decoder should not be used for typed intrinsic instructions.");
        }
    }
}
