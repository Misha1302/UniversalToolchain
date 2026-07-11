using BenchmarkDotNet.Attributes;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Benchmarks;

/// <summary>
///     Measures cold and warm compilation costs separately from steady-state execution.
/// </summary>
[MemoryDiagnoser]
public class FormulaCompilationBenchmarks
{
    private const string Formula = "A + B * C / 5.0";
    private WistEngine? _warmEngine;

    [GlobalSetup]
    public void Setup() => _warmEngine = WistEngine.CreateRestrictedArithmetic();

    [GlobalCleanup]
    public void Cleanup() => _warmEngine?.Dispose();

    [Benchmark(Baseline = true)]
    public Func<double, double, double, double> CSharp_PreparedDelegate() => FormulaBenchmarkData.CSharpDelegate(FormulaWorkload.SimpleArithmetic);

    [Benchmark]
    public Func<double, double, double, double> Wist_CompileOnExistingEngine()
        => WarmEngine.Compile<Func<double, double, double, double>>(Formula, "A", "B", "C").CompiledDelegate;

    [Benchmark]
    public Func<double, double, double, double> Wist_CreateEngineAndCompile()
    {
        using var wist = WistEngine.CreateRestrictedArithmetic();
        return wist.Compile<Func<double, double, double, double>>(Formula, "A", "B", "C").CompiledDelegate;
    }

    private WistEngine WarmEngine => _warmEngine ?? throw new InvalidOperationException("Warm Wist engine is not initialized.");
}
