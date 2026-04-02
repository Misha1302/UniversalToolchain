using System.Collections.Specialized;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using DynamicMethodCalling;
using ExceptionsManager;
using UniversalToolchain.Benchmarks.Infrastructure;

namespace UniversalToolchain.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[CsvMeasurementsExporter]
[JsonExporterAttribute.Full]
public class ScaleExecutionBenchmarks
{
    [Params(8, 32, 128, 512)]
    public int NodeCount { get; set; }

    private Func<int, int>? _wistFunc;
    private INativeDelegateInvoker? _invoker;
    private dynamic? _artifact;
    private dynamic? _reusedSession;

    [GlobalSetup]
    public void GlobalSetup()
    {
        using var host = BenchmarkHostFactory.CreateWistHost();
        var compiler = host.GetArtifactCompiler<DynamicMethod>("compiler");

        var expression = BuildGrowthExpression(NodeCount);
        _artifact = compiler.Compile(expression, new OrderedDictionary<string, Type>
        {
            ["x"] = typeof(int)
        });

        _wistFunc = _artifact.AsFunc<int, int>();
        _invoker = _artifact.GetNativeDelegateInvoker();
        _reusedSession = _artifact.CreateSession();
    }

    [Benchmark(Baseline = true)]
    public int CSharpBaseline() => EvaluateGrowthBaseline(InputData.X, NodeCount);

    [Benchmark]
    public int WistAsFunc() => _wistFunc.NotNull()(InputData.X);

    [Benchmark]
    public int WistNativeInvoker() => _invoker.NotNull().Invoke<int, int>(InputData.X);

    [Benchmark]
    public int WistSessionReused()
    {
        var session = _reusedSession.NotNull();
        session.SetArgument("x", InputData.X);
        return session.Run<int>();
    }

    [Benchmark]
    public int WistSessionNew()
    {
        var session = _artifact.NotNull().CreateSession();
        session.SetArgument("x", InputData.X);
        return session.Run<int>();
    }

    private static string BuildGrowthExpression(int nodeCount)
    {
        if (nodeCount < 2)
            Thrower.ArgumentOutOfRange<string>(nameof(nodeCount), "Node count must be at least 2.");

        var terms = new List<string>(nodeCount);
        for (var i = 1; i <= nodeCount; i++)
            terms.Add($"x * {i}");

        return string.Join(" + ", terms);
    }

    private static int EvaluateGrowthBaseline(int x, int nodeCount)
    {
        var sum = 0;
        for (var i = 1; i <= nodeCount; i++)
            sum += x * i;

        return sum;
    }
}
