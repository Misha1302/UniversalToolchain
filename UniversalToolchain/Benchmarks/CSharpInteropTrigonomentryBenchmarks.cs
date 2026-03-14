namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class CSharpInteropTrigonomentryBenchmarks : BenchmarkBase
{
    private readonly string _trigonometry = @"
        let angle = 0.5
        let sinVal = Main.Sin(angle)
        let cosVal = Main.Cos(angle)
        let tanVal = sinVal / cosVal
        sinVal * sinVal + cosVal * cosVal";

    [Benchmark]
    public object? Interpreter_Trigonometry()
    {
        InterpreterCore.PrepareToRun(_trigonometry);
        return InterpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_Trigonometry()
    {
        CompilerCore.PrepareToRun(_trigonometry);
        return CompilerCore.RunPrepared();
    }
}