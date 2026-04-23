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
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class ExternalRepeatedSubexpressions5ExecutionBenchmarks : ExternalArithmeticExecutionBenchmarkEnvironmentBase
{
    private const string WistFormula = "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)";
    private const string NCalcFormula = "(([A] * [B]) + ([A] * [B]) + ([A] * [B]) + ([C] * [D])) / ([E] + 1.0)";
    private const string DynamicExpressoFormula = "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)";
    private const int InnerCount = 4096;

    private ExternalBenchContext5 _nCalcContext = null!;
    private Func<ExternalBenchContext5, double> _nCalcLambda = null!;
    private Func<double, double, double, double, double, double> _dynamicExpressoDelegate = null!;
    private DynamicMethodInvoker<double, double, double, double, double, double> _wistFastInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C", "D", "E"]);
        _wistFastInvoker = new DynamicMethodInvoker<double, double, double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<ExternalBenchContext5, double>();
        _nCalcContext = new ExternalBenchContext5();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate = dynamicExpressoInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double>>(
            DynamicExpressoFormula,
            "A", "B", "C", "D", "E");

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true)]
    public double CSharp_NoInliningMethod()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i]);
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
            sum += _dynamicExpressoDelegate(A[i], B[i], C[i], D[i], E[i]);
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
            _nCalcContext.D = D[i]; _nCalcContext.E = E[i];
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
            sum += _wistFastInvoker.Invoke(A[i], B[i], C[i], D[i], E[i]);
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e)
        => ((a * b) + (a * b) + (a * b) + (c * d)) / (e + 1.0);

    private double CSharpAt(int index) => CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index]);

    private double DynamicExpressoAt(int index) => _dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index]);

    private double NCalcAt(int index)
    {
        _nCalcContext.A = A[index]; _nCalcContext.B = B[index]; _nCalcContext.C = C[index];
        _nCalcContext.D = D[index]; _nCalcContext.E = E[index];
        return _nCalcLambda(_nCalcContext);
    }

    private double WistAt(int index) => _wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index]);
}
