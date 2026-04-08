using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Legacy;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class LegacyIntrinsicDecoderTests
{
    private static readonly ILegacyIntrinsicDecoder Decoder = new LegacyIntrinsicDecoder();

    [Test]
    public void TryDecode_ShouldDecodeArithmeticIntrinsic()
    {
        var instruction = CreateIntrinsicInstruction("add_i32", 1, 2);

        var success = Decoder.TryDecode(instruction, out var invocation);

        Assert.That(success, Is.True);
        AssertInvocation(
            invocation,
            BuiltinIntrinsicSymbols.Arithmetic.Add,
            [typeof(int)],
            [1, 2]);
    }

    [Test]
    public void TryDecode_ShouldDecodeComparisonIntrinsic()
    {
        var instruction = CreateIntrinsicInstruction("cmp_ge_f64", 1.0, 2.0);

        var success = Decoder.TryDecode(instruction, out var invocation);

        Assert.That(success, Is.True);
        AssertInvocation(
            invocation,
            BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual,
            [typeof(double)],
            [1.0, 2.0]);
    }

    [Test]
    public void TryDecode_ShouldDecodeLoadConstIntrinsic()
    {
        var instruction = CreateIntrinsicInstruction("load_decimal", 12.5m);

        var success = Decoder.TryDecode(instruction, out var invocation);

        Assert.That(success, Is.True);
        AssertInvocation(
            invocation,
            BuiltinIntrinsicSymbols.Core.LoadConst,
            [typeof(decimal)],
            [12.5m]);
    }

    [Test]
    public void TryDecode_ShouldDecodeLoadExternalIntrinsic()
    {
        var instruction = CreateIntrinsicInstruction("load_external", 3, typeof(long));

        var success = Decoder.TryDecode(instruction, out var invocation);

        Assert.That(success, Is.True);
        AssertInvocation(
            invocation,
            BuiltinIntrinsicSymbols.Core.LoadExternal,
            [typeof(long)],
            [3]);
    }

    [Test]
    public void TryDecode_ShouldDecodeCallCSharpIntrinsic()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!;
        var instruction = CreateIntrinsicInstruction("call C#", method);

        var success = Decoder.TryDecode(instruction, out var invocation);

        Assert.That(success, Is.True);
        AssertInvocation(
            invocation,
            BuiltinIntrinsicSymbols.Core.CallCSharp,
            [],
            [method]);
    }

    [Test]
    public void TryDecode_ShouldDecodeBooleanIntrinsic()
    {
        var instruction = CreateIntrinsicInstruction("boolean_not");

        var success = Decoder.TryDecode(instruction, out var invocation);

        Assert.That(success, Is.True);
        AssertInvocation(
            invocation,
            BuiltinIntrinsicSymbols.Boolean.Not,
            [],
            Array.Empty<object>());
    }

    [Test]
    public void TryDecode_ShouldDecodeLoadLocalRefIntrinsic()
    {
        var instruction = CreateIntrinsicInstruction("load_local_ref", "value", typeof(float));

        var success = Decoder.TryDecode(instruction, out var invocation);

        Assert.That(success, Is.True);
        AssertInvocation(
            invocation,
            BuiltinIntrinsicSymbols.Storage.LoadLocalRef,
            [typeof(float)],
            ["value", typeof(float)]);
    }

    [Test]
    public void TryDecode_ShouldReturnFalse_ForUnknownIntrinsic()
    {
        var instruction = CreateIntrinsicInstruction("unknown_intrinsic", 1, 2, 3);

        var success = Decoder.TryDecode(instruction, out var invocation);

        Assert.That(success, Is.False);
        Assert.That(invocation, Is.EqualTo(default(IntrinsicInvocation)));
    }

    private static Instruction CreateIntrinsicInstruction(string name, params object[] operands)
    {
        return new Instruction(UOpCode.Intrinsic, [name, .. operands]);
    }

    private static void AssertInvocation(
        IntrinsicInvocation invocation,
        IntrinsicSymbol expectedSymbol,
        IReadOnlyList<Type> expectedTypes,
        IReadOnlyList<object?> expectedDataOperands)
    {
        Assert.That(invocation.Symbol, Is.EqualTo(expectedSymbol));
        Assert.That(invocation.TypeArguments.Select(argument => argument.RuntimeType), Is.EqualTo(expectedTypes));
        Assert.That(invocation.DataOperands, Is.EqualTo(expectedDataOperands));
    }
}
