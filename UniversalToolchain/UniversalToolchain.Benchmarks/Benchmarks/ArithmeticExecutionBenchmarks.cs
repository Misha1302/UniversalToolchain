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
public class ArithmeticExecutionBenchmarks
{
    [Params(ExpressionCatalog.IntUnary, ExpressionCatalog.IntBinary, ExpressionCatalog.IntQuad)]
    public string CaseName { get; set; } = ExpressionCatalog.IntUnary;

    private Func<int, int>? _funcUnary;
    private Func<int, int, int>? _funcBinary;
    private Func<int, int, int, int, int>? _funcQuad;

    private INativeDelegateInvoker? _invoker;
    private dynamic? _artifact;
    private dynamic? _reusedSession;

    [GlobalSetup]
    public void GlobalSetup()
    {
        using var host = BenchmarkHostFactory.CreateWistHost();
        var compiler = host.GetArtifactCompiler<DynamicMethod>("compiler");

        switch (CaseName)
        {
            case ExpressionCatalog.IntUnary:
                _artifact = compiler.Compile(ExpressionCatalog.IntUnary, new OrderedDictionary<string, Type>
                {
                    ["x"] = typeof(int)
                });
                _funcUnary = _artifact.AsFunc<int, int>();
                break;
            case ExpressionCatalog.IntBinary:
                _artifact = compiler.Compile(ExpressionCatalog.IntBinary, new OrderedDictionary<string, Type>
                {
                    ["x"] = typeof(int),
                    ["y"] = typeof(int)
                });
                _funcBinary = _artifact.AsFunc<int, int, int>();
                break;
            case ExpressionCatalog.IntQuad:
                _artifact = compiler.Compile(ExpressionCatalog.IntQuad, new OrderedDictionary<string, Type>
                {
                    ["a"] = typeof(int),
                    ["b"] = typeof(int),
                    ["c"] = typeof(int),
                    ["d"] = typeof(int)
                });
                _funcQuad = _artifact.AsFunc<int, int, int, int, int>();
                break;
            default:
                Thrower.Argument(nameof(CaseName), $"Unsupported arithmetic case '{CaseName}'.");
                break;
        }

        _invoker = _artifact.GetNativeDelegateInvoker();
        _reusedSession = _artifact.CreateSession();
    }

    [Benchmark(Baseline = true)]
    public int CSharpBaseline() => CaseName switch
    {
        ExpressionCatalog.IntUnary => InputData.X * 2 + 3,
        ExpressionCatalog.IntBinary => InputData.X * InputData.Y + 7,
        ExpressionCatalog.IntQuad => InputData.A * InputData.B + InputData.C * InputData.D - InputData.A + 3,
        _ => Thrower.ArgumentOutOfRange<int>(nameof(CaseName), $"Unknown case '{CaseName}'.")
    };

    [Benchmark]
    public int WistAsFunc() => CaseName switch
    {
        ExpressionCatalog.IntUnary => _funcUnary.NotNull()(InputData.X),
        ExpressionCatalog.IntBinary => _funcBinary.NotNull()(InputData.X, InputData.Y),
        ExpressionCatalog.IntQuad => _funcQuad.NotNull()(InputData.A, InputData.B, InputData.C, InputData.D),
        _ => Thrower.ArgumentOutOfRange<int>(nameof(CaseName), $"Unknown case '{CaseName}'.")
    };

    [Benchmark]
    public int WistNativeInvoker() => CaseName switch
    {
        ExpressionCatalog.IntUnary => _invoker.NotNull().Invoke<int, int>(InputData.X),
        ExpressionCatalog.IntBinary => _invoker.NotNull().Invoke<int, int, int>(InputData.X, InputData.Y),
        ExpressionCatalog.IntQuad => _invoker.NotNull().Invoke<int, int, int, int, int>(InputData.A, InputData.B, InputData.C, InputData.D),
        _ => Thrower.ArgumentOutOfRange<int>(nameof(CaseName), $"Unknown case '{CaseName}'.")
    };

    [Benchmark]
    public int WistSessionReused()
    {
        var session = _reusedSession.NotNull();

        switch (CaseName)
        {
            case ExpressionCatalog.IntUnary:
                session.SetArgument("x", InputData.X);
                break;
            case ExpressionCatalog.IntBinary:
                session.SetArgument("x", InputData.X);
                session.SetArgument("y", InputData.Y);
                break;
            case ExpressionCatalog.IntQuad:
                session.SetArgument("a", InputData.A);
                session.SetArgument("b", InputData.B);
                session.SetArgument("c", InputData.C);
                session.SetArgument("d", InputData.D);
                break;
            default:
                Thrower.Argument(nameof(CaseName), $"Unknown case '{CaseName}'.");
                break;
        }

        return session.Run<int>();
    }

    [Benchmark]
    public int WistSessionNew()
    {
        var session = _artifact.NotNull().CreateSession();

        switch (CaseName)
        {
            case ExpressionCatalog.IntUnary:
                session.SetArgument("x", InputData.X);
                break;
            case ExpressionCatalog.IntBinary:
                session.SetArgument("x", InputData.X);
                session.SetArgument("y", InputData.Y);
                break;
            case ExpressionCatalog.IntQuad:
                session.SetArgument("a", InputData.A);
                session.SetArgument("b", InputData.B);
                session.SetArgument("c", InputData.C);
                session.SetArgument("d", InputData.D);
                break;
            default:
                Thrower.Argument(nameof(CaseName), $"Unknown case '{CaseName}'.");
                break;
        }

        return session.Run<int>();
    }
}
