using System.Reflection;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;

namespace Tests.Intrinsics;

[TestFixture]
public class IntrinsicInstructionLegacyProjectorTests
{
    [Test]
    public void TryProject_TypedBooleanIntrinsic_ProjectsToLegacyName()
    {
        var instruction = CreateTypedInstruction(BuiltinIntrinsicSymbols.Boolean.Not);

        var success = TryProject(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "boolean_not" }));
    }

    [Test]
    public void TryProject_TypedArithmeticIntrinsic_ProjectsToTypedLegacyName()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Arithmetic.Add,
            [IntrinsicTypeArgument.From(typeof(double))]);

        var success = TryProject(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "add_f64" }));
    }

    [Test]
    public void TryProject_TypedComparisonIntrinsic_ProjectsToTypedLegacyName()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Comparison.LessOrEqual,
            [IntrinsicTypeArgument.From(typeof(double))]);

        var success = TryProject(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "cmp_le_f64" }));
    }

    [Test]
    public void TryProject_TypedLoadConst_ProjectsToLegacyNameAndDataOperand()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Core.LoadConst,
            [IntrinsicTypeArgument.From(typeof(double))],
            [12.5d]);

        var success = TryProject(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "load_f64", 12.5d }));
    }

    [Test]
    public void TryProject_TypedLoadLocal_ProjectsToLegacyShape()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Storage.LoadLocal,
            [IntrinsicTypeArgument.From(typeof(int))],
            ["value"]);

        var success = TryProject(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "load_local", "value", typeof(int) }));
    }

    [Test]
    public void TryProject_TypedLoadLocalRef_ProjectsToLegacyShape()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Storage.LoadLocalRef,
            [IntrinsicTypeArgument.From(typeof(int))],
            ["value"]);

        var success = TryProject(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "load_local_ref", "value", typeof(int) }));
    }

    [Test]
    public void TryProject_TypedCallCSharp_ProjectsToLegacyShape()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)]);
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Core.CallCSharp,
            dataOperands: [method]);

        var success = TryProject(instruction, out var projectedInstruction);

        Assert.That(success, Is.True);
        Assert.That(projectedInstruction.Operands, Is.EqualTo(new object?[] { "call C#", method }));
    }

    [Test]
    public void TryProject_MalformedTypedPayload_ReturnsFalse()
    {
        var instruction = CreateTypedInstruction(BuiltinIntrinsicSymbols.Core.LoadExternal);

        var success = TryProject(instruction, out _);

        Assert.That(success, Is.False);
    }


    private static bool TryProject(Instruction instruction, out Instruction projectedInstruction)
    {
        var projectorType = typeof(IntrinsicInvocation).Assembly
            .GetType("UniversalToolchain.Intrinsics.Legacy.IntrinsicInstructionLegacyProjector", throwOnError: true)!;
        var method = projectorType.GetMethod("TryProject", BindingFlags.Public | BindingFlags.Static)!;

        var args = new object?[] { instruction, null };
        var success = (bool)(method.Invoke(null, args) ?? false);
        projectedInstruction = args[1] is Instruction projected ? projected : default!;
        return success;
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
