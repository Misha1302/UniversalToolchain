using System.Reflection;

namespace Tests.Backends;

[TestFixture]
public sealed class ManagedCallContractRegressionTests
{
    [Test]
    public void InterpreterState_PreservesPublicValueStackContract()
    {
        var property = typeof(InterpreterState).GetProperty(nameof(InterpreterState.ValueStack));

        Assert.Multiple(() =>
        {
            Assert.That(property, Is.Not.Null);
            Assert.That(property!.GetMethod!.IsPublic, Is.True);
            Assert.That(property.PropertyType, Is.EqualTo(typeof(Stack<object>)));
        });
    }

    [Test]
    public void Interpreter_PreservesDeclaredTypeForNullDuringGenericResolution()
    {
        var method = typeof(ManagedCallContractRegressionTests)
            .GetMethod(nameof(GetTypeName), BindingFlags.NonPublic | BindingFlags.Static)!;
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [new AirConstant(typeof(string), null)]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", method));

        var result = new InterpreterImpl().Execute(ir, new ExecutionEnvironment([]));

        Assert.That(result, Is.EqualTo(nameof(String)));
    }

    [Test]
    public void NeutralManagedCallDescriptor_IsAcceptedByInterpreterAndCil()
    {
        var descriptor = new TestDescriptor(
            typeof(ManagedCallContractRegressionTests).GetMethod(nameof(AddOne), BindingFlags.NonPublic | BindingFlags.Static)!,
            ManagedCallReceiverKind.Static,
            null);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [41]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", descriptor));

        var interpreterResult = new InterpreterImpl().Execute(ir, new ExecutionEnvironment([]));
        var output = new AbstractMethodsCompilerImpl().Compile(ir, new CompilationInput { SourceText = string.Empty });
        var cilResult = new DynamicMethodExecutor().Execute(output, new ExecutionEnvironment([]));

        Assert.Multiple(() =>
        {
            Assert.That(interpreterResult, Is.EqualTo(42));
            Assert.That(cilResult, Is.EqualTo(42));
        });
    }

    [Test]
    public void ManagedCall_StringToIntegerConversion_HasBackendParity()
    {
        var method = typeof(ManagedCallContractRegressionTests)
            .GetMethod(nameof(IdentityInt), BindingFlags.NonPublic | BindingFlags.Static)!;
        var ir = BuildIr(
            new Instruction(UOpCode.Push, ["42"]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", method));

        var interpreterResult = new InterpreterImpl().Execute(ir, new ExecutionEnvironment([]));
        var output = new AbstractMethodsCompilerImpl().Compile(ir, new CompilationInput { SourceText = string.Empty });
        var cilResult = new DynamicMethodExecutor().Execute(output, new ExecutionEnvironment([]));

        Assert.Multiple(() =>
        {
            Assert.That(interpreterResult, Is.EqualTo(42));
            Assert.That(cilResult, Is.EqualTo(42));
        });
    }

    [TestCase("not-an-int", RuntimeValueConversionFailureKind.InvalidFormat)]
    [TestCase("999999999999999999999999999", RuntimeValueConversionFailureKind.Overflow)]
    public void ManagedCall_ConversionFailureKind_HasBackendParity(
        string value,
        RuntimeValueConversionFailureKind expectedKind)
    {
        var method = typeof(ManagedCallContractRegressionTests)
            .GetMethod(nameof(IdentityInt), BindingFlags.NonPublic | BindingFlags.Static)!;
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [value]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", method));

        var interpreterFailure = Assert.Catch(() =>
            new InterpreterImpl().Execute(ir, new ExecutionEnvironment([])));
        var output = new AbstractMethodsCompilerImpl().Compile(ir, new CompilationInput { SourceText = string.Empty });
        var cilFailure = Assert.Catch(() =>
            new DynamicMethodExecutor().Execute(output, new ExecutionEnvironment([])));

        Assert.Multiple(() =>
        {
            Assert.That(FindConversionFailure(interpreterFailure!), Is.EqualTo(expectedKind));
            Assert.That(FindConversionFailure(cilFailure!), Is.EqualTo(expectedKind));
        });
    }

    [Test]
    public void ExecutionScopedProviderCall_WithArguments_HasBackendParity()
    {
        var descriptor = new TestDescriptor(
            typeof(ArgumentProvider).GetMethod(nameof(ArgumentProvider.AddBase))!,
            ManagedCallReceiverKind.ExecutionScopedProvider,
            typeof(ArgumentProvider));
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [42]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", descriptor));
        var interpreterEnvironment = new ExecutionEnvironment([], allowedRuntimeProviderTypes: [typeof(ArgumentProvider)]);
        var cilEnvironment = new ExecutionEnvironment([], allowedRuntimeProviderTypes: [typeof(ArgumentProvider)]);

        var interpreterResult = new InterpreterImpl().Execute(ir, interpreterEnvironment);
        var output = new AbstractMethodsCompilerImpl().Compile(ir, new CompilationInput { SourceText = string.Empty });
        var cilResult = new DynamicMethodExecutor().Execute(output, cilEnvironment);

        Assert.Multiple(() =>
        {
            Assert.That(interpreterResult, Is.EqualTo(47));
            Assert.That(cilResult, Is.EqualTo(47));
        });
    }

    private static string GetTypeName<T>(T value) => typeof(T).Name;
    private static int AddOne(int value) => value + 1;
    private static int IdentityInt(int value) => value;

    private static RuntimeValueConversionFailureKind? FindConversionFailure(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is RuntimeValueConversionException conversion)
                return conversion.FailureKind;
        }

        return null;
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private sealed record TestDescriptor(
        MethodInfo Method,
        ManagedCallReceiverKind ReceiverKind,
        Type? ExecutionScopedProviderType) : IManagedCallDescriptor;

    private sealed class ArgumentProvider
    {
        public int AddBase(int value) => value + 5;
    }
}
