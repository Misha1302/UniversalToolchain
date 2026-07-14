using BasicCore.Builtins;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class IntrinsicTypeProcessorTests
{
    [Test]
    public void LoadF64_PushesDoubleType()
    {
        var instruction = IntrinsicInstructionFactory.CreateForCapability("load_f64", 1.5d);
        var stack = new List<Type>();

        IntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(double) }));
    }

    [Test]
    public void AddF64_ReplacesTwoDoubleOperands_WithDoubleResult()
    {
        var instruction = IntrinsicInstructionFactory.CreateForCapability("add_f64");
        var stack = new List<Type> { typeof(double), typeof(double) };

        IntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(double) }));
    }

    [Test]
    public void CmpLeF64_ReplacesTwoDoubleOperands_WithBooleanResult()
    {
        var instruction = IntrinsicInstructionFactory.CreateForCapability("cmp_le_f64");
        var stack = new List<Type> { typeof(double), typeof(double) };

        IntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(bool) }));
    }

    [Test]
    public void LoadLocalRef_PushesByRefType()
    {
        var instruction = IntrinsicInstructionFactory.CreateForCapability("load_local_ref", "x", typeof(int));
        var stack = new List<Type>();

        IntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(int).MakeByRefType() }));
    }

    [Test]
    public void ProcessTypes_LoadLocalRef_PushesByRefType()
    {
        var stack = new List<Type>();
        var instruction = IntrinsicInstructionFactory.CreateForCapability("load_local_ref", "x", typeof(int));

        IntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Has.Count.EqualTo(1));
        Assert.That(stack[0], Is.EqualTo(typeof(int).MakeByRefType()));
    }

    [Test]
    public void TypedComparisonIntrinsic_IsProjected_AndProducesBooleanResult()
    {
        var instruction = CreateTypedInstruction(
            BuiltinIntrinsicSymbols.Comparison.LessOrEqual,
            [IntrinsicTypeArgument.From(typeof(double))]);
        var stack = new List<Type> { typeof(double), typeof(double) };

        IntrinsicTypeProcessor.ProcessTypes(instruction, stack);

        Assert.That(stack, Is.EqualTo(new[] { typeof(bool) }));
    }

    [Test]
    public void SupportedCilIntrinsicNames_AreAcceptedBySharedProcessor()
    {
        var registry = new CilIntrinsicRegistry();

        foreach (var intrinsicName in registry.SupportedIntrinsics)
        {
            var (instruction, stack) = CreateScenario(intrinsicName);

            Assert.DoesNotThrow(() => IntrinsicTypeProcessor.ProcessTypes(instruction, stack), intrinsicName);
        }
    }

    [Test]
    public void MalformedIntrinsicPayload_ThrowsMeaningfulError()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, [new object()]);
        var stack = new List<Type>();

        var exception = Assert.Throws<InvalidOperationException>(() => IntrinsicTypeProcessor.ProcessTypes(instruction, stack));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("exactly one structured IntrinsicInvocation payload"));
    }

    private static (Instruction Instruction, List<Type> Stack) CreateScenario(string name)
    {
        if (name == "call C#")
            return (IntrinsicInstructionFactory.CreateForCapability("call C#", typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!), [typeof(int)]);

        if (name == "call C# ctor")
            return (IntrinsicInstructionFactory.CreateForCapability("call C# ctor", typeof(Uri).GetConstructor([typeof(string)])!), [typeof(string)]);

        if (name == "store_local")
            return (IntrinsicInstructionFactory.CreateForCapability("store_local", "x", typeof(int)), [typeof(int)]);

        if (name == "load_local")
            return (IntrinsicInstructionFactory.CreateForCapability("load_local", "x", typeof(int)), []);

        if (name == "load_local_ref")
            return (IntrinsicInstructionFactory.CreateForCapability("load_local_ref", "x", typeof(int)), []);

        if (name == "load_external")
            return (IntrinsicInstructionFactory.CreateForCapability("load_external", 0, typeof(int)), []);

        if (name == "store_external")
            return (IntrinsicInstructionFactory.CreateForCapability("store_external", 0), [typeof(int)]);

        if (name == "load_bool")
            return (IntrinsicInstructionFactory.CreateForCapability("load_bool", true), []);

        if (name == "boolean_and" || name == "boolean_or")
            return (IntrinsicInstructionFactory.CreateForCapability(name), [typeof(bool), typeof(bool)]);

        if (name == "boolean_not")
            return (IntrinsicInstructionFactory.CreateForCapability("boolean_not"), [typeof(bool)]);

        if (name.StartsWith("load_", StringComparison.Ordinal))
            return (IntrinsicInstructionFactory.CreateForCapability(name, 1), []);

        if (name.StartsWith("cmp_", StringComparison.Ordinal))
        {
            var operandType = ResolveOperandType(name);
            return (IntrinsicInstructionFactory.CreateForCapability(name), [operandType, operandType]);
        }

        if (name.StartsWith("add_", StringComparison.Ordinal)
            || name.StartsWith("sub_", StringComparison.Ordinal)
            || name.StartsWith("mul_", StringComparison.Ordinal)
            || name.StartsWith("div_", StringComparison.Ordinal))
        {
            var operandType = ResolveOperandType(name);
            return (IntrinsicInstructionFactory.CreateForCapability(name), [operandType, operandType]);
        }

        Thrower.InvalidOpEx($"Unsupported intrinsic test scenario '{name}'.");
        return default;
    }

    private static Type ResolveOperandType(string intrinsicName)
    {
        if (intrinsicName.EndsWith("_i32", StringComparison.Ordinal))
            return typeof(int);

        if (intrinsicName.EndsWith("_i64", StringComparison.Ordinal))
            return typeof(long);

        if (intrinsicName.EndsWith("_f32", StringComparison.Ordinal))
            return typeof(float);

        if (intrinsicName.EndsWith("_f64", StringComparison.Ordinal))
            return typeof(double);

        if (intrinsicName.EndsWith("_decimal", StringComparison.Ordinal))
            return typeof(decimal);

        return Thrower.InvalidOpEx<Type>($"Unsupported intrinsic operand suffix in '{intrinsicName}'.");
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