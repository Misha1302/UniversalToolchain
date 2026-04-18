using BasicCore.Builtins;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class IntrinsicTypeProcessorCoverageTests
{
    [Test]
    public void SharedProcessor_HandlesRepresentativeIntrinsicSurface_WithoutLegacyShim()
    {
        foreach (var scenario in GetRepresentativeScenarios())
        {
            var stack = scenario.InitialStack.ToList();

            Assert.DoesNotThrow(() => IntrinsicTypeProcessor.ProcessTypes(scenario.Instruction, stack), scenario.Name);
            Assert.That(stack, Is.EqualTo(scenario.ExpectedStack), scenario.Name);
        }
    }

    [Test]
    public void SharedProcessor_ThrowsMeaningfulError_ForUnknownIntrinsicName()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["unknown_intrinsic"]);
        var stack = new List<Type>();

        var exception = Assert.Throws<InvalidOperationException>(() => IntrinsicTypeProcessor.ProcessTypes(instruction, stack));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("unknown_intrinsic"));
    }

    private static IReadOnlyList<(string Name, Instruction Instruction, IReadOnlyList<Type> InitialStack, IReadOnlyList<Type> ExpectedStack)> GetRepresentativeScenarios() =>
    [
        ("load_i32", new Instruction(UOpCode.Intrinsic, ["load_i32", 1]), [], [typeof(int)]),
        ("load_f64", new Instruction(UOpCode.Intrinsic, ["load_f64", 1.5d]), [], [typeof(double)]),
        ("boolean_not", new Instruction(UOpCode.Intrinsic, ["boolean_not"]), [typeof(bool)], [typeof(bool)]),
        ("boolean_and", new Instruction(UOpCode.Intrinsic, ["boolean_and"]), [typeof(bool), typeof(bool)], [typeof(bool)]),
        ("add_i32", new Instruction(UOpCode.Intrinsic, ["add_i32"]), [typeof(int), typeof(int)], [typeof(int)]),
        ("mul_f64", new Instruction(UOpCode.Intrinsic, ["mul_f64"]), [typeof(double), typeof(double)], [typeof(double)]),
        ("cmp_le_f64", new Instruction(UOpCode.Intrinsic, ["cmp_le_f64"]), [typeof(double), typeof(double)], [typeof(bool)]),
        ("load_local", new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]), [], [typeof(int)]),
        ("load_local_ref", new Instruction(UOpCode.Intrinsic, ["load_local_ref", "x", typeof(int)]), [], [typeof(int).MakeByRefType()]),
        ("store_local", new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]), [typeof(int)], []),
        ("load_external", new Instruction(UOpCode.Intrinsic, ["load_external", 0, typeof(int)]), [], [typeof(int)]),
        ("store_external", new Instruction(UOpCode.Intrinsic, ["store_external", 0]), [typeof(int)], []),
        ("typed_cmp_le_f64", CreateTypedInstruction(BuiltinIntrinsicSymbols.Comparison.LessOrEqual, [IntrinsicTypeArgument.From(typeof(double))]), [typeof(double), typeof(double)], [typeof(bool)]),
        ("typed_add_f64", CreateTypedInstruction(BuiltinIntrinsicSymbols.Arithmetic.Add, [IntrinsicTypeArgument.From(typeof(double))]), [typeof(double), typeof(double)], [typeof(double)])
    ];

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