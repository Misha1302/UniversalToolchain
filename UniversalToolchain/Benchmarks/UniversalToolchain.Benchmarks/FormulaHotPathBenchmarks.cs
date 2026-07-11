using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NCalc;
using NCalc.LambdaCompilation;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Benchmarks;

/// <summary>
///     Measures steady-state execution of already compiled/prepared formula artifacts.
///     Compilation, parsing and engine creation are intentionally outside the measured hot path.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class FormulaHotPathBenchmarks
{
    private const int Operations = 1024;

    private double[] _a = [];
    private double[] _b = [];
    private double[] _c = [];
    private int _index;

    private WistEngine? _wist;
    private FormulaBenchContext _context = null!;
    private Func<double, double, double, double> _cSharp = null!;
    private Func<FormulaBenchContext, double> _nCalc = null!;
    private Func<double, double, double, double> _wistCompiledDelegate = null!;
    private WistFunc<double, double, double, double> _wistCompileFunc = null!;

    [Params(FormulaWorkload.SimpleArithmetic, FormulaWorkload.DeepArithmetic, FormulaWorkload.RepeatedSubexpressions)]
    public FormulaWorkload Workload { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_a, _b, _c) = FormulaBenchmarkData.CreateInputs();
        _context = new FormulaBenchContext();
        _cSharp = FormulaBenchmarkData.CSharpDelegate(Workload);

        var nCalcExpression = new Expression(FormulaBenchmarkData.NCalcFormula(Workload));
        _nCalc = nCalcExpression.ToLambda<FormulaBenchContext, double>();

        _wist = WistEngine.CreateRestrictedArithmetic();
        _wistCompiledDelegate = _wist
            .Compile<Func<double, double, double, double>>(FormulaBenchmarkData.WistFormula(Workload), "A", "B", "C")
            .CompiledDelegate;
        _wistCompileFunc = _wist.CompileFunc<double, double, double, double>(FormulaBenchmarkData.WistFormula(Workload), "A", "B", "C");

        AssertParity();
    }

    [GlobalCleanup]
    public void Cleanup() => _wist?.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public double CSharp_PreparedDelegate()
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
    public double NCalc_CompiledLambda()
    {
        var sum = 0.0;
        for (var k = 0; k < Operations; k++)
        {
            var i = NextIndex();
            _context.A = _a[i];
            _context.B = _b[i];
            _context.C = _c[i];
            sum += _nCalc(_context);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public double Wist_CompiledDelegate()
    {
        var sum = 0.0;
        for (var k = 0; k < Operations; k++)
        {
            var i = NextIndex();
            sum += _wistCompiledDelegate(_a[i], _b[i], _c[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public double Wist_CompileFuncFastInvoker()
    {
        var sum = 0.0;
        for (var k = 0; k < Operations; k++)
        {
            var i = NextIndex();
            sum += _wistCompileFunc.Invoke(_a[i], _b[i], _c[i]);
        }

        return sum;
    }

    private int NextIndex()
    {
        var i = _index;
        _index = i + 1 & FormulaBenchmarkData.DataSize - 1;
        return i;
    }

    private void AssertParity()
    {
        int[] indexes = [0, 1, 17, 255, 1023, 2047, 4095];

        foreach (var i in indexes)
        {
            var expected = _cSharp(_a[i], _b[i], _c[i]);

            _context.A = _a[i];
            _context.B = _b[i];
            _context.C = _c[i];

            FormulaBenchmarkData.AssertClose(expected, _nCalc(_context), nameof(NCalc_CompiledLambda), i);
            FormulaBenchmarkData.AssertClose(expected, _wistCompiledDelegate(_a[i], _b[i], _c[i]), nameof(Wist_CompiledDelegate), i);
            FormulaBenchmarkData.AssertClose(expected, _wistCompileFunc.Invoke(_a[i], _b[i], _c[i]), nameof(Wist_CompileFuncFastInvoker), i);
        }
    }
}
