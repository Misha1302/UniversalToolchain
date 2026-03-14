namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class BasicLoopBenchmarks : BenchmarkBase
{
    private readonly string _loopSum = @"
        let sum = 0
        let i = 1
        @loop:
        if i > 100 goto @end
            sum = sum + i
            i = i + 1
            goto @loop
        @end:
        sum";

    [Benchmark]
    public object? Interpreter_BasicLoop()
    {
        InterpreterCore.PrepareToRun(_loopSum);
        return InterpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_BasicLoop()
    {
        CompilerCore.PrepareToRun(_loopSum);
        return CompilerCore.RunPrepared();
    }
}