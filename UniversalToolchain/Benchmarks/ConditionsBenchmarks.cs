namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class ConditionsBenchmarks : BenchmarkBase
{
    private readonly string _conditions = @"
        let x = 75
        let result = 0
        
        if x >= 90
            result = 5
        elif x >= 80
            result = 4
        elif x >= 70
            result = 3
        elif x >= 60
            result = 2
        else
            result = 1
        
        result";

    [Benchmark]
    public object? Interpreter_Conditions()
    {
        InterpreterCore.PrepareToRun(_conditions);
        return InterpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_Conditions()
    {
        CompilerCore.PrepareToRun(_conditions);
        return CompilerCore.RunPrepared();
    }
}