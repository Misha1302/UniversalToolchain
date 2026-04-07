using SettableGettableModule.Core;
using Tests.Infrastructure;

namespace Tests.Backends;

[TestFixture]
public class InterpreterBackendIrExecutionTests
{
    [Test]
    public void ConditionalJump_PreservesExpectedStackState()
    {
        var targetLabel = Guid.NewGuid();
        var endLabel = Guid.NewGuid();

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [false]),
            new Instruction(UOpCode.JmpIf, [targetLabel]),
            new Instruction(UOpCode.Push, [10]),
            new Instruction(UOpCode.Jmp, [endLabel]),
            new Instruction(UOpCode.Label, [targetLabel]),
            new Instruction(UOpCode.Push, [20]),
            new Instruction(UOpCode.Label, [endLabel])
        );

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ConstructorAndInstanceCalls_WorkInSingleFlow()
    {
        var ctor = typeof(StringBuilder).GetConstructor([typeof(string)]);
        var append = typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(string)]);
        var toString = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, ["ab"]),
            new Instruction(UOpCode.Intrinsic, ["call C# ctor", ctor!]),
            new Instruction(UOpCode.Push, ["cd"]),
            new Instruction(UOpCode.Intrinsic, ["call C#", append!]),
            new Instruction(UOpCode.Intrinsic, ["call C#", toString!])
        );

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.EqualTo("abcd"));
    }

    [Test]
    public void GenericMethodCall_ResolvesTypesFromStack()
    {
        var genericEcho = typeof(InterpreterBackendIrExecutionTests)
            .GetMethod(nameof(Echo), BindingFlags.NonPublic | BindingFlags.Static);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [123]),
            new Instruction(UOpCode.Intrinsic, ["call C#", genericEcho!])
        );

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void StaticCall_PerformsConvertibleArgumentCasting()
    {
        var sqrtMethod = typeof(Math).GetMethod(nameof(Math.Sqrt), [typeof(double)]);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [9]),
            new Instruction(UOpCode.Intrinsic, ["call C#", sqrtMethod!])
        );

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.EqualTo(3d).Within(1e-9));
    }

    [Test]
    public void InstanceCallWithoutInstance_ThrowsMeaningfulException()
    {
        var toString = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes);
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["call C#", toString!]));

        var exception = Assert.Throws<RuntimeExecutionException>(() => ExecuteInInterpreter(ir));

        Assert.That(exception!.Message, Does.Contain("instance is missing"));
    }

    [Test]
    public void CallWithoutEnoughArguments_ThrowsMeaningfulException()
    {
        var compareMethod = typeof(Math).GetMethod(nameof(Math.Max), [typeof(int), typeof(int)]);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["call C#", compareMethod!])
        );

        var exception = Assert.Throws<RuntimeExecutionException>(() => ExecuteInInterpreter(ir));

        Assert.That(exception!.Message, Does.Contain("not enough arguments"));
    }

    [Test]
    public void UnknownIntrinsic_ThrowsMeaningfulException()
    {
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["unknown intrinsic"]));

        var exception = Assert.Throws<RuntimeExecutionException>(() => ExecuteInInterpreter(ir));

        Assert.That(exception!.Message, Does.Contain("unknown intrinsic"));
    }

    [Test]
    public void VariablesContainerGet_UsesDeclaredExternalBindingLayoutSlot()
    {
        var getMethod = typeof(VariablesContainer<int>).GetMethod(nameof(VariablesContainer<int>.Get), [typeof(string)]);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, ["target"]),
            new Instruction(UOpCode.Intrinsic, ["call C#", getMethod!]));

        var environment = new ExecutionEnvironment(
        [
            new ExternalBinding { Name = "other", Type = typeof(int), Value = 11, Kind = ExternalBindingKind.Variable },
            new ExternalBinding { Name = "target", Type = typeof(int), Value = 42, Kind = ExternalBindingKind.Variable }
        ]);

        var result = ExecuteInInterpreter(ir, environment);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void VariablesContainerGet_NameMissingInLayout_IsTreatedAsLocalVariable()
    {
        using var _ = GlobalTestStateScope.Create();

        const string key = "local_only";
        VariablesContainer<int>.Set(key, 7);

        var getMethod = typeof(VariablesContainer<int>).GetMethod(nameof(VariablesContainer<int>.Get), [typeof(string)]);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [key]),
            new Instruction(UOpCode.Intrinsic, ["call C#", getMethod!]));

        var environment = new ExecutionEnvironment(
        [
            new ExternalBinding { Name = "declared", Type = typeof(int), Value = 999, Kind = ExternalBindingKind.Variable }
        ]);

        var result = ExecuteInInterpreter(ir, environment);

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void ManualStackManipulationWithDropsAndNestedBranches_RemainsDeterministic()
    {
        var outerTrue = Guid.NewGuid();
        var innerTrue = Guid.NewGuid();
        var endInner = Guid.NewGuid();

        var combineMethod = typeof(InterpreterBackendIrExecutionTests)
            .GetMethod(nameof(CombineDigits), BindingFlags.NonPublic | BindingFlags.Static);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Push, [777]),
            new Instruction(UOpCode.Drop),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [outerTrue]),
            new Instruction(UOpCode.Push, [999]),
            new Instruction(UOpCode.Label, [outerTrue]),
            new Instruction(UOpCode.Push, [false]),
            new Instruction(UOpCode.JmpIf, [innerTrue]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Jmp, [endInner]),
            new Instruction(UOpCode.Label, [innerTrue]),
            new Instruction(UOpCode.Push, [9]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Label, [endInner]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!])
        );

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.EqualTo(123));
    }

    private static int CombineDigits(int acc, int nextDigit) => acc * 10 + nextDigit;

    private static T Echo<T>(T value) => value;

    private static object? ExecuteInInterpreter(IAbstractIR ir)
    {
        var interpreter = new InterpreterImpl();
        return interpreter.Execute(ir, new ExecutionEnvironment([]));
    }

    private static object? ExecuteInInterpreter(IAbstractIR ir, IExecutionEnvironment environment)
    {
        var interpreter = new InterpreterImpl();
        return interpreter.Execute(ir, environment);
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }
}
