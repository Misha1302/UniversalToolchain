using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class HeavyLoopBenchmarks : BenchmarkBase
{
    private readonly string _heavyLoop = @"
        let sum = 0
        let i = 1
        @loop:
        if i > 1000 goto @end
            let j = 1
            @inner:
            if j > 100 goto @inner_end
                sum = sum + (i * j)
                j = j + 1
                goto @inner
            @inner_end:
            i = i + 1
            goto @loop
        @end:
        sum";


    [Benchmark]
    public object? Interpreter_HeavyLoop()
    {
        InterpreterCore.PrepareToRun(_heavyLoop);
        return InterpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_HeavyLoop()
    {
        CompilerCore.PrepareToRun(_heavyLoop);
        return CompilerCore.RunPrepared();
    }
}