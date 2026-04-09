using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;

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
}
