namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class HeavyColdStartBenchmarks : BenchmarkBase
{
    private readonly string _heavyLoop = @"
        let sum = 0
        let i = 1
        @loop:
        if i > 2 goto @end
            let j = 1
            @inner:
            if j > 2 goto @inner_end
                sum = sum + (i * j)
                j = j + 1
                goto @inner
            @inner_end:
            i = i + 1
            goto @loop
        @end:
        sum";

    [Benchmark(Baseline = true)]
    public object? Interpreter_ColdStart()
    {
        InterpreterCore.PrepareToRun("");
        InterpreterCore.PrepareToRun(_heavyLoop);
        return InterpreterCore.Run(_heavyLoop);
    }

    [Benchmark]
    public object? Compiler_ColdStart()
    {
        CompilerCore.PrepareToRun("");
        CompilerCore.PrepareToRun(_heavyLoop);
        return CompilerCore.RunPrepared();
    }
}