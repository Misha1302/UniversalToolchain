using BenchmarkDotNet.Attributes;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Benchmarks;

/// <summary>
///     Measures convenience API cost. These methods intentionally include per-call runtime/evaluate overhead and must not
///     be compared with the hot compiled delegate benchmark as execution-speed evidence.
/// </summary>
[MemoryDiagnoser]
public class FormulaConvenienceBenchmarks
{
    private const string Formula = "A + B * C / 5.0";
    private const int Operations = 128;

    private double[] _a = [];
    private double[] _b = [];
    private double[] _c = [];
    private int _index;

    private WistEngine? _compilerEngine;
    private WistEngine? _interpreterEngine;
    private Func<double, double, double, double> _cSharp = null!;

    [GlobalSetup]
    public void Setup()
    {
        (_a, _b, _c) = FormulaBenchmarkData.CreateInputs();
        _cSharp = FormulaBenchmarkData.CSharpDelegate(FormulaWorkload.SimpleArithmetic);
        _compilerEngine = WistEngine.CreateSafeFormulas();
        _interpreterEngine = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.SafeFormulas,
            Backend = WistBackend.Interpreter
        });

        AssertParity();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _compilerEngine?.Dispose();
        _interpreterEngine?.Dispose();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public double CSharp_DirectFormula()
    {
        var sum = 0.0;
        for (var k = 0; k < Operations; k++)
        {
            var i = NextIndex();
            sum += _cSharp(_a[i], _b[i], _c[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public double Wist_CompilerEvaluateWithDictionary()
    {
        var sum = 0.0;
        for (var k = 0; k < Operations; k++)
        {
            var i = NextIndex();
            sum += CompilerEngine.Evaluate<double>(Formula, CreateArguments(i));
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public double Wist_InterpreterEvaluateWithDictionary()
    {
        var sum = 0.0;
        for (var k = 0; k < Operations; k++)
        {
            var i = NextIndex();
            sum += InterpreterEngine.Evaluate<double>(Formula, CreateArguments(i));
        }

        return sum;
    }

    private IReadOnlyDictionary<string, object?> CreateArguments(int index) => new Dictionary<string, object?>
    {
        ["A"] = _a[index],
        ["B"] = _b[index],
        ["C"] = _c[index]
    };

    private int NextIndex()
    {
        var i = _index;
        _index = i + 1 & FormulaBenchmarkData.DataSize - 1;
        return i;
    }

    private void AssertParity()
    {
        var expected = _cSharp(_a[17], _b[17], _c[17]);
        FormulaBenchmarkData.AssertClose(expected, CompilerEngine.Evaluate<double>(Formula, CreateArguments(17)), nameof(Wist_CompilerEvaluateWithDictionary), 17);
        FormulaBenchmarkData.AssertClose(expected, InterpreterEngine.Evaluate<double>(Formula, CreateArguments(17)), nameof(Wist_InterpreterEvaluateWithDictionary), 17);
    }

    private WistEngine CompilerEngine => _compilerEngine ?? throw new InvalidOperationException("Compiler engine is not initialized.");

    private WistEngine InterpreterEngine => _interpreterEngine ?? throw new InvalidOperationException("Interpreter engine is not initialized.");
}
