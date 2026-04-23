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
[DryJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class ExternalWideExpression11ExecutionBenchmarks : ExternalArithmeticExecutionBenchmarkEnvironmentBase
{
    private const string WistFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    private const string NCalcFormula = "([A] + [B] + [C] + [D]) * ([E] - [F] + [G]) / ([H] + 1.0) + [I] * [J] - [K] / 3.0";
    private const string DynamicExpressoFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    private const int InnerCount = 4096;

    private ExternalBenchContext11 _nCalcContext = null!;
    private Func<ExternalBenchContext11, double> _nCalcLambda = null!;
    private Func<double, double, double, double, double, double, double, double, double, double, double, double> _dynamicExpressoDelegate = null!;
    private DynamicMethodInvoker<double, double, double, double, double, double, double, double, double, double, double, double> _wistFastInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K"]);
        _wistFastInvoker = new DynamicMethodInvoker<double, double, double, double, double, double, double, double, double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<ExternalBenchContext11, double>();
        _nCalcContext = new ExternalBenchContext11();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate = dynamicExpressoInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double, double, double, double, double, double>>(
            DynamicExpressoFormula,
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K");

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true)]
    public double CSharp_NoInliningMethod()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i], K[i]);
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
            sum += _dynamicExpressoDelegate(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i], K[i]);
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
            _nCalcContext.A = A[i]; _nCalcContext.B = B[i]; _nCalcContext.C = C[i]; _nCalcContext.D = D[i];
            _nCalcContext.E = E[i]; _nCalcContext.F = F[i]; _nCalcContext.G = G[i]; _nCalcContext.H = H[i];
            _nCalcContext.I = I[i]; _nCalcContext.J = J[i]; _nCalcContext.K = K[i];
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
            sum += _wistFastInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i], K[i]);
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f, double g, double h, double i, double j, double k)
        => (a + b + c + d) * (e - f + g) / (h + 1.0) + i * j - k / 3.0;

    private double CSharpAt(int index) => CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);

    private double DynamicExpressoAt(int index) => _dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);

    private double NCalcAt(int index)
    {
        _nCalcContext.A = A[index]; _nCalcContext.B = B[index]; _nCalcContext.C = C[index]; _nCalcContext.D = D[index];
        _nCalcContext.E = E[index]; _nCalcContext.F = F[index]; _nCalcContext.G = G[index]; _nCalcContext.H = H[index];
        _nCalcContext.I = I[index]; _nCalcContext.J = J[index]; _nCalcContext.K = K[index];
        return _nCalcLambda(_nCalcContext);
    }

    private double WistAt(int index) => _wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);
}
