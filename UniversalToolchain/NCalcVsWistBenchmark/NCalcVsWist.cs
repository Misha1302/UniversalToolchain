using System.Reflection.Emit;
using BasicCore;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using DependencyInjection;
using DynamicMethodCalling;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NCalc;
using NCalc.LambdaCompilation;

namespace NCalcVsWistBenchmark;

[MemoryDiagnoser]
[RankColumn]
public class NCalcVsWist
{
    private readonly string _code =
        """
        3 + 4 * 5
        """;

    private Func<int> _ncalcInvoker = null!;

    private DynamicMethodInvoker<int> _wistInvoker = null!;


    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection().AddWistServices("./../../../../../../../..");

        var provider = services.BuildServiceProvider();
        var method = provider
            .GetService<IExecutableGiver<DynamicMethod>>()
            .NotNull()
            .GetExecutable(_code);
        var invoker = new DynamicMethodInvoker<int>(method);
        _wistInvoker = invoker;


        var expression = new Expression(_code);
        _ncalcInvoker = expression.ToLambda<int>();
    }

    [Benchmark(Baseline = true)]
    public int WistRun() => _wistInvoker.Invoke();

    [Benchmark]
    public int NCalcRun() => _ncalcInvoker.Invoke();
}