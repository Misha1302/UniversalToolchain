using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;
using DynamicExpresso;
using DynamicMethodCalling.Core;
using NCalc;
using NCalc.LambdaCompilation;

namespace UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks.Unrolled16;

[MemoryDiagnoser]
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class ExternalRepeatedSubexpressions5ExecutionUnrolled16Benchmarks : ExternalArithmeticExecutionUnrolled16BenchmarkEnvironmentBase
{
    private const string WistFormula = "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)";
    private const string NCalcFormula = "(([A] * [B]) + ([A] * [B]) + ([A] * [B]) + ([C] * [D])) / ([E] + 1.0)";
    private const string DynamicExpressoFormula = "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)";

    private ExternalBenchContext5Unrolled16 _nCalcContext = null!;
    private Func<ExternalBenchContext5Unrolled16, double> _nCalcLambda = null!;
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
        _nCalcLambda = nCalcExpression.ToLambda<ExternalBenchContext5Unrolled16, double>();
        _nCalcContext = new ExternalBenchContext5Unrolled16();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate =
            dynamicExpressoInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double>>(
                DynamicExpressoFormula,
                "A", "B", "C", "D", "E");

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 16)]
    public double CSharp_NoInliningMethod_Unrolled16()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i0], B[i0], C[i0], D[i0], E[i0]);

        var i1 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i1], B[i1], C[i1], D[i1], E[i1]);

        var i2 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i2], B[i2], C[i2], D[i2], E[i2]);

        var i3 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i3], B[i3], C[i3], D[i3], E[i3]);

        var i4 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i4], B[i4], C[i4], D[i4], E[i4]);

        var i5 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i5], B[i5], C[i5], D[i5], E[i5]);

        var i6 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i6], B[i6], C[i6], D[i6], E[i6]);

        var i7 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i7], B[i7], C[i7], D[i7], E[i7]);

        var i8 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i8], B[i8], C[i8], D[i8], E[i8]);

        var i9 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i9], B[i9], C[i9], D[i9], E[i9]);

        var i10 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i10], B[i10], C[i10], D[i10], E[i10]);

        var i11 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i11], B[i11], C[i11], D[i11], E[i11]);

        var i12 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i12], B[i12], C[i12], D[i12], E[i12]);

        var i13 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i13], B[i13], C[i13], D[i13], E[i13]);

        var i14 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i14], B[i14], C[i14], D[i14], E[i14]);

        var i15 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i15], B[i15], C[i15], D[i15], E[i15]);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double DynamicExpresso_Delegate_Unrolled16()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i0], B[i0], C[i0], D[i0], E[i0]);

        var i1 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i1], B[i1], C[i1], D[i1], E[i1]);

        var i2 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i2], B[i2], C[i2], D[i2], E[i2]);

        var i3 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i3], B[i3], C[i3], D[i3], E[i3]);

        var i4 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i4], B[i4], C[i4], D[i4], E[i4]);

        var i5 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i5], B[i5], C[i5], D[i5], E[i5]);

        var i6 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i6], B[i6], C[i6], D[i6], E[i6]);

        var i7 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i7], B[i7], C[i7], D[i7], E[i7]);

        var i8 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i8], B[i8], C[i8], D[i8], E[i8]);

        var i9 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i9], B[i9], C[i9], D[i9], E[i9]);

        var i10 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i10], B[i10], C[i10], D[i10], E[i10]);

        var i11 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i11], B[i11], C[i11], D[i11], E[i11]);

        var i12 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i12], B[i12], C[i12], D[i12], E[i12]);

        var i13 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i13], B[i13], C[i13], D[i13], E[i13]);

        var i14 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i14], B[i14], C[i14], D[i14], E[i14]);

        var i15 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i15], B[i15], C[i15], D[i15], E[i15]);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double NCalc_Lambda_Unrolled16()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        _nCalcContext.A = A[i0];
        _nCalcContext.B = B[i0];
        _nCalcContext.C = C[i0];
        _nCalcContext.D = D[i0];
        _nCalcContext.E = E[i0];
        sum += _nCalcLambda(_nCalcContext);

        var i1 = NextIndex();
        _nCalcContext.A = A[i1];
        _nCalcContext.B = B[i1];
        _nCalcContext.C = C[i1];
        _nCalcContext.D = D[i1];
        _nCalcContext.E = E[i1];
        sum += _nCalcLambda(_nCalcContext);

        var i2 = NextIndex();
        _nCalcContext.A = A[i2];
        _nCalcContext.B = B[i2];
        _nCalcContext.C = C[i2];
        _nCalcContext.D = D[i2];
        _nCalcContext.E = E[i2];
        sum += _nCalcLambda(_nCalcContext);

        var i3 = NextIndex();
        _nCalcContext.A = A[i3];
        _nCalcContext.B = B[i3];
        _nCalcContext.C = C[i3];
        _nCalcContext.D = D[i3];
        _nCalcContext.E = E[i3];
        sum += _nCalcLambda(_nCalcContext);

        var i4 = NextIndex();
        _nCalcContext.A = A[i4];
        _nCalcContext.B = B[i4];
        _nCalcContext.C = C[i4];
        _nCalcContext.D = D[i4];
        _nCalcContext.E = E[i4];
        sum += _nCalcLambda(_nCalcContext);

        var i5 = NextIndex();
        _nCalcContext.A = A[i5];
        _nCalcContext.B = B[i5];
        _nCalcContext.C = C[i5];
        _nCalcContext.D = D[i5];
        _nCalcContext.E = E[i5];
        sum += _nCalcLambda(_nCalcContext);

        var i6 = NextIndex();
        _nCalcContext.A = A[i6];
        _nCalcContext.B = B[i6];
        _nCalcContext.C = C[i6];
        _nCalcContext.D = D[i6];
        _nCalcContext.E = E[i6];
        sum += _nCalcLambda(_nCalcContext);

        var i7 = NextIndex();
        _nCalcContext.A = A[i7];
        _nCalcContext.B = B[i7];
        _nCalcContext.C = C[i7];
        _nCalcContext.D = D[i7];
        _nCalcContext.E = E[i7];
        sum += _nCalcLambda(_nCalcContext);

        var i8 = NextIndex();
        _nCalcContext.A = A[i8];
        _nCalcContext.B = B[i8];
        _nCalcContext.C = C[i8];
        _nCalcContext.D = D[i8];
        _nCalcContext.E = E[i8];
        sum += _nCalcLambda(_nCalcContext);

        var i9 = NextIndex();
        _nCalcContext.A = A[i9];
        _nCalcContext.B = B[i9];
        _nCalcContext.C = C[i9];
        _nCalcContext.D = D[i9];
        _nCalcContext.E = E[i9];
        sum += _nCalcLambda(_nCalcContext);

        var i10 = NextIndex();
        _nCalcContext.A = A[i10];
        _nCalcContext.B = B[i10];
        _nCalcContext.C = C[i10];
        _nCalcContext.D = D[i10];
        _nCalcContext.E = E[i10];
        sum += _nCalcLambda(_nCalcContext);

        var i11 = NextIndex();
        _nCalcContext.A = A[i11];
        _nCalcContext.B = B[i11];
        _nCalcContext.C = C[i11];
        _nCalcContext.D = D[i11];
        _nCalcContext.E = E[i11];
        sum += _nCalcLambda(_nCalcContext);

        var i12 = NextIndex();
        _nCalcContext.A = A[i12];
        _nCalcContext.B = B[i12];
        _nCalcContext.C = C[i12];
        _nCalcContext.D = D[i12];
        _nCalcContext.E = E[i12];
        sum += _nCalcLambda(_nCalcContext);

        var i13 = NextIndex();
        _nCalcContext.A = A[i13];
        _nCalcContext.B = B[i13];
        _nCalcContext.C = C[i13];
        _nCalcContext.D = D[i13];
        _nCalcContext.E = E[i13];
        sum += _nCalcLambda(_nCalcContext);

        var i14 = NextIndex();
        _nCalcContext.A = A[i14];
        _nCalcContext.B = B[i14];
        _nCalcContext.C = C[i14];
        _nCalcContext.D = D[i14];
        _nCalcContext.E = E[i14];
        sum += _nCalcLambda(_nCalcContext);

        var i15 = NextIndex();
        _nCalcContext.A = A[i15];
        _nCalcContext.B = B[i15];
        _nCalcContext.C = C[i15];
        _nCalcContext.D = D[i15];
        _nCalcContext.E = E[i15];
        sum += _nCalcLambda(_nCalcContext);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double Wist_Cil_FastInvoker_Unrolled16()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i0], B[i0], C[i0], D[i0], E[i0]);

        var i1 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i1], B[i1], C[i1], D[i1], E[i1]);

        var i2 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i2], B[i2], C[i2], D[i2], E[i2]);

        var i3 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i3], B[i3], C[i3], D[i3], E[i3]);

        var i4 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i4], B[i4], C[i4], D[i4], E[i4]);

        var i5 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i5], B[i5], C[i5], D[i5], E[i5]);

        var i6 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i6], B[i6], C[i6], D[i6], E[i6]);

        var i7 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i7], B[i7], C[i7], D[i7], E[i7]);

        var i8 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i8], B[i8], C[i8], D[i8], E[i8]);

        var i9 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i9], B[i9], C[i9], D[i9], E[i9]);

        var i10 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i10], B[i10], C[i10], D[i10], E[i10]);

        var i11 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i11], B[i11], C[i11], D[i11], E[i11]);

        var i12 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i12], B[i12], C[i12], D[i12], E[i12]);

        var i13 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i13], B[i13], C[i13], D[i13], E[i13]);

        var i14 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i14], B[i14], C[i14], D[i14], E[i14]);

        var i15 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i15], B[i15], C[i15], D[i15], E[i15]);

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e)
        => ((a * b) + (a * b) + (a * b) + (c * d)) / (e + 1.0);

    private double CSharpAt(int index)
        => CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index]);

    private double DynamicExpressoAt(int index)
        => _dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index]);

    private double NCalcAt(int index)
    {
            _nCalcContext.A = A[index];
            _nCalcContext.B = B[index];
            _nCalcContext.C = C[index];
            _nCalcContext.D = D[index];
            _nCalcContext.E = E[index];
        return _nCalcLambda(_nCalcContext);
    }

    private double WistAt(int index)
        => _wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index]);
}
