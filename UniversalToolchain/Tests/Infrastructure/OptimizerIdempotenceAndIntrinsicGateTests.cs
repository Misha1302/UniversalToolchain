namespace Tests.Infrastructure;

[TestFixture]
public class OptimizerIdempotenceAndIntrinsicGateTests
{
    [Test]
    public void LocalVariablesOptimizer_ShouldBeIdempotent_ForNormalizedProgram()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [9]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)])
        );

        var first = module.ProcessIr(ir, compiler);
        var second = module.ProcessIr(first, compiler);

        Assert.That(Project(second), Is.EqualTo(Project(first)));
    }

    [Test]
    public void ArithmeticOptimization_ShouldPreserveSemantics_WhenAppliedRepeatedly()
    {
        var module = new ArithmeticOptimizerModule();
        var compiler = new FakeCompiler(["add_i32", "sub_i32", "mul_i32", "div_i32"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Push, [4]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"])
        );

        var first = module.ProcessIr(ir, compiler);
        var second = module.ProcessIr(first, compiler);

        Assert.That(CompileAndExecute(second), Is.EqualTo(CompileAndExecute(first)));
    }

    [Test]
    public void OptimizerPipeline_ShouldNotEmitUnsupportedIntrinsic_ForCurrentBackend()
    {
        var module = new NativeCilOptimizerModule();
        var compiler = new FakeCompiler(["load_i32"]);

        var optimized = module.ProcessIr(BuildIr(new Instruction(UOpCode.Push, [1.2m])), compiler);

        Assert.That(optimized.Instructions.Select(x => x.Operands[0]).OfType<string>(), Does.Not.Contain("load_f64"));
    }

    [Test]
    public void Optimization_ShouldNotCrossObservableBehaviorBoundaries()
    {
        var module = new EGraphOptimizerModule();
        var compiler = new FakeCompiler(["add_i32", "mul_i32", "sub_i32", "div_i32"]);
        var label = Guid.NewGuid();
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Jmp, [label]),
            new Instruction(UOpCode.Push, [777]),
            new Instruction(UOpCode.Label, [label]),
            new Instruction(UOpCode.Push, [5])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions.Any(x => x.UOpCode == UOpCode.Jmp), Is.True);
        Assert.That(optimized.Instructions.Any(x => x.UOpCode == UOpCode.Label), Is.True);
    }

    private static string[] Project(IAbstractIR ir) => ir.Instructions.Select(x => x.ToString()).ToArray();

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private static object CompileAndExecute(IAbstractIR ir)
    {
        var compiled = new AbstractMethodsCompilerImpl().Compile(ir, new CompilationInput { SourceText = string.Empty });
        return new DynamicMethodExecutor().Execute(compiled, new ExecutionEnvironment([]));
    }

    private sealed class FakeCompiler(IReadOnlyList<string> intrinsics) : IAbstractIrCompiler<object>
    {
        public IReadOnlyList<string> SupportedIntrinsics => intrinsics;
        public object Compile(IAbstractIR air, CompilationInput input) => new();
    }
}
