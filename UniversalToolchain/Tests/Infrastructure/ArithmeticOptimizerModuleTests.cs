using BasicCilCompiler.Execution;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.Execution;
using BytecodeDynamicMethodsCompiler.Compilers;
using ExceptionsManager;

namespace Tests.Infrastructure;

public class ArithmeticOptimizerModuleTests
{
    private static readonly MethodInfo _addInt = typeof(NativeArithmetic).GetMethod(nameof(NativeArithmetic.Add))!.MakeGenericMethod(typeof(int));
    private static readonly MethodInfo _multiplyInt = typeof(NativeArithmetic).GetMethod(nameof(NativeArithmetic.Multiply))!.MakeGenericMethod(typeof(int));

    [Test]
    public void ShouldSimplify_XMinusZero_ToX()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Intrinsic, ["sub_i32"])));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo("load_local"));
    }

    [Test]
    public void ShouldSimplify_XDivOne_ToX()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["div_i32"])));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo("load_local"));
    }

    [Test]
    public void ShouldReassociate_IntAdditionConstants()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"])));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(3));
        Assert.That(optimized.Instructions[1].Operands[0], Is.EqualTo(5));
        Assert.That(optimized.Instructions[2].Operands[0], Is.EqualTo("add_i32"));
    }

    [Test]
    public void ShouldReassociate_IntMultiplyConstants()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"])));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(3));
        Assert.That(optimized.Instructions[1].Operands[0], Is.EqualTo(6));
        Assert.That(optimized.Instructions[2].Operands[0], Is.EqualTo("mul_i32"));
    }

    [Test]
    public void ShouldSimplify_NestedTrivialForms()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"])));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo("load_local"));
    }

    [Test]
    public void ShouldPreserveControlFlowBoundaries()
    {
        var label = Guid.NewGuid();
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Label, [label]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"])));

        Assert.That(optimized.Instructions.Any(x => x.UOpCode == UOpCode.Label), Is.True);
        Assert.That(optimized.Instructions.Count(x => x.UOpCode == UOpCode.Intrinsic && Equals(x.Operands[0], "add_i32")), Is.EqualTo(2));
    }

    [Test]
    public void UnsupportedIntrinsic_ShouldRemainUnchanged()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!;
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [-3]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method]));

        var optimized = Optimize(ir);

        Assert.That(optimized.Instructions.Select(x => x.ToString()), Is.EqualTo(ir.Instructions.Select(x => x.ToString())));
    }

    [Test]
    public void OptimizedStackTypes_ShouldRemainValid()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Intrinsic, ["sub_i32"]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"])));

        var stack = new List<Type>();
        Assert.DoesNotThrow(() => optimized.Instructions.ManipulateTypesStack(stack, AirTypes.ProcessTypesIntrinsic));
        Assert.That(stack, Has.Count.EqualTo(1));
        Assert.That(stack[0], Is.EqualTo(typeof(int)));
    }

    [Test]
    public void CompiledExecution_BeforeAndAfterOptimization_ShouldBeEquivalent()
    {
        var source = BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C#", _addInt]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["call C#", _addInt]),
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["call C#", _multiplyInt]));

        var optimized = Optimize(source);

        var before = CompileAndExecute(source, 10);
        var after = CompileAndExecute(optimized, 10);

        Assert.That(before, Is.EqualTo(after));
    }


    private static IAbstractIR Optimize(IAbstractIR ir)
    {
        var module = new ArithmeticOptimizerModule();
        var compiler = new FakeCompiler([
            "add_i32", "sub_i32", "mul_i32", "div_i32",
            "add_i64", "sub_i64", "mul_i64", "div_i64",
            "add_f32", "sub_f32", "mul_f32", "div_f32",
            "add_f64", "sub_f64", "mul_f64", "div_f64",
            "add_decimal", "sub_decimal", "mul_decimal", "div_decimal"
        ]);
        return module.ProcessIr(ir, compiler);
    }

    private static object CompileAndExecute(IAbstractIR ir, int x, bool hasExternal = true)
    {
        var compiler = new AbstractMethodsCompilerImpl();
        var input = hasExternal
            ? new CompilationInput { SourceText = string.Empty, ExternalBindings = [new ExternalBinding { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable }] }
            : new CompilationInput { SourceText = string.Empty };
        var compiled = compiler.Compile(ir, input);
        var executor = new DynamicMethodExecutor();
        var env = hasExternal
            ? new ExecutionEnvironment([new ExternalBinding { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable, Value = x }])
            : new ExecutionEnvironment([]);
        return executor.Execute(compiled, env);
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private sealed class FakeCompiler(IReadOnlyList<string> intrinsics) : IAbstractIrCompiler<object>
    {
        public IReadOnlyList<string> SupportedIntrinsics => intrinsics;

        public object Compile(IAbstractIR air, CompilationInput input) => Thrower.NotSupported<object>();
    }
}