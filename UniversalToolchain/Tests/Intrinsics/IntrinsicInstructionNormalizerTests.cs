using BasicCore.Builtins;

namespace Tests.Intrinsics;

[TestFixture]
public class IntrinsicInstructionNormalizerTests
{
    [Test]
    public void TryNormalize_TypedBooleanIntrinsic_ProjectsToLegacyName()
    {
        var instruction = CreateTypedInstruction(BuiltinIntrinsicSymbols.Boolean.Not);

        var success = IntrinsicInstructionNormalizer.TryNormalize(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "boolean_not" }));
    }

    [Test]
    public void TryNormalize_TypedArithmeticIntrinsic_ProjectsToTypedLegacyName()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Arithmetic.Add,
            [IntrinsicTypeArgument.From(typeof(double))]);

        var success = IntrinsicInstructionNormalizer.TryNormalize(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "add_f64" }));
    }

    [Test]
    public void TryNormalize_TypedComparisonIntrinsic_ProjectsToTypedLegacyName()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Comparison.LessOrEqual,
            [IntrinsicTypeArgument.From(typeof(double))]);

        var success = IntrinsicInstructionNormalizer.TryNormalize(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "cmp_le_f64" }));
    }

    [Test]
    public void TryNormalize_TypedLoadConst_ProjectsToLegacyNameAndDataOperand()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Core.LoadConst,
            [IntrinsicTypeArgument.From(typeof(double))],
            [12.5d]);

        var success = IntrinsicInstructionNormalizer.TryNormalize(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "load_f64", 12.5d }));
    }

    [Test]
    public void TryNormalize_TypedLoadLocal_ProjectsToLegacyShape()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Storage.LoadLocal,
            [IntrinsicTypeArgument.From(typeof(int))],
            ["value"]);

        var success = IntrinsicInstructionNormalizer.TryNormalize(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "load_local", "value", typeof(int) }));
    }

    [Test]
    public void TryNormalize_TypedLoadLocalRef_ProjectsToLegacyShape()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Storage.LoadLocalRef,
            [IntrinsicTypeArgument.From(typeof(int))],
            ["value"]);

        var success = IntrinsicInstructionNormalizer.TryNormalize(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "load_local_ref", "value", typeof(int) }));
    }

    [Test]
    public void TryNormalize_TypedCallCSharp_ProjectsToLegacyShape()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)]);
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Core.CallCSharp,
            dataOperands: [method]);

        var success = IntrinsicInstructionNormalizer.TryNormalize(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "call C#", method }));
    }

    [Test]
    public void NormalizeOrThrow_MalformedTypedPayload_Throws()
    {
        var instruction = CreateTypedInstruction(BuiltinIntrinsicSymbols.Core.LoadExternal);

        var exception = Assert.Throws<InvalidOperationException>(() => IntrinsicInstructionNormalizer.NormalizeOrThrow(instruction));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Unsupported intrinsic instruction payload"));
    }

    [Test]
    public void NormalizeOrThrow_ForAlreadyNormalizedLoadLocalRef_IsIdempotent()
    {
        var instruction = Intrinsic("load_local_ref", "x", typeof(int));

        var normalized1 = IntrinsicInstructionNormalizer.NormalizeOrThrow(instruction);
        var normalized2 = IntrinsicInstructionNormalizer.NormalizeOrThrow(normalized1);

        Assert.Multiple(() =>
        {
            Assert.That(normalized2.UOpCode, Is.EqualTo(normalized1.UOpCode));
            Assert.That(normalized2.Operands.Count, Is.EqualTo(normalized1.Operands.Count));
            Assert.That(normalized2.Operands[0], Is.EqualTo("load_local_ref"));
            Assert.That(normalized2.Operands[1], Is.EqualTo("x"));
            Assert.That(normalized2.Operands[2], Is.EqualTo(typeof(int)));
        });
    }

    [Test]
    public void TryNormalize_UnknownIntrinsic_ReturnsFalse()
    {
        var instruction = Intrinsic("definitely_unknown_intrinsic");

        var ok = IntrinsicInstructionNormalizer.TryNormalize(instruction, out var normalized);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(normalized, Is.Null);
        });
    }

    [Test]
    public void NormalizeOrThrow_UnknownIntrinsic_Throws()
    {
        var instruction = Intrinsic("definitely_unknown_intrinsic");

        Assert.Throws<InvalidOperationException>(() => { _ = IntrinsicInstructionNormalizer.NormalizeOrThrow(instruction); });
    }

    private static Instruction CreateTypedInstruction(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument>? typeArguments = null,
        IReadOnlyList<object?>? dataOperands = null)
    {
        var invocation = new IntrinsicInvocation(
            symbol,
            typeArguments ?? [],
            dataOperands ?? []);

        return new Instruction(UOpCode.Intrinsic, [invocation]);
    }

    private static Instruction Intrinsic(string name, params object?[] args)
    {
        var operands = new List<object>(args.Length + 1) { name };
        for (var i = 0; i < args.Length; i++)
            operands.Add(args[i]!);

        return new Instruction(UOpCode.Intrinsic, operands);
    }
}