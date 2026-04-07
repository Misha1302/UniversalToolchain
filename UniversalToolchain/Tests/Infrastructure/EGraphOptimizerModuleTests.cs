namespace Tests.Infrastructure;

[TestFixture]
public class EGraphOptimizerModuleTests
{
    private static readonly IReadOnlyList<string> _fullIntrinsicSet =
    [
        "add_i32", "sub_i32", "mul_i32", "div_i32",
        "add_i64", "sub_i64", "mul_i64", "div_i64",
        "add_f32", "sub_f32", "mul_f32", "div_f32",
        "add_f64", "sub_f64", "mul_f64", "div_f64"
    ];

    [Test]
    public void EGraphOptimizer_ShouldSimplify_XPlusZero()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"])
        ));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo("load_local"));
    }

    [Test]
    public void EGraphOptimizer_ShouldSimplify_XMulOne()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"])
        ));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo("load_local"));
    }

    [Test]
    public void EGraphOptimizer_ShouldSimplify_XMulZero_ToZero()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"])
        ));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].UOpCode, Is.EqualTo(UOpCode.Push));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo(0));
    }

    [Test]
    public void EGraphOptimizer_ShouldFoldConstants()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Push, [4]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"])
        ));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].UOpCode, Is.EqualTo(UOpCode.Push));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo(20));
    }

    [Test]
    public void EGraphOptimizer_ShouldSimplify_CompositeExpression_ToLocalLoad()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "a", typeof(int)]),
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"])
        ));

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo("load_local"));
        Assert.That(optimized.Instructions[0].Operands[1], Is.EqualTo("a"));
    }

    [Test]
    public void EGraphOptimizer_ShouldKeepControlFlowBoundaries_Intact()
    {
        var label = Guid.NewGuid();
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Jmp, [label]),
            new Instruction(UOpCode.Label, [label]),
            new Instruction(UOpCode.Push, [1])
        ));

        Assert.That(optimized.Instructions[1].UOpCode, Is.EqualTo(UOpCode.Jmp));
        Assert.That(optimized.Instructions[2].UOpCode, Is.EqualTo(UOpCode.Label));
    }

    [Test]
    public void EGraphOptimizer_ShouldLeaveUnsupportedIntrinsic_Unchanged()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [8]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["mod_i32"])
        );

        var optimized = Optimize(ir);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(ir.Instructions.Count));
        Assert.That(optimized.Instructions[2].Operands[0], Is.EqualTo("mod_i32"));
    }

    [Test]
    public void EGraphOptimizer_Result_ShouldKeepTypeStackValid()
    {
        var optimized = Optimize(BuildIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"])
        ));

        Assert.That(CompileAndExecute(optimized), Is.EqualTo(5));
    }

    [Test]
    public void EGraphOptimizer_CompiledExecution_ShouldMatchBeforeAfter()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Push, [4]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"])
        );

        var optimized = Optimize(ir);

        var before = CompileAndExecute(ir);
        var after = CompileAndExecute(optimized);

        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public void EGraphOptimizer_InterpreterAndCompiler_ShouldStayEquivalent_ForSimpleCases()
    {
        var examples = new[]
        {
            BuildIr(
                new Instruction(UOpCode.Push, [7]),
                new Instruction(UOpCode.Push, [0]),
                new Instruction(UOpCode.Intrinsic, ["add_i32"])),
            BuildIr(
                new Instruction(UOpCode.Push, [9]),
                new Instruction(UOpCode.Push, [1]),
                new Instruction(UOpCode.Intrinsic, ["mul_i32"])),
            BuildIr(
                new Instruction(UOpCode.Push, [10]),
                new Instruction(UOpCode.Push, [2]),
                new Instruction(UOpCode.Intrinsic, ["div_i32"]))
        };

        foreach (var ir in examples)
        {
            var optimized = Optimize(ir);
            var interpreted = ExecuteInInterpreter(optimized);
            var compiled = CompileAndExecute(optimized);
            Assert.That(compiled, Is.EqualTo(interpreted));
        }
    }

    private static IAbstractIR Optimize(IAbstractIR ir)
    {
        var module = new EGraphOptimizerModule();
        var compiler = new FakeCompiler(_fullIntrinsicSet);
        return module.ProcessIr(ir, compiler);
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private static object? ExecuteInInterpreter(IAbstractIR ir)
    {
        var interpreter = new InterpreterImpl();
        return interpreter.Execute(ir, new ExecutionEnvironment([]));
    }

    private static object CompileAndExecute(IAbstractIR ir)
    {
        var compiler = new AbstractMethodsCompilerImpl();
        var input = new CompilationInput { SourceText = string.Empty };
        var compiled = compiler.Compile(ir, input);
        var executor = new DynamicMethodExecutor();
        return executor.Execute(compiled, new ExecutionEnvironment([]));
    }

    private sealed class FakeCompiler(IReadOnlyList<string> intrinsics) : IAbstractIrCompiler<object>
    {
        public IReadOnlyList<string> SupportedIntrinsics => intrinsics;

        public object Compile(IAbstractIR air, CompilationInput input) => Thrower.NotSupported<object>();
    }
}
