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
public class BooleanExecutionBenchmarks
{
    [Params(ExpressionCatalog.TernaryClamp, ExpressionCatalog.ShortCircuit)]
    public string CaseName { get; set; } = ExpressionCatalog.TernaryClamp;

    [Params(true, false)]
    public bool UsePredictableData { get; set; }

    private Func<int, int>? _ternaryFunc;
    private Func<int, int, int, bool>? _shortCircuitFunc;
    private INativeDelegateInvoker? _invoker;
    private dynamic? _artifact;
    private dynamic? _reusedSession;

    private int _index;

    [GlobalSetup]
    public void GlobalSetup()
    {
        using var host = BenchmarkHostFactory.CreateWistHost();
        var compiler = host.GetArtifactCompiler<DynamicMethod>("compiler");

        switch (CaseName)
        {
            case ExpressionCatalog.TernaryClamp:
                _artifact = compiler.Compile(ExpressionCatalog.TernaryClamp, new OrderedDictionary<string, Type>
                {
                    ["x"] = typeof(int)
                });
                _ternaryFunc = _artifact.AsFunc<int, int>();
                break;
            case ExpressionCatalog.ShortCircuit:
                _artifact = compiler.Compile(ExpressionCatalog.ShortCircuit, new OrderedDictionary<string, Type>
                {
                    ["a"] = typeof(int),
                    ["b"] = typeof(int),
                    ["c"] = typeof(int)
                });
                _shortCircuitFunc = _artifact.AsFunc<int, int, int, bool>();
                break;
            default:
                Thrower.Argument(nameof(CaseName), $"Unsupported boolean case '{CaseName}'.");
                break;
        }

        _invoker = _artifact.GetNativeDelegateInvoker();
        _reusedSession = _artifact.CreateSession();
    }

    [Benchmark(Baseline = true)]
    public int CSharpBaseline()
    {
        return CaseName == ExpressionCatalog.TernaryClamp
            ? TernaryBaseline(NextTernary())
            : ShortCircuitBaseline(NextShortCircuit()) ? 1 : 0;
    }

    [Benchmark]
    public int WistAsFunc()
    {
        if (CaseName == ExpressionCatalog.TernaryClamp)
            return _ternaryFunc.NotNull()(NextTernary());

        var args = NextShortCircuit();
        return _shortCircuitFunc.NotNull()(args.a, args.b, args.c) ? 1 : 0;
    }

    [Benchmark]
    public int WistNativeInvoker()
    {
        if (CaseName == ExpressionCatalog.TernaryClamp)
            return _invoker.NotNull().Invoke<int, int>(NextTernary());

        var args = NextShortCircuit();
        return _invoker.NotNull().Invoke<int, int, int, bool>(args.a, args.b, args.c) ? 1 : 0;
    }

    [Benchmark]
    public int WistSessionReused()
    {
        var session = _reusedSession.NotNull();
        if (CaseName == ExpressionCatalog.TernaryClamp)
        {
            session.SetArgument("x", NextTernary());
            return session.Run<int>();
        }

        var args = NextShortCircuit();
        session.SetArgument("a", args.a);
        session.SetArgument("b", args.b);
        session.SetArgument("c", args.c);
        return session.Run<bool>() ? 1 : 0;
    }

    [Benchmark]
    public int WistSessionNew()
    {
        var session = _artifact.NotNull().CreateSession();
        if (CaseName == ExpressionCatalog.TernaryClamp)
        {
            session.SetArgument("x", NextTernary());
            return session.Run<int>();
        }

        var args = NextShortCircuit();
        session.SetArgument("a", args.a);
        session.SetArgument("b", args.b);
        session.SetArgument("c", args.c);
        return session.Run<bool>() ? 1 : 0;
    }

    private int NextTernary()
    {
        var data = UsePredictableData ? InputData.PredictableBranchInputs : InputData.RandomLikeBranchInputs;
        var value = data[_index % data.Length];
        _index++;
        return value;
    }

    private (int a, int b, int c) NextShortCircuit()
    {
        var data = UsePredictableData ? InputData.PredictableShortCircuitInputs : InputData.RandomLikeShortCircuitInputs;
        var value = data[_index % data.Length];
        _index++;
        return value;
    }

    private static int TernaryBaseline(int x) => x > 10 ? x : 0;

    private static bool ShortCircuitBaseline((int a, int b, int c) x) => (x.a > 0 && x.b > 0) || x.c > 0;
}
