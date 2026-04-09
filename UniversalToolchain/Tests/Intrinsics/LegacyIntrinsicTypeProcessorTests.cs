using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Legacy;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class LegacyIntrinsicTypeProcessorTests
{
    [Test]
    public void LoadF64_PushesDoubleType()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["load_f64", 1.5d]);
        var stack = new List<Type>();

        LegacyIntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(double) }));
    }

    [Test]
    public void AddF64_ReplacesTwoDoubleOperands_WithDoubleResult()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["add_f64"]);
        var stack = new List<Type> { typeof(double), typeof(double) };

        LegacyIntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(double) }));
    }

    [Test]
    public void CmpLeF64_ReplacesTwoDoubleOperands_WithBooleanResult()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["cmp_le_f64"]);
        var stack = new List<Type> { typeof(double), typeof(double) };

        LegacyIntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(bool) }));
    }

    [Test]
    public void LoadLocalRef_PushesByRefType()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["load_local_ref", "x", typeof(int)]);
        var stack = new List<Type>();

        LegacyIntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(int).MakeByRefType() }));
    }

    [Test]
    public void TypedComparisonIntrinsic_IsProjected_AndProducesBooleanResult()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Comparison.LessOrEqual,
            [IntrinsicTypeArgument.From(typeof(double))]);
        var stack = new List<Type> { typeof(double), typeof(double) };

        LegacyIntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(bool) }));
    }

    [Test]
    public void SupportedCilIntrinsicNames_AreAcceptedBySharedProcessor()
    {
        foreach (var scenario in GetSupportedIntrinsicScenarios())
        {
            var stack = scenario.StackFactory();

            Assert.DoesNotThrow(() => LegacyIntrinsicTypeProcessor.ProcessTypes(scenario.Instruction, stack), scenario.Name);
        }
    }

    private static IReadOnlyList<(string Name, Instruction Instruction, Func<List<Type>> StackFactory)> GetSupportedIntrinsicScenarios()
    {
        return
        [
            ("load_i32", new Instruction(UOpCode.Intrinsic, ["load_i32", 1]), () => []),
            ("load_f64", new Instruction(UOpCode.Intrinsic, ["load_f64", 1.5d]), () => []),
            ("load_decimal", new Instruction(UOpCode.Intrinsic, ["load_decimal", 1.5m]), () => []),
            ("boolean_not", new Instruction(UOpCode.Intrinsic, ["boolean_not"]), () => [typeof(bool)]),
            ("boolean_and", new Instruction(UOpCode.Intrinsic, ["boolean_and"]), () => [typeof(bool), typeof(bool)]),
            ("add_i32", new Instruction(UOpCode.Intrinsic, ["add_i32"]), () => [typeof(int), typeof(int)]),
            ("mul_f64", new Instruction(UOpCode.Intrinsic, ["mul_f64"]), () => [typeof(double), typeof(double)]),
            ("cmp_le_f64", new Instruction(UOpCode.Intrinsic, ["cmp_le_f64"]), () => [typeof(double), typeof(double)]),
            ("load_local", new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]), () => []),
            ("load_local_ref", new Instruction(UOpCode.Intrinsic, ["load_local_ref", "x", typeof(int)]), () => []),
            ("store_local", new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]), () => [typeof(int)]),
            ("load_external", new Instruction(UOpCode.Intrinsic, ["load_external", 0, typeof(int)]), () => []),
            ("store_external", new Instruction(UOpCode.Intrinsic, ["store_external", 0]), () => [typeof(int)])
        ];
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
