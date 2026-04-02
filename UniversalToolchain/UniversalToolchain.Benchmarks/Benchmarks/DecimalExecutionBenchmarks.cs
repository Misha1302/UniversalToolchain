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
public class DecimalExecutionBenchmarks
{
    [Params("double", "decimal")]
    public string NumericMode { get; set; } = "double";

    private Func<double, double, double>? _doubleFunc;
    private Func<decimal, decimal, decimal>? _decimalFunc;
    private INativeDelegateInvoker? _invoker;
    private dynamic? _artifact;
    private dynamic? _reusedSession;

    [GlobalSetup]
    public void GlobalSetup()
    {
        using var host = BenchmarkHostFactory.CreateWistHost();
        var compiler = host.GetArtifactCompiler<DynamicMethod>("compiler");

        if (NumericMode == "double")
        {
            _artifact = compiler.Compile(ExpressionCatalog.DoublePricing, new OrderedDictionary<string, Type>
            {
                ["price"] = typeof(double),
                ["fee"] = typeof(double)
            });
            _doubleFunc = _artifact.AsFunc<double, double, double>();
        }
        else if (NumericMode == "decimal")
        {
            _artifact = compiler.Compile(ExpressionCatalog.DecimalPricing, new OrderedDictionary<string, Type>
            {
                ["price"] = typeof(decimal),
                ["fee"] = typeof(decimal)
            });
            _decimalFunc = _artifact.AsFunc<decimal, decimal, decimal>();
        }
        else
        {
            Thrower.Argument(nameof(NumericMode), $"Unknown numeric mode '{NumericMode}'.");
        }

        _invoker = _artifact.GetNativeDelegateInvoker();
        _reusedSession = _artifact.CreateSession();
    }

    [Benchmark(Baseline = true)]
    public decimal CSharpBaseline()
    {
        return NumericMode == "double"
            ? (decimal)(InputData.Price * 0.9 + InputData.Fee)
            : InputData.DecimalPrice * 0.9m + InputData.DecimalFee;
    }

    [Benchmark]
    public decimal WistAsFunc()
    {
        return NumericMode == "double"
            ? (decimal)_doubleFunc.NotNull()(InputData.Price, InputData.Fee)
            : _decimalFunc.NotNull()(InputData.DecimalPrice, InputData.DecimalFee);
    }

    [Benchmark]
    public decimal WistNativeInvoker()
    {
        return NumericMode == "double"
            ? (decimal)_invoker.NotNull().Invoke<double, double, double>(InputData.Price, InputData.Fee)
            : _invoker.NotNull().Invoke<decimal, decimal, decimal>(InputData.DecimalPrice, InputData.DecimalFee);
    }

    [Benchmark]
    public decimal WistSessionReused()
    {
        var session = _reusedSession.NotNull();
        if (NumericMode == "double")
        {
            session.SetArgument("price", InputData.Price);
            session.SetArgument("fee", InputData.Fee);
            return (decimal)session.Run<double>();
        }

        session.SetArgument("price", InputData.DecimalPrice);
        session.SetArgument("fee", InputData.DecimalFee);
        return session.Run<decimal>();
    }

    [Benchmark]
    public decimal WistSessionNew()
    {
        var session = _artifact.NotNull().CreateSession();
        if (NumericMode == "double")
        {
            session.SetArgument("price", InputData.Price);
            session.SetArgument("fee", InputData.Fee);
            return (decimal)session.Run<double>();
        }

        session.SetArgument("price", InputData.DecimalPrice);
        session.SetArgument("fee", InputData.DecimalFee);
        return session.Run<decimal>();
    }
}
