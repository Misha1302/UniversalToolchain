using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DynamicExpresso;
using DynamicMethodCalling.Core;
using NCalc;
using NCalc.LambdaCompilation;

namespace UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ExternalSimple3ExecutionBenchmarks : ExternalArithmeticExecutionBenchmarkEnvironmentBase
{
    private const string WistFormula = "A + B * C / 5.0";
    private const string NCalcFormula = "[A] + [B] * [C] / 5.0";
    private const string DynamicExpressoFormula = "A + B * C / 5.0";
    private const int InnerCount = 4096;
    private Func<double, double, double, double> _dynamicExpressoDelegate = null!;

    private ExternalBenchContext3 _nCalcContext = null!;
    private Func<ExternalBenchContext3, double> _nCalcLambda = null!;
    private DynamicMethodInvoker<double, double, double, double> _wistFastInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C"]);
        _wistFastInvoker = new DynamicMethodInvoker<double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<ExternalBenchContext3, double>();
        _nCalcContext = new ExternalBenchContext3();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate =
            dynamicExpressoInterpreter.ParseAsDelegate<Func<double, double, double, double>>(
                DynamicExpressoFormula,
                "A",
                "B",
                "C");

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = InnerCount)]
    public double CSharp_NoInliningMethod()
    {
        var sum = 0.0;

        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += CSharp_NoInliningMethodCore(A[i], B[i], C[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = InnerCount)]
    public double DynamicExpresso_CompiledDelegate()
    {
        var sum = 0.0;

        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += _dynamicExpressoDelegate(A[i], B[i], C[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = InnerCount)]
    public double NCalc_CompiledLambda()
    {
        var sum = 0.0;

        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();

            _nCalcContext.A = A[i];
            _nCalcContext.B = B[i];
            _nCalcContext.C = C[i];

            sum += _nCalcLambda(_nCalcContext);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = InnerCount)]
    public double Wist_Cil_DynamicMethodFastInvoker()
    {
        var sum = 0.0;

        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += _wistFastInvoker.Invoke(A[i], B[i], C[i]);
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c) => a + b * c / 5.0;

    private double CSharpAt(int index) => CSharp_NoInliningMethodCore(A[index], B[index], C[index]);

    private double DynamicExpressoAt(int index) => _dynamicExpressoDelegate(A[index], B[index], C[index]);

    private double NCalcAt(int index)
    {
        _nCalcContext.A = A[index];
        _nCalcContext.B = B[index];
        _nCalcContext.C = C[index];

        return _nCalcLambda(_nCalcContext);
    }

    private double WistAt(int index) => _wistFastInvoker.Invoke(A[index], B[index], C[index]);
}