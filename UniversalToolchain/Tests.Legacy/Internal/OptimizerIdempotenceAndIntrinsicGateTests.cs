namespace Tests.Internal;

[TestFixture]
public class OptimizerIdempotenceAndIntrinsicGateTests
{
    [Test]
    public void LocalVariablesOptimizer_ShouldBeStructurallyIdempotent()
    {
        var optimizer = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [10]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [32]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"])
        );

        var pass1 = optimizer.ProcessIr(ir, compiler);
        var pass2 = optimizer.ProcessIr(pass1, compiler);

        Assert.That(Project(pass2), Is.EqualTo(Project(pass1)));
    }

    [Test]
    public void ArithmeticOptimizer_ShouldBeStructurallyIdempotent_AcrossRepeatedPasses()
    {
        var optimizer = new ArithmeticOptimizerModule();
        var compiler = new FakeCompiler(["add_i32", "sub_i32", "mul_i32", "div_i32"]);
        var original = BuildIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Push, [4]),
            new Instruction(UOpCode.Intrinsic, ["mul_i32"])
        );

        var pass1 = optimizer.ProcessIr(original, compiler);
        var pass2 = optimizer.ProcessIr(pass1, compiler);

        Assert.That(Project(pass2), Is.EqualTo(Project(pass1)));
        Assert.That(pass1.Instructions.Count, Is.LessThanOrEqualTo(original.Instructions.Count));
    }

    [Test]
    public void NativeCilOptimizer_ShouldEmitOnlySupportedLoadIntrinsics_ForLiteralTypes()
    {
        var optimizer = new NativeCilOptimizerModule();
        var compiler = new FakeCompiler(["load_i32", "load_f64"]);

        var optimized = optimizer.ProcessIr(BuildIr(new Instruction(UOpCode.Push, [1.2m])), compiler);

        var projected = optimized.Instructions.Select(x => x.ToString()).ToArray();

        Assert.That(projected.Any(x => x.Contains("load_decimal", StringComparison.Ordinal)), Is.False);
        Assert.That(CompileAndExecute(optimized), Is.EqualTo(1.2m));
    }

    [Test]
    public void EGraphOptimizer_ShouldRemainStable_AcrossRepeatedPasses_ForControlFlowSensitiveProgram()
    {
        var optimizer = new EGraphOptimizerModule();
        var compiler = new FakeCompiler(["add_i32", "sub_i32", "mul_i32", "div_i32"]);
        var labelTrue = Guid.NewGuid();
        var labelEnd = Guid.NewGuid();

        var original = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [labelTrue]),
            new Instruction(UOpCode.Push, [100]),
            new Instruction(UOpCode.Jmp, [labelEnd]),
            new Instruction(UOpCode.Label, [labelTrue]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Label, [labelEnd])
        );

        var pass1 = optimizer.ProcessIr(original, compiler);
        var pass2 = optimizer.ProcessIr(pass1, compiler);

        Assert.Multiple(() =>
        {
            Assert.That(Project(pass2), Is.EqualTo(Project(pass1)));
            Assert.That(pass1.Instructions.Count(x => x.UOpCode == UOpCode.Label), Is.EqualTo(original.Instructions.Count(x => x.UOpCode == UOpCode.Label)));
            Assert.That(pass1.Instructions.Count(x => x.UOpCode == UOpCode.Jmp || x.UOpCode == UOpCode.JmpIf), Is.EqualTo(original.Instructions.Count(x => x.UOpCode == UOpCode.Jmp || x.UOpCode == UOpCode.JmpIf)));
        });
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
