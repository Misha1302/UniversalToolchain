using BasicCore.Builtins;
using BasicCore.Legacy;

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
            BuiltinIntrinsicSymbols.Core.LoadConst,
            [IntrinsicTypeArgument.From(typeof(int))],
            [42]);
        var instruction = IntrinsicInstructionFactory.Create(invocation);
        var stack = new List<Type>();
        var processor = new IntrinsicTypeStackProcessor(
            new IntrinsicCatalogBuilder().Build(
            [
                new CoreIntrinsicDescriptorProvider(new MethodCallTypeSemanticsResolver())
            ]),
            new IntrinsicTypeResolutionContext());

        InstructionTypeStackApplier.Apply(
            [instruction],
            stack,
            new InstructionIntrinsicReader(new ThrowingLegacyIntrinsicDecoder()),
            processor);

        Assert.That(stack, Is.EqualTo(new[] { typeof(int) }));
    }

    [Test]
    public void InstructionIntrinsicReader_ShouldUseTypedIntrinsicInvocation_WithoutLegacyDecoding()
    {
        var invocation = CreateInvocation(BuiltinIntrinsicSymbols.Boolean.Not);
        var instruction = IntrinsicInstructionFactory.Create(invocation);
        var reader = new InstructionIntrinsicReader(new ThrowingLegacyIntrinsicDecoder());

        var success = reader.TryRead(instruction, out var decodedInvocation);

        Assert.That(success, Is.True);
        Assert.That(decodedInvocation, Is.EqualTo(invocation));
    }

    [Test]
    public void InstructionIntrinsicReader_ShouldUseLegacyDecoder_AsFallback()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["boolean_not"]);
        var legacyDecoder = new RecordingLegacyIntrinsicDecoder(CreateInvocation(BuiltinIntrinsicSymbols.Boolean.Not));
        var reader = new InstructionIntrinsicReader(legacyDecoder);

        var success = reader.TryRead(instruction, out var invocation);

        Assert.That(success, Is.True);
        Assert.That(invocation.Symbol, Is.EqualTo(BuiltinIntrinsicSymbols.Boolean.Not));
        Assert.That(legacyDecoder.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void InstructionTypeStackApplier_ShouldFailClearly_WhenIntrinsicCannotBeRead()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["unknown_intrinsic"]);
        var stack = new List<Type>();
        var processor = new IntrinsicTypeStackProcessor(
            new IntrinsicCatalogBuilder().Build(
            [
                new CoreIntrinsicDescriptorProvider(new MethodCallTypeSemanticsResolver())
            ]),
            new IntrinsicTypeResolutionContext());

        var exception = Assert.Throws<InvalidOperationException>(() => InstructionTypeStackApplier.Apply(
            [instruction],
            stack,
            new InstructionIntrinsicReader(new LegacyIntrinsicDecoder()),
            processor));

        Assert.That(exception!.Message, Does.Contain("Unable to read intrinsic invocation"));
        Assert.That(exception.Message, Does.Contain("unknown_intrinsic"));
    }

    private static IntrinsicInvocation CreateInvocation(
        IntrinsicSymbol? symbol = null,
        IReadOnlyList<IntrinsicTypeArgument>? typeArguments = null,
        IReadOnlyList<object?>? dataOperands = null) =>
        new(
            symbol ?? BuiltinIntrinsicSymbols.Boolean.Not,
            typeArguments ?? [],
            dataOperands ?? []);

    private sealed class ThrowingLegacyIntrinsicDecoder : ILegacyIntrinsicDecoder
    {
        public bool TryDecode(Instruction instruction, out IntrinsicInvocation invocation) => throw new AssertionException("Legacy decoder should not be used for typed intrinsic instructions.");
    }

    private sealed class RecordingLegacyIntrinsicDecoder(IntrinsicInvocation invocation) : ILegacyIntrinsicDecoder
    {
        public int CallCount { get; private set; }

        public bool TryDecode(Instruction instruction, out IntrinsicInvocation decodedInvocation)
        {
            CallCount++;
            decodedInvocation = invocation;
            return true;
        }
    }
}