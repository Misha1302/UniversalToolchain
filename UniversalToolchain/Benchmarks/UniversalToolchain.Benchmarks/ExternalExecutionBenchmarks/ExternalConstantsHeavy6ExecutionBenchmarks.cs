using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;
using DynamicExpresso;
using DynamicMethodCalling.Core;
using NCalc;
using NCalc.LambdaCompilation;

namespace UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ExternalConstantsHeavy6ExecutionBenchmarks : ExternalArithmeticExecutionBenchmarkEnvironmentBase
{
    private const string WistFormula = "(A * 1.5 + B * 2.0 - C * 3.0 + D / 4.0 + E / 5.0) * 0.75 + F";
    private const string NCalcFormula = "([A] * 1.5 + [B] * 2.0 - [C] * 3.0 + [D] / 4.0 + [E] / 5.0) * 0.75 + [F]";
    private const string DynamicExpressoFormula = "(A * 1.5 + B * 2.0 - C * 3.0 + D / 4.0 + E / 5.0) * 0.75 + F";
    private const int InnerCount = 4096;

    private ExternalBenchContext6 _nCalcContext = null!;
    private Func<ExternalBenchContext6, double> _nCalcLambda = null!;
    private Func<double, double, double, double, double, double, double> _dynamicExpressoDelegate = null!;
    private DynamicMethodInvoker<double, double, double, double, double, double, double> _wistFastInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C", "D", "E", "F"]);
        _wistFastInvoker = new DynamicMethodInvoker<double, double, double, double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<ExternalBenchContext6, double>();
        _nCalcContext = new ExternalBenchContext6();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate = dynamicExpressoInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double>>(
            DynamicExpressoFormula,
            "A", "B", "C", "D", "E", "F");

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true)]
    public double CSharp_NoInliningMethod()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i], F[i]);
        }

        return sum;
    }

    [Benchmark]
    public double DynamicExpresso_Delegate()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += _dynamicExpressoDelegate(A[i], B[i], C[i], D[i], E[i], F[i]);
        }

        return sum;
    }

    [Benchmark]
    public double NCalc_Lambda()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            _nCalcContext.A = A[i]; _nCalcContext.B = B[i]; _nCalcContext.C = C[i];
            _nCalcContext.D = D[i]; _nCalcContext.E = E[i]; _nCalcContext.F = F[i];
            sum += _nCalcLambda(_nCalcContext);
        }

        return sum;
    }

    [Benchmark]
    public double Wist_Cil_FastInvoker()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += _wistFastInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i]);
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f)
        => (a * 1.5 + b * 2.0 - c * 3.0 + d / 4.0 + e / 5.0) * 0.75 + f;

    private double CSharpAt(int index) => CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index]);

    private double DynamicExpressoAt(int index) => _dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index], F[index]);

    private double NCalcAt(int index)
    {
        _nCalcContext.A = A[index]; _nCalcContext.B = B[index]; _nCalcContext.C = C[index];
        _nCalcContext.D = D[index]; _nCalcContext.E = E[index]; _nCalcContext.F = F[index];
        return _nCalcLambda(_nCalcContext);
    }

    private double WistAt(int index) => _wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index]);
}
