using BasicCilCompiler.Execution;
using BytecodeDynamicMethodsCompiler.Compilers;

namespace Tests.Infrastructure;

[TestFixture]
public class CompilerAndInterpreterResilienceTests
{
    [Test]
    public void Interpreter_ConditionalJump_PreservesExpectedStackState()
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
    public void Interpreter_ConstructorAndInstanceCalls_WorkInSingleFlow()
    {
        var ctor = typeof(StringBuilder).GetConstructor([typeof(string)]);
        var append = typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(string)]);
        var toString = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes);

        AssertReflectionMembersExist(ctor, append, toString);

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
    public void Interpreter_GenericMethodCall_ResolvesTypesFromStack()
    {
        var genericEcho = typeof(CompilerAndInterpreterResilienceTests)
            .GetMethod(nameof(Echo), BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(genericEcho, Is.Not.Null);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [123]),
            new Instruction(UOpCode.Intrinsic, ["call C#", genericEcho!])
        );

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void Interpreter_StaticCall_PerformsConvertibleArgumentCasting()
    {
        var sqrtMethod = typeof(Math).GetMethod(nameof(Math.Sqrt), [typeof(double)]);
        Assert.That(sqrtMethod, Is.Not.Null);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [9]),
            new Instruction(UOpCode.Intrinsic, ["call C#", sqrtMethod!])
        );

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.EqualTo(3d).Within(1e-9));
    }

    [Test]
    public void Interpreter_InstanceCallWithoutInstance_ThrowsMeaningfulException()
    {
        var toString = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes);
        Assert.That(toString, Is.Not.Null);

        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["call C#", toString!]));

        var exception = Assert.Throws<InvalidOperationException>(() => ExecuteInInterpreter(ir));

        Assert.That(exception!.Message, Does.Contain("instance is missing"));
    }

    [Test]
    public void Interpreter_CallWithoutEnoughArguments_ThrowsMeaningfulException()
    {
        var compareMethod = typeof(Math).GetMethod(nameof(Math.Max), [typeof(int), typeof(int)]);
        Assert.That(compareMethod, Is.Not.Null);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["call C#", compareMethod!])
        );

        var exception = Assert.Throws<InvalidOperationException>(() => ExecuteInInterpreter(ir));

        Assert.That(exception!.Message, Does.Contain("not enough arguments"));
    }

    [Test]
    public void Interpreter_UnknownIntrinsic_ThrowsMeaningfulException()
    {
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["unknown intrinsic"]));

        var exception = Assert.Throws<InvalidOperationException>(() => ExecuteInInterpreter(ir));

        Assert.That(exception!.Message, Does.Contain("unknown intrinsic"));
    }

    [Test]
    public void Compiler_LocalStoreAndLoad_WithStaticCall_ProducesCorrectResult()
    {
        var addOneMethod = typeof(CompilerAndInterpreterResilienceTests)
            .GetMethod(nameof(AddOne), BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(addOneMethod, Is.Not.Null);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [41]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["call C#", addOneMethod!])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Compiler_BranchWithStackMerge_InfersReturnTypeAndExecutesCorrectly()
    {
        var trueLabel = Guid.NewGuid();
        var endLabel = Guid.NewGuid();

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [trueLabel]),
            new Instruction(UOpCode.Push, ["left"]),
            new Instruction(UOpCode.Jmp, [endLabel]),
            new Instruction(UOpCode.Label, [trueLabel]),
            new Instruction(UOpCode.Push, ["right"]),
            new Instruction(UOpCode.Label, [endLabel])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo("right"));
    }

    [Test]
    public void Compiler_GenericMethodCall_ResolvesMethodViaReflection()
    {
        var genericEcho = typeof(CompilerAndInterpreterResilienceTests)
            .GetMethod(nameof(Echo), BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(genericEcho, Is.Not.Null);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, ["generic"]),
            new Instruction(UOpCode.Intrinsic, ["call C#", genericEcho!])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo("generic"));
    }

    [Test]
    public void Compiler_ConstructorAndInstanceCall_UsesReflectionMembersCorrectly()
    {
        var ctor = typeof(ReflectionTarget).GetConstructor([typeof(int)]);
        var increment = typeof(ReflectionTarget).GetMethod(nameof(ReflectionTarget.IncrementBy), [typeof(int)]);

        AssertReflectionMembersExist(ctor, increment);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [40]),
            new Instruction(UOpCode.Intrinsic, ["call C# ctor", ctor!]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C#", increment!])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Compiler_UnknownNumericLoaderIntrinsic_ThrowsInvalidOperationException()
    {
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["load_x128", 1]));

        Assert.Throws<InvalidOperationException>(() => CompileAndExecute(ir));
    }

    [Test]
    public void Interpreter_ManualStackManipulationWithDropsAndNestedBranches_RemainsDeterministic()
    {
        var outerTrue = Guid.NewGuid();
        var innerTrue = Guid.NewGuid();
        var endInner = Guid.NewGuid();

        var combineMethod = typeof(CompilerAndInterpreterResilienceTests)
            .GetMethod(nameof(CombineDigits), BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(combineMethod, Is.Not.Null);

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

    [Test]
    public void Compiler_DeepNestedConditionsWithSharedStackState_HandlesComplexControlFlow()
    {
        var branch1 = Guid.NewGuid();
        var branch2 = Guid.NewGuid();
        var branch3 = Guid.NewGuid();
        var afterInner = Guid.NewGuid();
        var finish = Guid.NewGuid();

        var combineMethod = typeof(CompilerAndInterpreterResilienceTests)
            .GetMethod(nameof(CombineDigits), BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(combineMethod, Is.Not.Null);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [branch1]),
            new Instruction(UOpCode.Push, [999]),
            new Instruction(UOpCode.Drop),
            new Instruction(UOpCode.Label, [branch1]),
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [branch2]),
            new Instruction(UOpCode.Jmp, [finish]),
            new Instruction(UOpCode.Label, [branch2]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Push, [false]),
            new Instruction(UOpCode.JmpIf, [branch3]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Jmp, [afterInner]),
            new Instruction(UOpCode.Label, [branch3]),
            new Instruction(UOpCode.Push, [8]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Label, [afterInner]),
            new Instruction(UOpCode.Push, [4]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Label, [finish])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(1234));
    }

    [Test]
    public void Compiler_BranchingAndDropPipeline_CombinesStackOperationsWithoutLeakingGarbage()
    {
        var toBranch = Guid.NewGuid();
        var end = Guid.NewGuid();

        var combineMethod = typeof(CompilerAndInterpreterResilienceTests)
            .GetMethod(nameof(CombineDigits), BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(combineMethod, Is.Not.Null);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [10]),
            new Instruction(UOpCode.Push, [111]),
            new Instruction(UOpCode.Drop),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [toBranch]),
            new Instruction(UOpCode.Push, [77]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Jmp, [end]),
            new Instruction(UOpCode.Label, [toBranch]),
            new Instruction(UOpCode.Push, [5]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Label, [end]),
            new Instruction(UOpCode.Push, [6]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(1056));
    }

    private static int AddOne(int value) => value + 1;

    private static int CombineDigits(int acc, int nextDigit) => acc * 10 + nextDigit;

    private static T Echo<T>(T value) => value;

    private static object? ExecuteInInterpreter(IAbstractIR ir)
    {
        var interpreter = new InterpreterImpl();
        return interpreter.Execute(ir);
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private static object CompileAndExecute(IAbstractIR ir)
    {
        var compiler = new AbstractMethodsCompilerImpl();
        var compiled = compiler.Compile(ir, []);
        var executor = new DynamicMethodExecutor();
        return executor.Execute(compiled);
    }

    private static void AssertReflectionMembersExist(params MethodBase?[] members)
    {
        Assert.Multiple(() =>
        {
            foreach (var member in members)
                Assert.That(member, Is.Not.Null);
        });
    }

    private sealed class ReflectionTarget(int seed)
    {
        public int IncrementBy(int value) => seed + value;
    }
}