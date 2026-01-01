using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class CSharpInteropMathBenchmarks : BenchmarkBase
{
    private readonly string _mathFunctions = @"
        let x = 2.5
        let result = Main.Sqrt(x) + Main.Log(x, 10) + Main.Pow(x, 2)
        result";

    [Benchmark]
    public object? Interpreter_MathFunctions()
    {
        InterpreterCore.PrepareToRun(_mathFunctions);
        return InterpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_MathFunctions()
    {
        CompilerCore.PrepareToRun(_mathFunctions);
        return CompilerCore.RunPrepared();
    }
}