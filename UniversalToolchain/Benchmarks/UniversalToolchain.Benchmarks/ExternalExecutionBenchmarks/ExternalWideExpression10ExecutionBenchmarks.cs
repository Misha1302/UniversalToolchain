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
public class ExternalWideExpression10ExecutionBenchmarks : ExternalArithmeticExecutionBenchmarkEnvironmentBase
{
    private const string WistFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J";
    private const string NCalcFormula = "([A] + [B] + [C] + [D]) * ([E] - [F] + [G]) / ([H] + 1.0) + [I] * [J]";
    private const string DynamicExpressoFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J";
    private const int InnerCount = 4096;

    private Func<double, double, double, double, double, double, double, double, double, double, double> _dynamicExpressoDelegate = null!;
    private ExternalBenchContext10 _nCalcContext = null!;
    private Func<ExternalBenchContext10, double> _nCalcLambda = null!;
    private DynamicMethodInvoker<double, double, double, double, double, double, double, double, double, double, double> _wistFastInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"]);
        _wistFastInvoker = new DynamicMethodInvoker<double, double, double, double, double, double, double, double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<ExternalBenchContext10, double>();
        _nCalcContext = new ExternalBenchContext10();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate = dynamicExpressoInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double, double, double, double, double>>(
            DynamicExpressoFormula,
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J");

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = InnerCount)]
    public double CSharp_NoInliningMethod()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i]);
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
            sum += _dynamicExpressoDelegate(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i]);
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
            _nCalcContext.D = D[i];
            _nCalcContext.E = E[i];
            _nCalcContext.F = F[i];
            _nCalcContext.G = G[i];
            _nCalcContext.H = H[i];
            _nCalcContext.I = I[i];
            _nCalcContext.J = J[i];
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
            sum += _wistFastInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i]);
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f, double g, double h, double i, double j)
        => (a + b + c + d) * (e - f + g) / (h + 1.0) + i * j;

    private double CSharpAt(int index) => CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index]);

    private double DynamicExpressoAt(int index) => _dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index]);

    private double NCalcAt(int index)
    {
        _nCalcContext.A = A[index];
        _nCalcContext.B = B[index];
        _nCalcContext.C = C[index];
        _nCalcContext.D = D[index];
        _nCalcContext.E = E[index];
        _nCalcContext.F = F[index];
        _nCalcContext.G = G[index];
        _nCalcContext.H = H[index];
        _nCalcContext.I = I[index];
        _nCalcContext.J = J[index];
        return _nCalcLambda(_nCalcContext);
    }

    private double WistAt(int index) => _wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index]);
}
