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
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class ExternalWideExpression11ExecutionUnrolled16Benchmarks : ExternalArithmeticExecutionUnrolled16BenchmarkEnvironmentBase
{
    private const string WistFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    private const string NCalcFormula = "([A] + [B] + [C] + [D]) * ([E] - [F] + [G]) / ([H] + 1.0) + [I] * [J] - [K] / 3.0";
    private const string DynamicExpressoFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";

    private ExternalBenchContext11Unrolled16 _nCalcContext = null!;
    private Func<ExternalBenchContext11Unrolled16, double> _nCalcLambda = null!;
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
        _nCalcLambda = nCalcExpression.ToLambda<ExternalBenchContext11Unrolled16, double>();
        _nCalcContext = new ExternalBenchContext11Unrolled16();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate =
            dynamicExpressoInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double, double, double, double, double, double>>(
                DynamicExpressoFormula,
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K");

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 16)]
    public double CSharp_NoInliningMethod_Unrolled16()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i0], B[i0], C[i0], D[i0], E[i0], F[i0], G[i0], H[i0], I[i0], J[i0], K[i0]);

        var i1 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i1], B[i1], C[i1], D[i1], E[i1], F[i1], G[i1], H[i1], I[i1], J[i1], K[i1]);

        var i2 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i2], B[i2], C[i2], D[i2], E[i2], F[i2], G[i2], H[i2], I[i2], J[i2], K[i2]);

        var i3 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i3], B[i3], C[i3], D[i3], E[i3], F[i3], G[i3], H[i3], I[i3], J[i3], K[i3]);

        var i4 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i4], B[i4], C[i4], D[i4], E[i4], F[i4], G[i4], H[i4], I[i4], J[i4], K[i4]);

        var i5 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i5], B[i5], C[i5], D[i5], E[i5], F[i5], G[i5], H[i5], I[i5], J[i5], K[i5]);

        var i6 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i6], B[i6], C[i6], D[i6], E[i6], F[i6], G[i6], H[i6], I[i6], J[i6], K[i6]);

        var i7 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i7], B[i7], C[i7], D[i7], E[i7], F[i7], G[i7], H[i7], I[i7], J[i7], K[i7]);

        var i8 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i8], B[i8], C[i8], D[i8], E[i8], F[i8], G[i8], H[i8], I[i8], J[i8], K[i8]);

        var i9 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i9], B[i9], C[i9], D[i9], E[i9], F[i9], G[i9], H[i9], I[i9], J[i9], K[i9]);

        var i10 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i10], B[i10], C[i10], D[i10], E[i10], F[i10], G[i10], H[i10], I[i10], J[i10], K[i10]);

        var i11 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i11], B[i11], C[i11], D[i11], E[i11], F[i11], G[i11], H[i11], I[i11], J[i11], K[i11]);

        var i12 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i12], B[i12], C[i12], D[i12], E[i12], F[i12], G[i12], H[i12], I[i12], J[i12], K[i12]);

        var i13 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i13], B[i13], C[i13], D[i13], E[i13], F[i13], G[i13], H[i13], I[i13], J[i13], K[i13]);

        var i14 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i14], B[i14], C[i14], D[i14], E[i14], F[i14], G[i14], H[i14], I[i14], J[i14], K[i14]);

        var i15 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i15], B[i15], C[i15], D[i15], E[i15], F[i15], G[i15], H[i15], I[i15], J[i15], K[i15]);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double DynamicExpresso_Delegate_Unrolled16()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i0], B[i0], C[i0], D[i0], E[i0], F[i0], G[i0], H[i0], I[i0], J[i0], K[i0]);

        var i1 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i1], B[i1], C[i1], D[i1], E[i1], F[i1], G[i1], H[i1], I[i1], J[i1], K[i1]);

        var i2 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i2], B[i2], C[i2], D[i2], E[i2], F[i2], G[i2], H[i2], I[i2], J[i2], K[i2]);

        var i3 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i3], B[i3], C[i3], D[i3], E[i3], F[i3], G[i3], H[i3], I[i3], J[i3], K[i3]);

        var i4 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i4], B[i4], C[i4], D[i4], E[i4], F[i4], G[i4], H[i4], I[i4], J[i4], K[i4]);

        var i5 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i5], B[i5], C[i5], D[i5], E[i5], F[i5], G[i5], H[i5], I[i5], J[i5], K[i5]);

        var i6 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i6], B[i6], C[i6], D[i6], E[i6], F[i6], G[i6], H[i6], I[i6], J[i6], K[i6]);

        var i7 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i7], B[i7], C[i7], D[i7], E[i7], F[i7], G[i7], H[i7], I[i7], J[i7], K[i7]);

        var i8 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i8], B[i8], C[i8], D[i8], E[i8], F[i8], G[i8], H[i8], I[i8], J[i8], K[i8]);

        var i9 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i9], B[i9], C[i9], D[i9], E[i9], F[i9], G[i9], H[i9], I[i9], J[i9], K[i9]);

        var i10 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i10], B[i10], C[i10], D[i10], E[i10], F[i10], G[i10], H[i10], I[i10], J[i10], K[i10]);

        var i11 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i11], B[i11], C[i11], D[i11], E[i11], F[i11], G[i11], H[i11], I[i11], J[i11], K[i11]);

        var i12 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i12], B[i12], C[i12], D[i12], E[i12], F[i12], G[i12], H[i12], I[i12], J[i12], K[i12]);

        var i13 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i13], B[i13], C[i13], D[i13], E[i13], F[i13], G[i13], H[i13], I[i13], J[i13], K[i13]);

        var i14 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i14], B[i14], C[i14], D[i14], E[i14], F[i14], G[i14], H[i14], I[i14], J[i14], K[i14]);

        var i15 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i15], B[i15], C[i15], D[i15], E[i15], F[i15], G[i15], H[i15], I[i15], J[i15], K[i15]);

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
        _nCalcContext.F = F[i0];
        _nCalcContext.G = G[i0];
        _nCalcContext.H = H[i0];
        _nCalcContext.I = I[i0];
        _nCalcContext.J = J[i0];
        _nCalcContext.K = K[i0];
        sum += _nCalcLambda(_nCalcContext);

        var i1 = NextIndex();
        _nCalcContext.A = A[i1];
        _nCalcContext.B = B[i1];
        _nCalcContext.C = C[i1];
        _nCalcContext.D = D[i1];
        _nCalcContext.E = E[i1];
        _nCalcContext.F = F[i1];
        _nCalcContext.G = G[i1];
        _nCalcContext.H = H[i1];
        _nCalcContext.I = I[i1];
        _nCalcContext.J = J[i1];
        _nCalcContext.K = K[i1];
        sum += _nCalcLambda(_nCalcContext);

        var i2 = NextIndex();
        _nCalcContext.A = A[i2];
        _nCalcContext.B = B[i2];
        _nCalcContext.C = C[i2];
        _nCalcContext.D = D[i2];
        _nCalcContext.E = E[i2];
        _nCalcContext.F = F[i2];
        _nCalcContext.G = G[i2];
        _nCalcContext.H = H[i2];
        _nCalcContext.I = I[i2];
        _nCalcContext.J = J[i2];
        _nCalcContext.K = K[i2];
        sum += _nCalcLambda(_nCalcContext);

        var i3 = NextIndex();
        _nCalcContext.A = A[i3];
        _nCalcContext.B = B[i3];
        _nCalcContext.C = C[i3];
        _nCalcContext.D = D[i3];
        _nCalcContext.E = E[i3];
        _nCalcContext.F = F[i3];
        _nCalcContext.G = G[i3];
        _nCalcContext.H = H[i3];
        _nCalcContext.I = I[i3];
        _nCalcContext.J = J[i3];
        _nCalcContext.K = K[i3];
        sum += _nCalcLambda(_nCalcContext);

        var i4 = NextIndex();
        _nCalcContext.A = A[i4];
        _nCalcContext.B = B[i4];
        _nCalcContext.C = C[i4];
        _nCalcContext.D = D[i4];
        _nCalcContext.E = E[i4];
        _nCalcContext.F = F[i4];
        _nCalcContext.G = G[i4];
        _nCalcContext.H = H[i4];
        _nCalcContext.I = I[i4];
        _nCalcContext.J = J[i4];
        _nCalcContext.K = K[i4];
        sum += _nCalcLambda(_nCalcContext);

        var i5 = NextIndex();
        _nCalcContext.A = A[i5];
        _nCalcContext.B = B[i5];
        _nCalcContext.C = C[i5];
        _nCalcContext.D = D[i5];
        _nCalcContext.E = E[i5];
        _nCalcContext.F = F[i5];
        _nCalcContext.G = G[i5];
        _nCalcContext.H = H[i5];
        _nCalcContext.I = I[i5];
        _nCalcContext.J = J[i5];
        _nCalcContext.K = K[i5];
        sum += _nCalcLambda(_nCalcContext);

        var i6 = NextIndex();
        _nCalcContext.A = A[i6];
        _nCalcContext.B = B[i6];
        _nCalcContext.C = C[i6];
        _nCalcContext.D = D[i6];
        _nCalcContext.E = E[i6];
        _nCalcContext.F = F[i6];
        _nCalcContext.G = G[i6];
        _nCalcContext.H = H[i6];
        _nCalcContext.I = I[i6];
        _nCalcContext.J = J[i6];
        _nCalcContext.K = K[i6];
        sum += _nCalcLambda(_nCalcContext);

        var i7 = NextIndex();
        _nCalcContext.A = A[i7];
        _nCalcContext.B = B[i7];
        _nCalcContext.C = C[i7];
        _nCalcContext.D = D[i7];
        _nCalcContext.E = E[i7];
        _nCalcContext.F = F[i7];
        _nCalcContext.G = G[i7];
        _nCalcContext.H = H[i7];
        _nCalcContext.I = I[i7];
        _nCalcContext.J = J[i7];
        _nCalcContext.K = K[i7];
        sum += _nCalcLambda(_nCalcContext);

        var i8 = NextIndex();
        _nCalcContext.A = A[i8];
        _nCalcContext.B = B[i8];
        _nCalcContext.C = C[i8];
        _nCalcContext.D = D[i8];
        _nCalcContext.E = E[i8];
        _nCalcContext.F = F[i8];
        _nCalcContext.G = G[i8];
        _nCalcContext.H = H[i8];
        _nCalcContext.I = I[i8];
        _nCalcContext.J = J[i8];
        _nCalcContext.K = K[i8];
        sum += _nCalcLambda(_nCalcContext);

        var i9 = NextIndex();
        _nCalcContext.A = A[i9];
        _nCalcContext.B = B[i9];
        _nCalcContext.C = C[i9];
        _nCalcContext.D = D[i9];
        _nCalcContext.E = E[i9];
        _nCalcContext.F = F[i9];
        _nCalcContext.G = G[i9];
        _nCalcContext.H = H[i9];
        _nCalcContext.I = I[i9];
        _nCalcContext.J = J[i9];
        _nCalcContext.K = K[i9];
        sum += _nCalcLambda(_nCalcContext);

        var i10 = NextIndex();
        _nCalcContext.A = A[i10];
        _nCalcContext.B = B[i10];
        _nCalcContext.C = C[i10];
        _nCalcContext.D = D[i10];
        _nCalcContext.E = E[i10];
        _nCalcContext.F = F[i10];
        _nCalcContext.G = G[i10];
        _nCalcContext.H = H[i10];
        _nCalcContext.I = I[i10];
        _nCalcContext.J = J[i10];
        _nCalcContext.K = K[i10];
        sum += _nCalcLambda(_nCalcContext);

        var i11 = NextIndex();
        _nCalcContext.A = A[i11];
        _nCalcContext.B = B[i11];
        _nCalcContext.C = C[i11];
        _nCalcContext.D = D[i11];
        _nCalcContext.E = E[i11];
        _nCalcContext.F = F[i11];
        _nCalcContext.G = G[i11];
        _nCalcContext.H = H[i11];
        _nCalcContext.I = I[i11];
        _nCalcContext.J = J[i11];
        _nCalcContext.K = K[i11];
        sum += _nCalcLambda(_nCalcContext);

        var i12 = NextIndex();
        _nCalcContext.A = A[i12];
        _nCalcContext.B = B[i12];
        _nCalcContext.C = C[i12];
        _nCalcContext.D = D[i12];
        _nCalcContext.E = E[i12];
        _nCalcContext.F = F[i12];
        _nCalcContext.G = G[i12];
        _nCalcContext.H = H[i12];
        _nCalcContext.I = I[i12];
        _nCalcContext.J = J[i12];
        _nCalcContext.K = K[i12];
        sum += _nCalcLambda(_nCalcContext);

        var i13 = NextIndex();
        _nCalcContext.A = A[i13];
        _nCalcContext.B = B[i13];
        _nCalcContext.C = C[i13];
        _nCalcContext.D = D[i13];
        _nCalcContext.E = E[i13];
        _nCalcContext.F = F[i13];
        _nCalcContext.G = G[i13];
        _nCalcContext.H = H[i13];
        _nCalcContext.I = I[i13];
        _nCalcContext.J = J[i13];
        _nCalcContext.K = K[i13];
        sum += _nCalcLambda(_nCalcContext);

        var i14 = NextIndex();
        _nCalcContext.A = A[i14];
        _nCalcContext.B = B[i14];
        _nCalcContext.C = C[i14];
        _nCalcContext.D = D[i14];
        _nCalcContext.E = E[i14];
        _nCalcContext.F = F[i14];
        _nCalcContext.G = G[i14];
        _nCalcContext.H = H[i14];
        _nCalcContext.I = I[i14];
        _nCalcContext.J = J[i14];
        _nCalcContext.K = K[i14];
        sum += _nCalcLambda(_nCalcContext);

        var i15 = NextIndex();
        _nCalcContext.A = A[i15];
        _nCalcContext.B = B[i15];
        _nCalcContext.C = C[i15];
        _nCalcContext.D = D[i15];
        _nCalcContext.E = E[i15];
        _nCalcContext.F = F[i15];
        _nCalcContext.G = G[i15];
        _nCalcContext.H = H[i15];
        _nCalcContext.I = I[i15];
        _nCalcContext.J = J[i15];
        _nCalcContext.K = K[i15];
        sum += _nCalcLambda(_nCalcContext);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double Wist_Cil_FastInvoker_Unrolled16()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i0], B[i0], C[i0], D[i0], E[i0], F[i0], G[i0], H[i0], I[i0], J[i0], K[i0]);

        var i1 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i1], B[i1], C[i1], D[i1], E[i1], F[i1], G[i1], H[i1], I[i1], J[i1], K[i1]);

        var i2 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i2], B[i2], C[i2], D[i2], E[i2], F[i2], G[i2], H[i2], I[i2], J[i2], K[i2]);

        var i3 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i3], B[i3], C[i3], D[i3], E[i3], F[i3], G[i3], H[i3], I[i3], J[i3], K[i3]);

        var i4 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i4], B[i4], C[i4], D[i4], E[i4], F[i4], G[i4], H[i4], I[i4], J[i4], K[i4]);

        var i5 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i5], B[i5], C[i5], D[i5], E[i5], F[i5], G[i5], H[i5], I[i5], J[i5], K[i5]);

        var i6 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i6], B[i6], C[i6], D[i6], E[i6], F[i6], G[i6], H[i6], I[i6], J[i6], K[i6]);

        var i7 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i7], B[i7], C[i7], D[i7], E[i7], F[i7], G[i7], H[i7], I[i7], J[i7], K[i7]);

        var i8 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i8], B[i8], C[i8], D[i8], E[i8], F[i8], G[i8], H[i8], I[i8], J[i8], K[i8]);

        var i9 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i9], B[i9], C[i9], D[i9], E[i9], F[i9], G[i9], H[i9], I[i9], J[i9], K[i9]);

        var i10 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i10], B[i10], C[i10], D[i10], E[i10], F[i10], G[i10], H[i10], I[i10], J[i10], K[i10]);

        var i11 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i11], B[i11], C[i11], D[i11], E[i11], F[i11], G[i11], H[i11], I[i11], J[i11], K[i11]);

        var i12 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i12], B[i12], C[i12], D[i12], E[i12], F[i12], G[i12], H[i12], I[i12], J[i12], K[i12]);

        var i13 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i13], B[i13], C[i13], D[i13], E[i13], F[i13], G[i13], H[i13], I[i13], J[i13], K[i13]);

        var i14 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i14], B[i14], C[i14], D[i14], E[i14], F[i14], G[i14], H[i14], I[i14], J[i14], K[i14]);

        var i15 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i15], B[i15], C[i15], D[i15], E[i15], F[i15], G[i15], H[i15], I[i15], J[i15], K[i15]);

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f, double g, double h, double i, double j, double k)
        => (a + b + c + d) * (e - f + g) / (h + 1.0) + i * j - k / 3.0;

    private double CSharpAt(int index)
        => CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);

    private double DynamicExpressoAt(int index)
        => _dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);

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
            _nCalcContext.K = K[index];
        return _nCalcLambda(_nCalcContext);
    }

    private double WistAt(int index)
        => _wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);
}
