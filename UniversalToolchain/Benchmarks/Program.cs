using BenchmarkDotNet.Running;

namespace Benchmarks;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("================================================================");
        BenchmarkRunner.Run<BasicLoopBenchmarks>();

        Console.WriteLine("================================================================");
        BenchmarkRunner.Run<ConditionsBenchmarks>();

        Console.WriteLine("================================================================");
        BenchmarkRunner.Run<CSharpInteropMathBenchmarks>();

        Console.WriteLine("================================================================");
        BenchmarkRunner.Run<CSharpInteropTrigonomentryBenchmarks>();

        Console.WriteLine("================================================================");
        BenchmarkRunner.Run<HeavyColdStartBenchmarks>();

        Console.WriteLine("================================================================");
        BenchmarkRunner.Run<HeavyLoopBenchmarks>();
    }
}