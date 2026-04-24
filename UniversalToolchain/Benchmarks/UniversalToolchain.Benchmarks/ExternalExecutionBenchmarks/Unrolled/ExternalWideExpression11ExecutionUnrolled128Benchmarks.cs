using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;
using DynamicExpresso;
using DynamicMethodCalling.Core;
using NCalc;
using NCalc.LambdaCompilation;

namespace UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks.Unrolled;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ExternalWideExpression11ExecutionUnrolled128Benchmarks : ExternalArithmeticExecutionUnrolledBenchmarkEnvironmentBase
{
    private const string WistFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    private const string NCalcFormula = "([A] + [B] + [C] + [D]) * ([E] - [F] + [G]) / ([H] + 1.0) + [I] * [J] - [K] / 3.0";
    private const string DynamicExpressoFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";

    private ExternalBenchContext11Unrolled _nCalcContext = null!;
    private Func<ExternalBenchContext11Unrolled, double> _nCalcLambda = null!;
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
        _nCalcLambda = nCalcExpression.ToLambda<ExternalBenchContext11Unrolled, double>();
        _nCalcContext = new ExternalBenchContext11Unrolled();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate =
            dynamicExpressoInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double, double, double, double, double, double>>(
                DynamicExpressoFormula,
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K");

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 16)]
    public double CSharp_NoInliningMethod_Unrolled128()
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

        var i16 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i16], B[i16], C[i16], D[i16], E[i16], F[i16], G[i16], H[i16], I[i16], J[i16], K[i16]);

        var i17 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i17], B[i17], C[i17], D[i17], E[i17], F[i17], G[i17], H[i17], I[i17], J[i17], K[i17]);

        var i18 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i18], B[i18], C[i18], D[i18], E[i18], F[i18], G[i18], H[i18], I[i18], J[i18], K[i18]);

        var i19 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i19], B[i19], C[i19], D[i19], E[i19], F[i19], G[i19], H[i19], I[i19], J[i19], K[i19]);

        var i20 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i20], B[i20], C[i20], D[i20], E[i20], F[i20], G[i20], H[i20], I[i20], J[i20], K[i20]);

        var i21 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i21], B[i21], C[i21], D[i21], E[i21], F[i21], G[i21], H[i21], I[i21], J[i21], K[i21]);

        var i22 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i22], B[i22], C[i22], D[i22], E[i22], F[i22], G[i22], H[i22], I[i22], J[i22], K[i22]);

        var i23 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i23], B[i23], C[i23], D[i23], E[i23], F[i23], G[i23], H[i23], I[i23], J[i23], K[i23]);

        var i24 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i24], B[i24], C[i24], D[i24], E[i24], F[i24], G[i24], H[i24], I[i24], J[i24], K[i24]);

        var i25 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i25], B[i25], C[i25], D[i25], E[i25], F[i25], G[i25], H[i25], I[i25], J[i25], K[i25]);

        var i26 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i26], B[i26], C[i26], D[i26], E[i26], F[i26], G[i26], H[i26], I[i26], J[i26], K[i26]);

        var i27 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i27], B[i27], C[i27], D[i27], E[i27], F[i27], G[i27], H[i27], I[i27], J[i27], K[i27]);

        var i28 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i28], B[i28], C[i28], D[i28], E[i28], F[i28], G[i28], H[i28], I[i28], J[i28], K[i28]);

        var i29 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i29], B[i29], C[i29], D[i29], E[i29], F[i29], G[i29], H[i29], I[i29], J[i29], K[i29]);

        var i30 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i30], B[i30], C[i30], D[i30], E[i30], F[i30], G[i30], H[i30], I[i30], J[i30], K[i30]);

        var i31 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i31], B[i31], C[i31], D[i31], E[i31], F[i31], G[i31], H[i31], I[i31], J[i31], K[i31]);

        var i32 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i32], B[i32], C[i32], D[i32], E[i32], F[i32], G[i32], H[i32], I[i32], J[i32], K[i32]);

        var i33 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i33], B[i33], C[i33], D[i33], E[i33], F[i33], G[i33], H[i33], I[i33], J[i33], K[i33]);

        var i34 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i34], B[i34], C[i34], D[i34], E[i34], F[i34], G[i34], H[i34], I[i34], J[i34], K[i34]);

        var i35 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i35], B[i35], C[i35], D[i35], E[i35], F[i35], G[i35], H[i35], I[i35], J[i35], K[i35]);

        var i36 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i36], B[i36], C[i36], D[i36], E[i36], F[i36], G[i36], H[i36], I[i36], J[i36], K[i36]);

        var i37 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i37], B[i37], C[i37], D[i37], E[i37], F[i37], G[i37], H[i37], I[i37], J[i37], K[i37]);

        var i38 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i38], B[i38], C[i38], D[i38], E[i38], F[i38], G[i38], H[i38], I[i38], J[i38], K[i38]);

        var i39 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i39], B[i39], C[i39], D[i39], E[i39], F[i39], G[i39], H[i39], I[i39], J[i39], K[i39]);

        var i40 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i40], B[i40], C[i40], D[i40], E[i40], F[i40], G[i40], H[i40], I[i40], J[i40], K[i40]);

        var i41 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i41], B[i41], C[i41], D[i41], E[i41], F[i41], G[i41], H[i41], I[i41], J[i41], K[i41]);

        var i42 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i42], B[i42], C[i42], D[i42], E[i42], F[i42], G[i42], H[i42], I[i42], J[i42], K[i42]);

        var i43 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i43], B[i43], C[i43], D[i43], E[i43], F[i43], G[i43], H[i43], I[i43], J[i43], K[i43]);

        var i44 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i44], B[i44], C[i44], D[i44], E[i44], F[i44], G[i44], H[i44], I[i44], J[i44], K[i44]);

        var i45 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i45], B[i45], C[i45], D[i45], E[i45], F[i45], G[i45], H[i45], I[i45], J[i45], K[i45]);

        var i46 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i46], B[i46], C[i46], D[i46], E[i46], F[i46], G[i46], H[i46], I[i46], J[i46], K[i46]);

        var i47 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i47], B[i47], C[i47], D[i47], E[i47], F[i47], G[i47], H[i47], I[i47], J[i47], K[i47]);

        var i48 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i48], B[i48], C[i48], D[i48], E[i48], F[i48], G[i48], H[i48], I[i48], J[i48], K[i48]);

        var i49 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i49], B[i49], C[i49], D[i49], E[i49], F[i49], G[i49], H[i49], I[i49], J[i49], K[i49]);

        var i50 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i50], B[i50], C[i50], D[i50], E[i50], F[i50], G[i50], H[i50], I[i50], J[i50], K[i50]);

        var i51 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i51], B[i51], C[i51], D[i51], E[i51], F[i51], G[i51], H[i51], I[i51], J[i51], K[i51]);

        var i52 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i52], B[i52], C[i52], D[i52], E[i52], F[i52], G[i52], H[i52], I[i52], J[i52], K[i52]);

        var i53 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i53], B[i53], C[i53], D[i53], E[i53], F[i53], G[i53], H[i53], I[i53], J[i53], K[i53]);

        var i54 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i54], B[i54], C[i54], D[i54], E[i54], F[i54], G[i54], H[i54], I[i54], J[i54], K[i54]);

        var i55 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i55], B[i55], C[i55], D[i55], E[i55], F[i55], G[i55], H[i55], I[i55], J[i55], K[i55]);

        var i56 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i56], B[i56], C[i56], D[i56], E[i56], F[i56], G[i56], H[i56], I[i56], J[i56], K[i56]);

        var i57 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i57], B[i57], C[i57], D[i57], E[i57], F[i57], G[i57], H[i57], I[i57], J[i57], K[i57]);

        var i58 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i58], B[i58], C[i58], D[i58], E[i58], F[i58], G[i58], H[i58], I[i58], J[i58], K[i58]);

        var i59 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i59], B[i59], C[i59], D[i59], E[i59], F[i59], G[i59], H[i59], I[i59], J[i59], K[i59]);

        var i60 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i60], B[i60], C[i60], D[i60], E[i60], F[i60], G[i60], H[i60], I[i60], J[i60], K[i60]);

        var i61 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i61], B[i61], C[i61], D[i61], E[i61], F[i61], G[i61], H[i61], I[i61], J[i61], K[i61]);

        var i62 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i62], B[i62], C[i62], D[i62], E[i62], F[i62], G[i62], H[i62], I[i62], J[i62], K[i62]);

        var i63 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i63], B[i63], C[i63], D[i63], E[i63], F[i63], G[i63], H[i63], I[i63], J[i63], K[i63]);

        var i64 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i64], B[i64], C[i64], D[i64], E[i64], F[i64], G[i64], H[i64], I[i64], J[i64], K[i64]);

        var i65 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i65], B[i65], C[i65], D[i65], E[i65], F[i65], G[i65], H[i65], I[i65], J[i65], K[i65]);

        var i66 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i66], B[i66], C[i66], D[i66], E[i66], F[i66], G[i66], H[i66], I[i66], J[i66], K[i66]);

        var i67 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i67], B[i67], C[i67], D[i67], E[i67], F[i67], G[i67], H[i67], I[i67], J[i67], K[i67]);

        var i68 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i68], B[i68], C[i68], D[i68], E[i68], F[i68], G[i68], H[i68], I[i68], J[i68], K[i68]);

        var i69 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i69], B[i69], C[i69], D[i69], E[i69], F[i69], G[i69], H[i69], I[i69], J[i69], K[i69]);

        var i70 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i70], B[i70], C[i70], D[i70], E[i70], F[i70], G[i70], H[i70], I[i70], J[i70], K[i70]);

        var i71 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i71], B[i71], C[i71], D[i71], E[i71], F[i71], G[i71], H[i71], I[i71], J[i71], K[i71]);

        var i72 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i72], B[i72], C[i72], D[i72], E[i72], F[i72], G[i72], H[i72], I[i72], J[i72], K[i72]);

        var i73 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i73], B[i73], C[i73], D[i73], E[i73], F[i73], G[i73], H[i73], I[i73], J[i73], K[i73]);

        var i74 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i74], B[i74], C[i74], D[i74], E[i74], F[i74], G[i74], H[i74], I[i74], J[i74], K[i74]);

        var i75 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i75], B[i75], C[i75], D[i75], E[i75], F[i75], G[i75], H[i75], I[i75], J[i75], K[i75]);

        var i76 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i76], B[i76], C[i76], D[i76], E[i76], F[i76], G[i76], H[i76], I[i76], J[i76], K[i76]);

        var i77 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i77], B[i77], C[i77], D[i77], E[i77], F[i77], G[i77], H[i77], I[i77], J[i77], K[i77]);

        var i78 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i78], B[i78], C[i78], D[i78], E[i78], F[i78], G[i78], H[i78], I[i78], J[i78], K[i78]);

        var i79 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i79], B[i79], C[i79], D[i79], E[i79], F[i79], G[i79], H[i79], I[i79], J[i79], K[i79]);

        var i80 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i80], B[i80], C[i80], D[i80], E[i80], F[i80], G[i80], H[i80], I[i80], J[i80], K[i80]);

        var i81 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i81], B[i81], C[i81], D[i81], E[i81], F[i81], G[i81], H[i81], I[i81], J[i81], K[i81]);

        var i82 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i82], B[i82], C[i82], D[i82], E[i82], F[i82], G[i82], H[i82], I[i82], J[i82], K[i82]);

        var i83 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i83], B[i83], C[i83], D[i83], E[i83], F[i83], G[i83], H[i83], I[i83], J[i83], K[i83]);

        var i84 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i84], B[i84], C[i84], D[i84], E[i84], F[i84], G[i84], H[i84], I[i84], J[i84], K[i84]);

        var i85 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i85], B[i85], C[i85], D[i85], E[i85], F[i85], G[i85], H[i85], I[i85], J[i85], K[i85]);

        var i86 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i86], B[i86], C[i86], D[i86], E[i86], F[i86], G[i86], H[i86], I[i86], J[i86], K[i86]);

        var i87 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i87], B[i87], C[i87], D[i87], E[i87], F[i87], G[i87], H[i87], I[i87], J[i87], K[i87]);

        var i88 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i88], B[i88], C[i88], D[i88], E[i88], F[i88], G[i88], H[i88], I[i88], J[i88], K[i88]);

        var i89 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i89], B[i89], C[i89], D[i89], E[i89], F[i89], G[i89], H[i89], I[i89], J[i89], K[i89]);

        var i90 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i90], B[i90], C[i90], D[i90], E[i90], F[i90], G[i90], H[i90], I[i90], J[i90], K[i90]);

        var i91 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i91], B[i91], C[i91], D[i91], E[i91], F[i91], G[i91], H[i91], I[i91], J[i91], K[i91]);

        var i92 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i92], B[i92], C[i92], D[i92], E[i92], F[i92], G[i92], H[i92], I[i92], J[i92], K[i92]);

        var i93 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i93], B[i93], C[i93], D[i93], E[i93], F[i93], G[i93], H[i93], I[i93], J[i93], K[i93]);

        var i94 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i94], B[i94], C[i94], D[i94], E[i94], F[i94], G[i94], H[i94], I[i94], J[i94], K[i94]);

        var i95 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i95], B[i95], C[i95], D[i95], E[i95], F[i95], G[i95], H[i95], I[i95], J[i95], K[i95]);

        var i96 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i96], B[i96], C[i96], D[i96], E[i96], F[i96], G[i96], H[i96], I[i96], J[i96], K[i96]);

        var i97 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i97], B[i97], C[i97], D[i97], E[i97], F[i97], G[i97], H[i97], I[i97], J[i97], K[i97]);

        var i98 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i98], B[i98], C[i98], D[i98], E[i98], F[i98], G[i98], H[i98], I[i98], J[i98], K[i98]);

        var i99 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i99], B[i99], C[i99], D[i99], E[i99], F[i99], G[i99], H[i99], I[i99], J[i99], K[i99]);

        var i100 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i100], B[i100], C[i100], D[i100], E[i100], F[i100], G[i100], H[i100], I[i100], J[i100], K[i100]);

        var i101 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i101], B[i101], C[i101], D[i101], E[i101], F[i101], G[i101], H[i101], I[i101], J[i101], K[i101]);

        var i102 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i102], B[i102], C[i102], D[i102], E[i102], F[i102], G[i102], H[i102], I[i102], J[i102], K[i102]);

        var i103 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i103], B[i103], C[i103], D[i103], E[i103], F[i103], G[i103], H[i103], I[i103], J[i103], K[i103]);

        var i104 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i104], B[i104], C[i104], D[i104], E[i104], F[i104], G[i104], H[i104], I[i104], J[i104], K[i104]);

        var i105 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i105], B[i105], C[i105], D[i105], E[i105], F[i105], G[i105], H[i105], I[i105], J[i105], K[i105]);

        var i106 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i106], B[i106], C[i106], D[i106], E[i106], F[i106], G[i106], H[i106], I[i106], J[i106], K[i106]);

        var i107 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i107], B[i107], C[i107], D[i107], E[i107], F[i107], G[i107], H[i107], I[i107], J[i107], K[i107]);

        var i108 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i108], B[i108], C[i108], D[i108], E[i108], F[i108], G[i108], H[i108], I[i108], J[i108], K[i108]);

        var i109 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i109], B[i109], C[i109], D[i109], E[i109], F[i109], G[i109], H[i109], I[i109], J[i109], K[i109]);

        var i110 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i110], B[i110], C[i110], D[i110], E[i110], F[i110], G[i110], H[i110], I[i110], J[i110], K[i110]);

        var i111 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i111], B[i111], C[i111], D[i111], E[i111], F[i111], G[i111], H[i111], I[i111], J[i111], K[i111]);

        var i112 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i112], B[i112], C[i112], D[i112], E[i112], F[i112], G[i112], H[i112], I[i112], J[i112], K[i112]);

        var i113 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i113], B[i113], C[i113], D[i113], E[i113], F[i113], G[i113], H[i113], I[i113], J[i113], K[i113]);

        var i114 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i114], B[i114], C[i114], D[i114], E[i114], F[i114], G[i114], H[i114], I[i114], J[i114], K[i114]);

        var i115 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i115], B[i115], C[i115], D[i115], E[i115], F[i115], G[i115], H[i115], I[i115], J[i115], K[i115]);

        var i116 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i116], B[i116], C[i116], D[i116], E[i116], F[i116], G[i116], H[i116], I[i116], J[i116], K[i116]);

        var i117 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i117], B[i117], C[i117], D[i117], E[i117], F[i117], G[i117], H[i117], I[i117], J[i117], K[i117]);

        var i118 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i118], B[i118], C[i118], D[i118], E[i118], F[i118], G[i118], H[i118], I[i118], J[i118], K[i118]);

        var i119 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i119], B[i119], C[i119], D[i119], E[i119], F[i119], G[i119], H[i119], I[i119], J[i119], K[i119]);

        var i120 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i120], B[i120], C[i120], D[i120], E[i120], F[i120], G[i120], H[i120], I[i120], J[i120], K[i120]);

        var i121 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i121], B[i121], C[i121], D[i121], E[i121], F[i121], G[i121], H[i121], I[i121], J[i121], K[i121]);

        var i122 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i122], B[i122], C[i122], D[i122], E[i122], F[i122], G[i122], H[i122], I[i122], J[i122], K[i122]);

        var i123 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i123], B[i123], C[i123], D[i123], E[i123], F[i123], G[i123], H[i123], I[i123], J[i123], K[i123]);

        var i124 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i124], B[i124], C[i124], D[i124], E[i124], F[i124], G[i124], H[i124], I[i124], J[i124], K[i124]);

        var i125 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i125], B[i125], C[i125], D[i125], E[i125], F[i125], G[i125], H[i125], I[i125], J[i125], K[i125]);

        var i126 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i126], B[i126], C[i126], D[i126], E[i126], F[i126], G[i126], H[i126], I[i126], J[i126], K[i126]);

        var i127 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i127], B[i127], C[i127], D[i127], E[i127], F[i127], G[i127], H[i127], I[i127], J[i127], K[i127]);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double DynamicExpresso_Delegate_Unrolled128()
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

        var i16 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i16], B[i16], C[i16], D[i16], E[i16], F[i16], G[i16], H[i16], I[i16], J[i16], K[i16]);

        var i17 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i17], B[i17], C[i17], D[i17], E[i17], F[i17], G[i17], H[i17], I[i17], J[i17], K[i17]);

        var i18 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i18], B[i18], C[i18], D[i18], E[i18], F[i18], G[i18], H[i18], I[i18], J[i18], K[i18]);

        var i19 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i19], B[i19], C[i19], D[i19], E[i19], F[i19], G[i19], H[i19], I[i19], J[i19], K[i19]);

        var i20 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i20], B[i20], C[i20], D[i20], E[i20], F[i20], G[i20], H[i20], I[i20], J[i20], K[i20]);

        var i21 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i21], B[i21], C[i21], D[i21], E[i21], F[i21], G[i21], H[i21], I[i21], J[i21], K[i21]);

        var i22 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i22], B[i22], C[i22], D[i22], E[i22], F[i22], G[i22], H[i22], I[i22], J[i22], K[i22]);

        var i23 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i23], B[i23], C[i23], D[i23], E[i23], F[i23], G[i23], H[i23], I[i23], J[i23], K[i23]);

        var i24 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i24], B[i24], C[i24], D[i24], E[i24], F[i24], G[i24], H[i24], I[i24], J[i24], K[i24]);

        var i25 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i25], B[i25], C[i25], D[i25], E[i25], F[i25], G[i25], H[i25], I[i25], J[i25], K[i25]);

        var i26 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i26], B[i26], C[i26], D[i26], E[i26], F[i26], G[i26], H[i26], I[i26], J[i26], K[i26]);

        var i27 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i27], B[i27], C[i27], D[i27], E[i27], F[i27], G[i27], H[i27], I[i27], J[i27], K[i27]);

        var i28 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i28], B[i28], C[i28], D[i28], E[i28], F[i28], G[i28], H[i28], I[i28], J[i28], K[i28]);

        var i29 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i29], B[i29], C[i29], D[i29], E[i29], F[i29], G[i29], H[i29], I[i29], J[i29], K[i29]);

        var i30 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i30], B[i30], C[i30], D[i30], E[i30], F[i30], G[i30], H[i30], I[i30], J[i30], K[i30]);

        var i31 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i31], B[i31], C[i31], D[i31], E[i31], F[i31], G[i31], H[i31], I[i31], J[i31], K[i31]);

        var i32 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i32], B[i32], C[i32], D[i32], E[i32], F[i32], G[i32], H[i32], I[i32], J[i32], K[i32]);

        var i33 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i33], B[i33], C[i33], D[i33], E[i33], F[i33], G[i33], H[i33], I[i33], J[i33], K[i33]);

        var i34 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i34], B[i34], C[i34], D[i34], E[i34], F[i34], G[i34], H[i34], I[i34], J[i34], K[i34]);

        var i35 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i35], B[i35], C[i35], D[i35], E[i35], F[i35], G[i35], H[i35], I[i35], J[i35], K[i35]);

        var i36 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i36], B[i36], C[i36], D[i36], E[i36], F[i36], G[i36], H[i36], I[i36], J[i36], K[i36]);

        var i37 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i37], B[i37], C[i37], D[i37], E[i37], F[i37], G[i37], H[i37], I[i37], J[i37], K[i37]);

        var i38 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i38], B[i38], C[i38], D[i38], E[i38], F[i38], G[i38], H[i38], I[i38], J[i38], K[i38]);

        var i39 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i39], B[i39], C[i39], D[i39], E[i39], F[i39], G[i39], H[i39], I[i39], J[i39], K[i39]);

        var i40 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i40], B[i40], C[i40], D[i40], E[i40], F[i40], G[i40], H[i40], I[i40], J[i40], K[i40]);

        var i41 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i41], B[i41], C[i41], D[i41], E[i41], F[i41], G[i41], H[i41], I[i41], J[i41], K[i41]);

        var i42 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i42], B[i42], C[i42], D[i42], E[i42], F[i42], G[i42], H[i42], I[i42], J[i42], K[i42]);

        var i43 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i43], B[i43], C[i43], D[i43], E[i43], F[i43], G[i43], H[i43], I[i43], J[i43], K[i43]);

        var i44 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i44], B[i44], C[i44], D[i44], E[i44], F[i44], G[i44], H[i44], I[i44], J[i44], K[i44]);

        var i45 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i45], B[i45], C[i45], D[i45], E[i45], F[i45], G[i45], H[i45], I[i45], J[i45], K[i45]);

        var i46 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i46], B[i46], C[i46], D[i46], E[i46], F[i46], G[i46], H[i46], I[i46], J[i46], K[i46]);

        var i47 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i47], B[i47], C[i47], D[i47], E[i47], F[i47], G[i47], H[i47], I[i47], J[i47], K[i47]);

        var i48 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i48], B[i48], C[i48], D[i48], E[i48], F[i48], G[i48], H[i48], I[i48], J[i48], K[i48]);

        var i49 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i49], B[i49], C[i49], D[i49], E[i49], F[i49], G[i49], H[i49], I[i49], J[i49], K[i49]);

        var i50 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i50], B[i50], C[i50], D[i50], E[i50], F[i50], G[i50], H[i50], I[i50], J[i50], K[i50]);

        var i51 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i51], B[i51], C[i51], D[i51], E[i51], F[i51], G[i51], H[i51], I[i51], J[i51], K[i51]);

        var i52 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i52], B[i52], C[i52], D[i52], E[i52], F[i52], G[i52], H[i52], I[i52], J[i52], K[i52]);

        var i53 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i53], B[i53], C[i53], D[i53], E[i53], F[i53], G[i53], H[i53], I[i53], J[i53], K[i53]);

        var i54 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i54], B[i54], C[i54], D[i54], E[i54], F[i54], G[i54], H[i54], I[i54], J[i54], K[i54]);

        var i55 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i55], B[i55], C[i55], D[i55], E[i55], F[i55], G[i55], H[i55], I[i55], J[i55], K[i55]);

        var i56 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i56], B[i56], C[i56], D[i56], E[i56], F[i56], G[i56], H[i56], I[i56], J[i56], K[i56]);

        var i57 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i57], B[i57], C[i57], D[i57], E[i57], F[i57], G[i57], H[i57], I[i57], J[i57], K[i57]);

        var i58 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i58], B[i58], C[i58], D[i58], E[i58], F[i58], G[i58], H[i58], I[i58], J[i58], K[i58]);

        var i59 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i59], B[i59], C[i59], D[i59], E[i59], F[i59], G[i59], H[i59], I[i59], J[i59], K[i59]);

        var i60 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i60], B[i60], C[i60], D[i60], E[i60], F[i60], G[i60], H[i60], I[i60], J[i60], K[i60]);

        var i61 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i61], B[i61], C[i61], D[i61], E[i61], F[i61], G[i61], H[i61], I[i61], J[i61], K[i61]);

        var i62 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i62], B[i62], C[i62], D[i62], E[i62], F[i62], G[i62], H[i62], I[i62], J[i62], K[i62]);

        var i63 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i63], B[i63], C[i63], D[i63], E[i63], F[i63], G[i63], H[i63], I[i63], J[i63], K[i63]);

        var i64 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i64], B[i64], C[i64], D[i64], E[i64], F[i64], G[i64], H[i64], I[i64], J[i64], K[i64]);

        var i65 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i65], B[i65], C[i65], D[i65], E[i65], F[i65], G[i65], H[i65], I[i65], J[i65], K[i65]);

        var i66 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i66], B[i66], C[i66], D[i66], E[i66], F[i66], G[i66], H[i66], I[i66], J[i66], K[i66]);

        var i67 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i67], B[i67], C[i67], D[i67], E[i67], F[i67], G[i67], H[i67], I[i67], J[i67], K[i67]);

        var i68 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i68], B[i68], C[i68], D[i68], E[i68], F[i68], G[i68], H[i68], I[i68], J[i68], K[i68]);

        var i69 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i69], B[i69], C[i69], D[i69], E[i69], F[i69], G[i69], H[i69], I[i69], J[i69], K[i69]);

        var i70 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i70], B[i70], C[i70], D[i70], E[i70], F[i70], G[i70], H[i70], I[i70], J[i70], K[i70]);

        var i71 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i71], B[i71], C[i71], D[i71], E[i71], F[i71], G[i71], H[i71], I[i71], J[i71], K[i71]);

        var i72 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i72], B[i72], C[i72], D[i72], E[i72], F[i72], G[i72], H[i72], I[i72], J[i72], K[i72]);

        var i73 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i73], B[i73], C[i73], D[i73], E[i73], F[i73], G[i73], H[i73], I[i73], J[i73], K[i73]);

        var i74 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i74], B[i74], C[i74], D[i74], E[i74], F[i74], G[i74], H[i74], I[i74], J[i74], K[i74]);

        var i75 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i75], B[i75], C[i75], D[i75], E[i75], F[i75], G[i75], H[i75], I[i75], J[i75], K[i75]);

        var i76 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i76], B[i76], C[i76], D[i76], E[i76], F[i76], G[i76], H[i76], I[i76], J[i76], K[i76]);

        var i77 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i77], B[i77], C[i77], D[i77], E[i77], F[i77], G[i77], H[i77], I[i77], J[i77], K[i77]);

        var i78 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i78], B[i78], C[i78], D[i78], E[i78], F[i78], G[i78], H[i78], I[i78], J[i78], K[i78]);

        var i79 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i79], B[i79], C[i79], D[i79], E[i79], F[i79], G[i79], H[i79], I[i79], J[i79], K[i79]);

        var i80 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i80], B[i80], C[i80], D[i80], E[i80], F[i80], G[i80], H[i80], I[i80], J[i80], K[i80]);

        var i81 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i81], B[i81], C[i81], D[i81], E[i81], F[i81], G[i81], H[i81], I[i81], J[i81], K[i81]);

        var i82 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i82], B[i82], C[i82], D[i82], E[i82], F[i82], G[i82], H[i82], I[i82], J[i82], K[i82]);

        var i83 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i83], B[i83], C[i83], D[i83], E[i83], F[i83], G[i83], H[i83], I[i83], J[i83], K[i83]);

        var i84 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i84], B[i84], C[i84], D[i84], E[i84], F[i84], G[i84], H[i84], I[i84], J[i84], K[i84]);

        var i85 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i85], B[i85], C[i85], D[i85], E[i85], F[i85], G[i85], H[i85], I[i85], J[i85], K[i85]);

        var i86 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i86], B[i86], C[i86], D[i86], E[i86], F[i86], G[i86], H[i86], I[i86], J[i86], K[i86]);

        var i87 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i87], B[i87], C[i87], D[i87], E[i87], F[i87], G[i87], H[i87], I[i87], J[i87], K[i87]);

        var i88 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i88], B[i88], C[i88], D[i88], E[i88], F[i88], G[i88], H[i88], I[i88], J[i88], K[i88]);

        var i89 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i89], B[i89], C[i89], D[i89], E[i89], F[i89], G[i89], H[i89], I[i89], J[i89], K[i89]);

        var i90 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i90], B[i90], C[i90], D[i90], E[i90], F[i90], G[i90], H[i90], I[i90], J[i90], K[i90]);

        var i91 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i91], B[i91], C[i91], D[i91], E[i91], F[i91], G[i91], H[i91], I[i91], J[i91], K[i91]);

        var i92 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i92], B[i92], C[i92], D[i92], E[i92], F[i92], G[i92], H[i92], I[i92], J[i92], K[i92]);

        var i93 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i93], B[i93], C[i93], D[i93], E[i93], F[i93], G[i93], H[i93], I[i93], J[i93], K[i93]);

        var i94 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i94], B[i94], C[i94], D[i94], E[i94], F[i94], G[i94], H[i94], I[i94], J[i94], K[i94]);

        var i95 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i95], B[i95], C[i95], D[i95], E[i95], F[i95], G[i95], H[i95], I[i95], J[i95], K[i95]);

        var i96 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i96], B[i96], C[i96], D[i96], E[i96], F[i96], G[i96], H[i96], I[i96], J[i96], K[i96]);

        var i97 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i97], B[i97], C[i97], D[i97], E[i97], F[i97], G[i97], H[i97], I[i97], J[i97], K[i97]);

        var i98 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i98], B[i98], C[i98], D[i98], E[i98], F[i98], G[i98], H[i98], I[i98], J[i98], K[i98]);

        var i99 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i99], B[i99], C[i99], D[i99], E[i99], F[i99], G[i99], H[i99], I[i99], J[i99], K[i99]);

        var i100 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i100], B[i100], C[i100], D[i100], E[i100], F[i100], G[i100], H[i100], I[i100], J[i100], K[i100]);

        var i101 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i101], B[i101], C[i101], D[i101], E[i101], F[i101], G[i101], H[i101], I[i101], J[i101], K[i101]);

        var i102 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i102], B[i102], C[i102], D[i102], E[i102], F[i102], G[i102], H[i102], I[i102], J[i102], K[i102]);

        var i103 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i103], B[i103], C[i103], D[i103], E[i103], F[i103], G[i103], H[i103], I[i103], J[i103], K[i103]);

        var i104 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i104], B[i104], C[i104], D[i104], E[i104], F[i104], G[i104], H[i104], I[i104], J[i104], K[i104]);

        var i105 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i105], B[i105], C[i105], D[i105], E[i105], F[i105], G[i105], H[i105], I[i105], J[i105], K[i105]);

        var i106 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i106], B[i106], C[i106], D[i106], E[i106], F[i106], G[i106], H[i106], I[i106], J[i106], K[i106]);

        var i107 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i107], B[i107], C[i107], D[i107], E[i107], F[i107], G[i107], H[i107], I[i107], J[i107], K[i107]);

        var i108 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i108], B[i108], C[i108], D[i108], E[i108], F[i108], G[i108], H[i108], I[i108], J[i108], K[i108]);

        var i109 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i109], B[i109], C[i109], D[i109], E[i109], F[i109], G[i109], H[i109], I[i109], J[i109], K[i109]);

        var i110 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i110], B[i110], C[i110], D[i110], E[i110], F[i110], G[i110], H[i110], I[i110], J[i110], K[i110]);

        var i111 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i111], B[i111], C[i111], D[i111], E[i111], F[i111], G[i111], H[i111], I[i111], J[i111], K[i111]);

        var i112 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i112], B[i112], C[i112], D[i112], E[i112], F[i112], G[i112], H[i112], I[i112], J[i112], K[i112]);

        var i113 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i113], B[i113], C[i113], D[i113], E[i113], F[i113], G[i113], H[i113], I[i113], J[i113], K[i113]);

        var i114 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i114], B[i114], C[i114], D[i114], E[i114], F[i114], G[i114], H[i114], I[i114], J[i114], K[i114]);

        var i115 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i115], B[i115], C[i115], D[i115], E[i115], F[i115], G[i115], H[i115], I[i115], J[i115], K[i115]);

        var i116 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i116], B[i116], C[i116], D[i116], E[i116], F[i116], G[i116], H[i116], I[i116], J[i116], K[i116]);

        var i117 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i117], B[i117], C[i117], D[i117], E[i117], F[i117], G[i117], H[i117], I[i117], J[i117], K[i117]);

        var i118 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i118], B[i118], C[i118], D[i118], E[i118], F[i118], G[i118], H[i118], I[i118], J[i118], K[i118]);

        var i119 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i119], B[i119], C[i119], D[i119], E[i119], F[i119], G[i119], H[i119], I[i119], J[i119], K[i119]);

        var i120 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i120], B[i120], C[i120], D[i120], E[i120], F[i120], G[i120], H[i120], I[i120], J[i120], K[i120]);

        var i121 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i121], B[i121], C[i121], D[i121], E[i121], F[i121], G[i121], H[i121], I[i121], J[i121], K[i121]);

        var i122 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i122], B[i122], C[i122], D[i122], E[i122], F[i122], G[i122], H[i122], I[i122], J[i122], K[i122]);

        var i123 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i123], B[i123], C[i123], D[i123], E[i123], F[i123], G[i123], H[i123], I[i123], J[i123], K[i123]);

        var i124 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i124], B[i124], C[i124], D[i124], E[i124], F[i124], G[i124], H[i124], I[i124], J[i124], K[i124]);

        var i125 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i125], B[i125], C[i125], D[i125], E[i125], F[i125], G[i125], H[i125], I[i125], J[i125], K[i125]);

        var i126 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i126], B[i126], C[i126], D[i126], E[i126], F[i126], G[i126], H[i126], I[i126], J[i126], K[i126]);

        var i127 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i127], B[i127], C[i127], D[i127], E[i127], F[i127], G[i127], H[i127], I[i127], J[i127], K[i127]);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double NCalc_Lambda_Unrolled128()
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

        var i16 = NextIndex();
        _nCalcContext.A = A[i16];
        _nCalcContext.B = B[i16];
        _nCalcContext.C = C[i16];
        _nCalcContext.D = D[i16];
        _nCalcContext.E = E[i16];
        _nCalcContext.F = F[i16];
        _nCalcContext.G = G[i16];
        _nCalcContext.H = H[i16];
        _nCalcContext.I = I[i16];
        _nCalcContext.J = J[i16];
        _nCalcContext.K = K[i16];
        sum += _nCalcLambda(_nCalcContext);

        var i17 = NextIndex();
        _nCalcContext.A = A[i17];
        _nCalcContext.B = B[i17];
        _nCalcContext.C = C[i17];
        _nCalcContext.D = D[i17];
        _nCalcContext.E = E[i17];
        _nCalcContext.F = F[i17];
        _nCalcContext.G = G[i17];
        _nCalcContext.H = H[i17];
        _nCalcContext.I = I[i17];
        _nCalcContext.J = J[i17];
        _nCalcContext.K = K[i17];
        sum += _nCalcLambda(_nCalcContext);

        var i18 = NextIndex();
        _nCalcContext.A = A[i18];
        _nCalcContext.B = B[i18];
        _nCalcContext.C = C[i18];
        _nCalcContext.D = D[i18];
        _nCalcContext.E = E[i18];
        _nCalcContext.F = F[i18];
        _nCalcContext.G = G[i18];
        _nCalcContext.H = H[i18];
        _nCalcContext.I = I[i18];
        _nCalcContext.J = J[i18];
        _nCalcContext.K = K[i18];
        sum += _nCalcLambda(_nCalcContext);

        var i19 = NextIndex();
        _nCalcContext.A = A[i19];
        _nCalcContext.B = B[i19];
        _nCalcContext.C = C[i19];
        _nCalcContext.D = D[i19];
        _nCalcContext.E = E[i19];
        _nCalcContext.F = F[i19];
        _nCalcContext.G = G[i19];
        _nCalcContext.H = H[i19];
        _nCalcContext.I = I[i19];
        _nCalcContext.J = J[i19];
        _nCalcContext.K = K[i19];
        sum += _nCalcLambda(_nCalcContext);

        var i20 = NextIndex();
        _nCalcContext.A = A[i20];
        _nCalcContext.B = B[i20];
        _nCalcContext.C = C[i20];
        _nCalcContext.D = D[i20];
        _nCalcContext.E = E[i20];
        _nCalcContext.F = F[i20];
        _nCalcContext.G = G[i20];
        _nCalcContext.H = H[i20];
        _nCalcContext.I = I[i20];
        _nCalcContext.J = J[i20];
        _nCalcContext.K = K[i20];
        sum += _nCalcLambda(_nCalcContext);

        var i21 = NextIndex();
        _nCalcContext.A = A[i21];
        _nCalcContext.B = B[i21];
        _nCalcContext.C = C[i21];
        _nCalcContext.D = D[i21];
        _nCalcContext.E = E[i21];
        _nCalcContext.F = F[i21];
        _nCalcContext.G = G[i21];
        _nCalcContext.H = H[i21];
        _nCalcContext.I = I[i21];
        _nCalcContext.J = J[i21];
        _nCalcContext.K = K[i21];
        sum += _nCalcLambda(_nCalcContext);

        var i22 = NextIndex();
        _nCalcContext.A = A[i22];
        _nCalcContext.B = B[i22];
        _nCalcContext.C = C[i22];
        _nCalcContext.D = D[i22];
        _nCalcContext.E = E[i22];
        _nCalcContext.F = F[i22];
        _nCalcContext.G = G[i22];
        _nCalcContext.H = H[i22];
        _nCalcContext.I = I[i22];
        _nCalcContext.J = J[i22];
        _nCalcContext.K = K[i22];
        sum += _nCalcLambda(_nCalcContext);

        var i23 = NextIndex();
        _nCalcContext.A = A[i23];
        _nCalcContext.B = B[i23];
        _nCalcContext.C = C[i23];
        _nCalcContext.D = D[i23];
        _nCalcContext.E = E[i23];
        _nCalcContext.F = F[i23];
        _nCalcContext.G = G[i23];
        _nCalcContext.H = H[i23];
        _nCalcContext.I = I[i23];
        _nCalcContext.J = J[i23];
        _nCalcContext.K = K[i23];
        sum += _nCalcLambda(_nCalcContext);

        var i24 = NextIndex();
        _nCalcContext.A = A[i24];
        _nCalcContext.B = B[i24];
        _nCalcContext.C = C[i24];
        _nCalcContext.D = D[i24];
        _nCalcContext.E = E[i24];
        _nCalcContext.F = F[i24];
        _nCalcContext.G = G[i24];
        _nCalcContext.H = H[i24];
        _nCalcContext.I = I[i24];
        _nCalcContext.J = J[i24];
        _nCalcContext.K = K[i24];
        sum += _nCalcLambda(_nCalcContext);

        var i25 = NextIndex();
        _nCalcContext.A = A[i25];
        _nCalcContext.B = B[i25];
        _nCalcContext.C = C[i25];
        _nCalcContext.D = D[i25];
        _nCalcContext.E = E[i25];
        _nCalcContext.F = F[i25];
        _nCalcContext.G = G[i25];
        _nCalcContext.H = H[i25];
        _nCalcContext.I = I[i25];
        _nCalcContext.J = J[i25];
        _nCalcContext.K = K[i25];
        sum += _nCalcLambda(_nCalcContext);

        var i26 = NextIndex();
        _nCalcContext.A = A[i26];
        _nCalcContext.B = B[i26];
        _nCalcContext.C = C[i26];
        _nCalcContext.D = D[i26];
        _nCalcContext.E = E[i26];
        _nCalcContext.F = F[i26];
        _nCalcContext.G = G[i26];
        _nCalcContext.H = H[i26];
        _nCalcContext.I = I[i26];
        _nCalcContext.J = J[i26];
        _nCalcContext.K = K[i26];
        sum += _nCalcLambda(_nCalcContext);

        var i27 = NextIndex();
        _nCalcContext.A = A[i27];
        _nCalcContext.B = B[i27];
        _nCalcContext.C = C[i27];
        _nCalcContext.D = D[i27];
        _nCalcContext.E = E[i27];
        _nCalcContext.F = F[i27];
        _nCalcContext.G = G[i27];
        _nCalcContext.H = H[i27];
        _nCalcContext.I = I[i27];
        _nCalcContext.J = J[i27];
        _nCalcContext.K = K[i27];
        sum += _nCalcLambda(_nCalcContext);

        var i28 = NextIndex();
        _nCalcContext.A = A[i28];
        _nCalcContext.B = B[i28];
        _nCalcContext.C = C[i28];
        _nCalcContext.D = D[i28];
        _nCalcContext.E = E[i28];
        _nCalcContext.F = F[i28];
        _nCalcContext.G = G[i28];
        _nCalcContext.H = H[i28];
        _nCalcContext.I = I[i28];
        _nCalcContext.J = J[i28];
        _nCalcContext.K = K[i28];
        sum += _nCalcLambda(_nCalcContext);

        var i29 = NextIndex();
        _nCalcContext.A = A[i29];
        _nCalcContext.B = B[i29];
        _nCalcContext.C = C[i29];
        _nCalcContext.D = D[i29];
        _nCalcContext.E = E[i29];
        _nCalcContext.F = F[i29];
        _nCalcContext.G = G[i29];
        _nCalcContext.H = H[i29];
        _nCalcContext.I = I[i29];
        _nCalcContext.J = J[i29];
        _nCalcContext.K = K[i29];
        sum += _nCalcLambda(_nCalcContext);

        var i30 = NextIndex();
        _nCalcContext.A = A[i30];
        _nCalcContext.B = B[i30];
        _nCalcContext.C = C[i30];
        _nCalcContext.D = D[i30];
        _nCalcContext.E = E[i30];
        _nCalcContext.F = F[i30];
        _nCalcContext.G = G[i30];
        _nCalcContext.H = H[i30];
        _nCalcContext.I = I[i30];
        _nCalcContext.J = J[i30];
        _nCalcContext.K = K[i30];
        sum += _nCalcLambda(_nCalcContext);

        var i31 = NextIndex();
        _nCalcContext.A = A[i31];
        _nCalcContext.B = B[i31];
        _nCalcContext.C = C[i31];
        _nCalcContext.D = D[i31];
        _nCalcContext.E = E[i31];
        _nCalcContext.F = F[i31];
        _nCalcContext.G = G[i31];
        _nCalcContext.H = H[i31];
        _nCalcContext.I = I[i31];
        _nCalcContext.J = J[i31];
        _nCalcContext.K = K[i31];
        sum += _nCalcLambda(_nCalcContext);

        var i32 = NextIndex();
        _nCalcContext.A = A[i32];
        _nCalcContext.B = B[i32];
        _nCalcContext.C = C[i32];
        _nCalcContext.D = D[i32];
        _nCalcContext.E = E[i32];
        _nCalcContext.F = F[i32];
        _nCalcContext.G = G[i32];
        _nCalcContext.H = H[i32];
        _nCalcContext.I = I[i32];
        _nCalcContext.J = J[i32];
        _nCalcContext.K = K[i32];
        sum += _nCalcLambda(_nCalcContext);

        var i33 = NextIndex();
        _nCalcContext.A = A[i33];
        _nCalcContext.B = B[i33];
        _nCalcContext.C = C[i33];
        _nCalcContext.D = D[i33];
        _nCalcContext.E = E[i33];
        _nCalcContext.F = F[i33];
        _nCalcContext.G = G[i33];
        _nCalcContext.H = H[i33];
        _nCalcContext.I = I[i33];
        _nCalcContext.J = J[i33];
        _nCalcContext.K = K[i33];
        sum += _nCalcLambda(_nCalcContext);

        var i34 = NextIndex();
        _nCalcContext.A = A[i34];
        _nCalcContext.B = B[i34];
        _nCalcContext.C = C[i34];
        _nCalcContext.D = D[i34];
        _nCalcContext.E = E[i34];
        _nCalcContext.F = F[i34];
        _nCalcContext.G = G[i34];
        _nCalcContext.H = H[i34];
        _nCalcContext.I = I[i34];
        _nCalcContext.J = J[i34];
        _nCalcContext.K = K[i34];
        sum += _nCalcLambda(_nCalcContext);

        var i35 = NextIndex();
        _nCalcContext.A = A[i35];
        _nCalcContext.B = B[i35];
        _nCalcContext.C = C[i35];
        _nCalcContext.D = D[i35];
        _nCalcContext.E = E[i35];
        _nCalcContext.F = F[i35];
        _nCalcContext.G = G[i35];
        _nCalcContext.H = H[i35];
        _nCalcContext.I = I[i35];
        _nCalcContext.J = J[i35];
        _nCalcContext.K = K[i35];
        sum += _nCalcLambda(_nCalcContext);

        var i36 = NextIndex();
        _nCalcContext.A = A[i36];
        _nCalcContext.B = B[i36];
        _nCalcContext.C = C[i36];
        _nCalcContext.D = D[i36];
        _nCalcContext.E = E[i36];
        _nCalcContext.F = F[i36];
        _nCalcContext.G = G[i36];
        _nCalcContext.H = H[i36];
        _nCalcContext.I = I[i36];
        _nCalcContext.J = J[i36];
        _nCalcContext.K = K[i36];
        sum += _nCalcLambda(_nCalcContext);

        var i37 = NextIndex();
        _nCalcContext.A = A[i37];
        _nCalcContext.B = B[i37];
        _nCalcContext.C = C[i37];
        _nCalcContext.D = D[i37];
        _nCalcContext.E = E[i37];
        _nCalcContext.F = F[i37];
        _nCalcContext.G = G[i37];
        _nCalcContext.H = H[i37];
        _nCalcContext.I = I[i37];
        _nCalcContext.J = J[i37];
        _nCalcContext.K = K[i37];
        sum += _nCalcLambda(_nCalcContext);

        var i38 = NextIndex();
        _nCalcContext.A = A[i38];
        _nCalcContext.B = B[i38];
        _nCalcContext.C = C[i38];
        _nCalcContext.D = D[i38];
        _nCalcContext.E = E[i38];
        _nCalcContext.F = F[i38];
        _nCalcContext.G = G[i38];
        _nCalcContext.H = H[i38];
        _nCalcContext.I = I[i38];
        _nCalcContext.J = J[i38];
        _nCalcContext.K = K[i38];
        sum += _nCalcLambda(_nCalcContext);

        var i39 = NextIndex();
        _nCalcContext.A = A[i39];
        _nCalcContext.B = B[i39];
        _nCalcContext.C = C[i39];
        _nCalcContext.D = D[i39];
        _nCalcContext.E = E[i39];
        _nCalcContext.F = F[i39];
        _nCalcContext.G = G[i39];
        _nCalcContext.H = H[i39];
        _nCalcContext.I = I[i39];
        _nCalcContext.J = J[i39];
        _nCalcContext.K = K[i39];
        sum += _nCalcLambda(_nCalcContext);

        var i40 = NextIndex();
        _nCalcContext.A = A[i40];
        _nCalcContext.B = B[i40];
        _nCalcContext.C = C[i40];
        _nCalcContext.D = D[i40];
        _nCalcContext.E = E[i40];
        _nCalcContext.F = F[i40];
        _nCalcContext.G = G[i40];
        _nCalcContext.H = H[i40];
        _nCalcContext.I = I[i40];
        _nCalcContext.J = J[i40];
        _nCalcContext.K = K[i40];
        sum += _nCalcLambda(_nCalcContext);

        var i41 = NextIndex();
        _nCalcContext.A = A[i41];
        _nCalcContext.B = B[i41];
        _nCalcContext.C = C[i41];
        _nCalcContext.D = D[i41];
        _nCalcContext.E = E[i41];
        _nCalcContext.F = F[i41];
        _nCalcContext.G = G[i41];
        _nCalcContext.H = H[i41];
        _nCalcContext.I = I[i41];
        _nCalcContext.J = J[i41];
        _nCalcContext.K = K[i41];
        sum += _nCalcLambda(_nCalcContext);

        var i42 = NextIndex();
        _nCalcContext.A = A[i42];
        _nCalcContext.B = B[i42];
        _nCalcContext.C = C[i42];
        _nCalcContext.D = D[i42];
        _nCalcContext.E = E[i42];
        _nCalcContext.F = F[i42];
        _nCalcContext.G = G[i42];
        _nCalcContext.H = H[i42];
        _nCalcContext.I = I[i42];
        _nCalcContext.J = J[i42];
        _nCalcContext.K = K[i42];
        sum += _nCalcLambda(_nCalcContext);

        var i43 = NextIndex();
        _nCalcContext.A = A[i43];
        _nCalcContext.B = B[i43];
        _nCalcContext.C = C[i43];
        _nCalcContext.D = D[i43];
        _nCalcContext.E = E[i43];
        _nCalcContext.F = F[i43];
        _nCalcContext.G = G[i43];
        _nCalcContext.H = H[i43];
        _nCalcContext.I = I[i43];
        _nCalcContext.J = J[i43];
        _nCalcContext.K = K[i43];
        sum += _nCalcLambda(_nCalcContext);

        var i44 = NextIndex();
        _nCalcContext.A = A[i44];
        _nCalcContext.B = B[i44];
        _nCalcContext.C = C[i44];
        _nCalcContext.D = D[i44];
        _nCalcContext.E = E[i44];
        _nCalcContext.F = F[i44];
        _nCalcContext.G = G[i44];
        _nCalcContext.H = H[i44];
        _nCalcContext.I = I[i44];
        _nCalcContext.J = J[i44];
        _nCalcContext.K = K[i44];
        sum += _nCalcLambda(_nCalcContext);

        var i45 = NextIndex();
        _nCalcContext.A = A[i45];
        _nCalcContext.B = B[i45];
        _nCalcContext.C = C[i45];
        _nCalcContext.D = D[i45];
        _nCalcContext.E = E[i45];
        _nCalcContext.F = F[i45];
        _nCalcContext.G = G[i45];
        _nCalcContext.H = H[i45];
        _nCalcContext.I = I[i45];
        _nCalcContext.J = J[i45];
        _nCalcContext.K = K[i45];
        sum += _nCalcLambda(_nCalcContext);

        var i46 = NextIndex();
        _nCalcContext.A = A[i46];
        _nCalcContext.B = B[i46];
        _nCalcContext.C = C[i46];
        _nCalcContext.D = D[i46];
        _nCalcContext.E = E[i46];
        _nCalcContext.F = F[i46];
        _nCalcContext.G = G[i46];
        _nCalcContext.H = H[i46];
        _nCalcContext.I = I[i46];
        _nCalcContext.J = J[i46];
        _nCalcContext.K = K[i46];
        sum += _nCalcLambda(_nCalcContext);

        var i47 = NextIndex();
        _nCalcContext.A = A[i47];
        _nCalcContext.B = B[i47];
        _nCalcContext.C = C[i47];
        _nCalcContext.D = D[i47];
        _nCalcContext.E = E[i47];
        _nCalcContext.F = F[i47];
        _nCalcContext.G = G[i47];
        _nCalcContext.H = H[i47];
        _nCalcContext.I = I[i47];
        _nCalcContext.J = J[i47];
        _nCalcContext.K = K[i47];
        sum += _nCalcLambda(_nCalcContext);

        var i48 = NextIndex();
        _nCalcContext.A = A[i48];
        _nCalcContext.B = B[i48];
        _nCalcContext.C = C[i48];
        _nCalcContext.D = D[i48];
        _nCalcContext.E = E[i48];
        _nCalcContext.F = F[i48];
        _nCalcContext.G = G[i48];
        _nCalcContext.H = H[i48];
        _nCalcContext.I = I[i48];
        _nCalcContext.J = J[i48];
        _nCalcContext.K = K[i48];
        sum += _nCalcLambda(_nCalcContext);

        var i49 = NextIndex();
        _nCalcContext.A = A[i49];
        _nCalcContext.B = B[i49];
        _nCalcContext.C = C[i49];
        _nCalcContext.D = D[i49];
        _nCalcContext.E = E[i49];
        _nCalcContext.F = F[i49];
        _nCalcContext.G = G[i49];
        _nCalcContext.H = H[i49];
        _nCalcContext.I = I[i49];
        _nCalcContext.J = J[i49];
        _nCalcContext.K = K[i49];
        sum += _nCalcLambda(_nCalcContext);

        var i50 = NextIndex();
        _nCalcContext.A = A[i50];
        _nCalcContext.B = B[i50];
        _nCalcContext.C = C[i50];
        _nCalcContext.D = D[i50];
        _nCalcContext.E = E[i50];
        _nCalcContext.F = F[i50];
        _nCalcContext.G = G[i50];
        _nCalcContext.H = H[i50];
        _nCalcContext.I = I[i50];
        _nCalcContext.J = J[i50];
        _nCalcContext.K = K[i50];
        sum += _nCalcLambda(_nCalcContext);

        var i51 = NextIndex();
        _nCalcContext.A = A[i51];
        _nCalcContext.B = B[i51];
        _nCalcContext.C = C[i51];
        _nCalcContext.D = D[i51];
        _nCalcContext.E = E[i51];
        _nCalcContext.F = F[i51];
        _nCalcContext.G = G[i51];
        _nCalcContext.H = H[i51];
        _nCalcContext.I = I[i51];
        _nCalcContext.J = J[i51];
        _nCalcContext.K = K[i51];
        sum += _nCalcLambda(_nCalcContext);

        var i52 = NextIndex();
        _nCalcContext.A = A[i52];
        _nCalcContext.B = B[i52];
        _nCalcContext.C = C[i52];
        _nCalcContext.D = D[i52];
        _nCalcContext.E = E[i52];
        _nCalcContext.F = F[i52];
        _nCalcContext.G = G[i52];
        _nCalcContext.H = H[i52];
        _nCalcContext.I = I[i52];
        _nCalcContext.J = J[i52];
        _nCalcContext.K = K[i52];
        sum += _nCalcLambda(_nCalcContext);

        var i53 = NextIndex();
        _nCalcContext.A = A[i53];
        _nCalcContext.B = B[i53];
        _nCalcContext.C = C[i53];
        _nCalcContext.D = D[i53];
        _nCalcContext.E = E[i53];
        _nCalcContext.F = F[i53];
        _nCalcContext.G = G[i53];
        _nCalcContext.H = H[i53];
        _nCalcContext.I = I[i53];
        _nCalcContext.J = J[i53];
        _nCalcContext.K = K[i53];
        sum += _nCalcLambda(_nCalcContext);

        var i54 = NextIndex();
        _nCalcContext.A = A[i54];
        _nCalcContext.B = B[i54];
        _nCalcContext.C = C[i54];
        _nCalcContext.D = D[i54];
        _nCalcContext.E = E[i54];
        _nCalcContext.F = F[i54];
        _nCalcContext.G = G[i54];
        _nCalcContext.H = H[i54];
        _nCalcContext.I = I[i54];
        _nCalcContext.J = J[i54];
        _nCalcContext.K = K[i54];
        sum += _nCalcLambda(_nCalcContext);

        var i55 = NextIndex();
        _nCalcContext.A = A[i55];
        _nCalcContext.B = B[i55];
        _nCalcContext.C = C[i55];
        _nCalcContext.D = D[i55];
        _nCalcContext.E = E[i55];
        _nCalcContext.F = F[i55];
        _nCalcContext.G = G[i55];
        _nCalcContext.H = H[i55];
        _nCalcContext.I = I[i55];
        _nCalcContext.J = J[i55];
        _nCalcContext.K = K[i55];
        sum += _nCalcLambda(_nCalcContext);

        var i56 = NextIndex();
        _nCalcContext.A = A[i56];
        _nCalcContext.B = B[i56];
        _nCalcContext.C = C[i56];
        _nCalcContext.D = D[i56];
        _nCalcContext.E = E[i56];
        _nCalcContext.F = F[i56];
        _nCalcContext.G = G[i56];
        _nCalcContext.H = H[i56];
        _nCalcContext.I = I[i56];
        _nCalcContext.J = J[i56];
        _nCalcContext.K = K[i56];
        sum += _nCalcLambda(_nCalcContext);

        var i57 = NextIndex();
        _nCalcContext.A = A[i57];
        _nCalcContext.B = B[i57];
        _nCalcContext.C = C[i57];
        _nCalcContext.D = D[i57];
        _nCalcContext.E = E[i57];
        _nCalcContext.F = F[i57];
        _nCalcContext.G = G[i57];
        _nCalcContext.H = H[i57];
        _nCalcContext.I = I[i57];
        _nCalcContext.J = J[i57];
        _nCalcContext.K = K[i57];
        sum += _nCalcLambda(_nCalcContext);

        var i58 = NextIndex();
        _nCalcContext.A = A[i58];
        _nCalcContext.B = B[i58];
        _nCalcContext.C = C[i58];
        _nCalcContext.D = D[i58];
        _nCalcContext.E = E[i58];
        _nCalcContext.F = F[i58];
        _nCalcContext.G = G[i58];
        _nCalcContext.H = H[i58];
        _nCalcContext.I = I[i58];
        _nCalcContext.J = J[i58];
        _nCalcContext.K = K[i58];
        sum += _nCalcLambda(_nCalcContext);

        var i59 = NextIndex();
        _nCalcContext.A = A[i59];
        _nCalcContext.B = B[i59];
        _nCalcContext.C = C[i59];
        _nCalcContext.D = D[i59];
        _nCalcContext.E = E[i59];
        _nCalcContext.F = F[i59];
        _nCalcContext.G = G[i59];
        _nCalcContext.H = H[i59];
        _nCalcContext.I = I[i59];
        _nCalcContext.J = J[i59];
        _nCalcContext.K = K[i59];
        sum += _nCalcLambda(_nCalcContext);

        var i60 = NextIndex();
        _nCalcContext.A = A[i60];
        _nCalcContext.B = B[i60];
        _nCalcContext.C = C[i60];
        _nCalcContext.D = D[i60];
        _nCalcContext.E = E[i60];
        _nCalcContext.F = F[i60];
        _nCalcContext.G = G[i60];
        _nCalcContext.H = H[i60];
        _nCalcContext.I = I[i60];
        _nCalcContext.J = J[i60];
        _nCalcContext.K = K[i60];
        sum += _nCalcLambda(_nCalcContext);

        var i61 = NextIndex();
        _nCalcContext.A = A[i61];
        _nCalcContext.B = B[i61];
        _nCalcContext.C = C[i61];
        _nCalcContext.D = D[i61];
        _nCalcContext.E = E[i61];
        _nCalcContext.F = F[i61];
        _nCalcContext.G = G[i61];
        _nCalcContext.H = H[i61];
        _nCalcContext.I = I[i61];
        _nCalcContext.J = J[i61];
        _nCalcContext.K = K[i61];
        sum += _nCalcLambda(_nCalcContext);

        var i62 = NextIndex();
        _nCalcContext.A = A[i62];
        _nCalcContext.B = B[i62];
        _nCalcContext.C = C[i62];
        _nCalcContext.D = D[i62];
        _nCalcContext.E = E[i62];
        _nCalcContext.F = F[i62];
        _nCalcContext.G = G[i62];
        _nCalcContext.H = H[i62];
        _nCalcContext.I = I[i62];
        _nCalcContext.J = J[i62];
        _nCalcContext.K = K[i62];
        sum += _nCalcLambda(_nCalcContext);

        var i63 = NextIndex();
        _nCalcContext.A = A[i63];
        _nCalcContext.B = B[i63];
        _nCalcContext.C = C[i63];
        _nCalcContext.D = D[i63];
        _nCalcContext.E = E[i63];
        _nCalcContext.F = F[i63];
        _nCalcContext.G = G[i63];
        _nCalcContext.H = H[i63];
        _nCalcContext.I = I[i63];
        _nCalcContext.J = J[i63];
        _nCalcContext.K = K[i63];
        sum += _nCalcLambda(_nCalcContext);

        var i64 = NextIndex();
        _nCalcContext.A = A[i64];
        _nCalcContext.B = B[i64];
        _nCalcContext.C = C[i64];
        _nCalcContext.D = D[i64];
        _nCalcContext.E = E[i64];
        _nCalcContext.F = F[i64];
        _nCalcContext.G = G[i64];
        _nCalcContext.H = H[i64];
        _nCalcContext.I = I[i64];
        _nCalcContext.J = J[i64];
        _nCalcContext.K = K[i64];
        sum += _nCalcLambda(_nCalcContext);

        var i65 = NextIndex();
        _nCalcContext.A = A[i65];
        _nCalcContext.B = B[i65];
        _nCalcContext.C = C[i65];
        _nCalcContext.D = D[i65];
        _nCalcContext.E = E[i65];
        _nCalcContext.F = F[i65];
        _nCalcContext.G = G[i65];
        _nCalcContext.H = H[i65];
        _nCalcContext.I = I[i65];
        _nCalcContext.J = J[i65];
        _nCalcContext.K = K[i65];
        sum += _nCalcLambda(_nCalcContext);

        var i66 = NextIndex();
        _nCalcContext.A = A[i66];
        _nCalcContext.B = B[i66];
        _nCalcContext.C = C[i66];
        _nCalcContext.D = D[i66];
        _nCalcContext.E = E[i66];
        _nCalcContext.F = F[i66];
        _nCalcContext.G = G[i66];
        _nCalcContext.H = H[i66];
        _nCalcContext.I = I[i66];
        _nCalcContext.J = J[i66];
        _nCalcContext.K = K[i66];
        sum += _nCalcLambda(_nCalcContext);

        var i67 = NextIndex();
        _nCalcContext.A = A[i67];
        _nCalcContext.B = B[i67];
        _nCalcContext.C = C[i67];
        _nCalcContext.D = D[i67];
        _nCalcContext.E = E[i67];
        _nCalcContext.F = F[i67];
        _nCalcContext.G = G[i67];
        _nCalcContext.H = H[i67];
        _nCalcContext.I = I[i67];
        _nCalcContext.J = J[i67];
        _nCalcContext.K = K[i67];
        sum += _nCalcLambda(_nCalcContext);

        var i68 = NextIndex();
        _nCalcContext.A = A[i68];
        _nCalcContext.B = B[i68];
        _nCalcContext.C = C[i68];
        _nCalcContext.D = D[i68];
        _nCalcContext.E = E[i68];
        _nCalcContext.F = F[i68];
        _nCalcContext.G = G[i68];
        _nCalcContext.H = H[i68];
        _nCalcContext.I = I[i68];
        _nCalcContext.J = J[i68];
        _nCalcContext.K = K[i68];
        sum += _nCalcLambda(_nCalcContext);

        var i69 = NextIndex();
        _nCalcContext.A = A[i69];
        _nCalcContext.B = B[i69];
        _nCalcContext.C = C[i69];
        _nCalcContext.D = D[i69];
        _nCalcContext.E = E[i69];
        _nCalcContext.F = F[i69];
        _nCalcContext.G = G[i69];
        _nCalcContext.H = H[i69];
        _nCalcContext.I = I[i69];
        _nCalcContext.J = J[i69];
        _nCalcContext.K = K[i69];
        sum += _nCalcLambda(_nCalcContext);

        var i70 = NextIndex();
        _nCalcContext.A = A[i70];
        _nCalcContext.B = B[i70];
        _nCalcContext.C = C[i70];
        _nCalcContext.D = D[i70];
        _nCalcContext.E = E[i70];
        _nCalcContext.F = F[i70];
        _nCalcContext.G = G[i70];
        _nCalcContext.H = H[i70];
        _nCalcContext.I = I[i70];
        _nCalcContext.J = J[i70];
        _nCalcContext.K = K[i70];
        sum += _nCalcLambda(_nCalcContext);

        var i71 = NextIndex();
        _nCalcContext.A = A[i71];
        _nCalcContext.B = B[i71];
        _nCalcContext.C = C[i71];
        _nCalcContext.D = D[i71];
        _nCalcContext.E = E[i71];
        _nCalcContext.F = F[i71];
        _nCalcContext.G = G[i71];
        _nCalcContext.H = H[i71];
        _nCalcContext.I = I[i71];
        _nCalcContext.J = J[i71];
        _nCalcContext.K = K[i71];
        sum += _nCalcLambda(_nCalcContext);

        var i72 = NextIndex();
        _nCalcContext.A = A[i72];
        _nCalcContext.B = B[i72];
        _nCalcContext.C = C[i72];
        _nCalcContext.D = D[i72];
        _nCalcContext.E = E[i72];
        _nCalcContext.F = F[i72];
        _nCalcContext.G = G[i72];
        _nCalcContext.H = H[i72];
        _nCalcContext.I = I[i72];
        _nCalcContext.J = J[i72];
        _nCalcContext.K = K[i72];
        sum += _nCalcLambda(_nCalcContext);

        var i73 = NextIndex();
        _nCalcContext.A = A[i73];
        _nCalcContext.B = B[i73];
        _nCalcContext.C = C[i73];
        _nCalcContext.D = D[i73];
        _nCalcContext.E = E[i73];
        _nCalcContext.F = F[i73];
        _nCalcContext.G = G[i73];
        _nCalcContext.H = H[i73];
        _nCalcContext.I = I[i73];
        _nCalcContext.J = J[i73];
        _nCalcContext.K = K[i73];
        sum += _nCalcLambda(_nCalcContext);

        var i74 = NextIndex();
        _nCalcContext.A = A[i74];
        _nCalcContext.B = B[i74];
        _nCalcContext.C = C[i74];
        _nCalcContext.D = D[i74];
        _nCalcContext.E = E[i74];
        _nCalcContext.F = F[i74];
        _nCalcContext.G = G[i74];
        _nCalcContext.H = H[i74];
        _nCalcContext.I = I[i74];
        _nCalcContext.J = J[i74];
        _nCalcContext.K = K[i74];
        sum += _nCalcLambda(_nCalcContext);

        var i75 = NextIndex();
        _nCalcContext.A = A[i75];
        _nCalcContext.B = B[i75];
        _nCalcContext.C = C[i75];
        _nCalcContext.D = D[i75];
        _nCalcContext.E = E[i75];
        _nCalcContext.F = F[i75];
        _nCalcContext.G = G[i75];
        _nCalcContext.H = H[i75];
        _nCalcContext.I = I[i75];
        _nCalcContext.J = J[i75];
        _nCalcContext.K = K[i75];
        sum += _nCalcLambda(_nCalcContext);

        var i76 = NextIndex();
        _nCalcContext.A = A[i76];
        _nCalcContext.B = B[i76];
        _nCalcContext.C = C[i76];
        _nCalcContext.D = D[i76];
        _nCalcContext.E = E[i76];
        _nCalcContext.F = F[i76];
        _nCalcContext.G = G[i76];
        _nCalcContext.H = H[i76];
        _nCalcContext.I = I[i76];
        _nCalcContext.J = J[i76];
        _nCalcContext.K = K[i76];
        sum += _nCalcLambda(_nCalcContext);

        var i77 = NextIndex();
        _nCalcContext.A = A[i77];
        _nCalcContext.B = B[i77];
        _nCalcContext.C = C[i77];
        _nCalcContext.D = D[i77];
        _nCalcContext.E = E[i77];
        _nCalcContext.F = F[i77];
        _nCalcContext.G = G[i77];
        _nCalcContext.H = H[i77];
        _nCalcContext.I = I[i77];
        _nCalcContext.J = J[i77];
        _nCalcContext.K = K[i77];
        sum += _nCalcLambda(_nCalcContext);

        var i78 = NextIndex();
        _nCalcContext.A = A[i78];
        _nCalcContext.B = B[i78];
        _nCalcContext.C = C[i78];
        _nCalcContext.D = D[i78];
        _nCalcContext.E = E[i78];
        _nCalcContext.F = F[i78];
        _nCalcContext.G = G[i78];
        _nCalcContext.H = H[i78];
        _nCalcContext.I = I[i78];
        _nCalcContext.J = J[i78];
        _nCalcContext.K = K[i78];
        sum += _nCalcLambda(_nCalcContext);

        var i79 = NextIndex();
        _nCalcContext.A = A[i79];
        _nCalcContext.B = B[i79];
        _nCalcContext.C = C[i79];
        _nCalcContext.D = D[i79];
        _nCalcContext.E = E[i79];
        _nCalcContext.F = F[i79];
        _nCalcContext.G = G[i79];
        _nCalcContext.H = H[i79];
        _nCalcContext.I = I[i79];
        _nCalcContext.J = J[i79];
        _nCalcContext.K = K[i79];
        sum += _nCalcLambda(_nCalcContext);

        var i80 = NextIndex();
        _nCalcContext.A = A[i80];
        _nCalcContext.B = B[i80];
        _nCalcContext.C = C[i80];
        _nCalcContext.D = D[i80];
        _nCalcContext.E = E[i80];
        _nCalcContext.F = F[i80];
        _nCalcContext.G = G[i80];
        _nCalcContext.H = H[i80];
        _nCalcContext.I = I[i80];
        _nCalcContext.J = J[i80];
        _nCalcContext.K = K[i80];
        sum += _nCalcLambda(_nCalcContext);

        var i81 = NextIndex();
        _nCalcContext.A = A[i81];
        _nCalcContext.B = B[i81];
        _nCalcContext.C = C[i81];
        _nCalcContext.D = D[i81];
        _nCalcContext.E = E[i81];
        _nCalcContext.F = F[i81];
        _nCalcContext.G = G[i81];
        _nCalcContext.H = H[i81];
        _nCalcContext.I = I[i81];
        _nCalcContext.J = J[i81];
        _nCalcContext.K = K[i81];
        sum += _nCalcLambda(_nCalcContext);

        var i82 = NextIndex();
        _nCalcContext.A = A[i82];
        _nCalcContext.B = B[i82];
        _nCalcContext.C = C[i82];
        _nCalcContext.D = D[i82];
        _nCalcContext.E = E[i82];
        _nCalcContext.F = F[i82];
        _nCalcContext.G = G[i82];
        _nCalcContext.H = H[i82];
        _nCalcContext.I = I[i82];
        _nCalcContext.J = J[i82];
        _nCalcContext.K = K[i82];
        sum += _nCalcLambda(_nCalcContext);

        var i83 = NextIndex();
        _nCalcContext.A = A[i83];
        _nCalcContext.B = B[i83];
        _nCalcContext.C = C[i83];
        _nCalcContext.D = D[i83];
        _nCalcContext.E = E[i83];
        _nCalcContext.F = F[i83];
        _nCalcContext.G = G[i83];
        _nCalcContext.H = H[i83];
        _nCalcContext.I = I[i83];
        _nCalcContext.J = J[i83];
        _nCalcContext.K = K[i83];
        sum += _nCalcLambda(_nCalcContext);

        var i84 = NextIndex();
        _nCalcContext.A = A[i84];
        _nCalcContext.B = B[i84];
        _nCalcContext.C = C[i84];
        _nCalcContext.D = D[i84];
        _nCalcContext.E = E[i84];
        _nCalcContext.F = F[i84];
        _nCalcContext.G = G[i84];
        _nCalcContext.H = H[i84];
        _nCalcContext.I = I[i84];
        _nCalcContext.J = J[i84];
        _nCalcContext.K = K[i84];
        sum += _nCalcLambda(_nCalcContext);

        var i85 = NextIndex();
        _nCalcContext.A = A[i85];
        _nCalcContext.B = B[i85];
        _nCalcContext.C = C[i85];
        _nCalcContext.D = D[i85];
        _nCalcContext.E = E[i85];
        _nCalcContext.F = F[i85];
        _nCalcContext.G = G[i85];
        _nCalcContext.H = H[i85];
        _nCalcContext.I = I[i85];
        _nCalcContext.J = J[i85];
        _nCalcContext.K = K[i85];
        sum += _nCalcLambda(_nCalcContext);

        var i86 = NextIndex();
        _nCalcContext.A = A[i86];
        _nCalcContext.B = B[i86];
        _nCalcContext.C = C[i86];
        _nCalcContext.D = D[i86];
        _nCalcContext.E = E[i86];
        _nCalcContext.F = F[i86];
        _nCalcContext.G = G[i86];
        _nCalcContext.H = H[i86];
        _nCalcContext.I = I[i86];
        _nCalcContext.J = J[i86];
        _nCalcContext.K = K[i86];
        sum += _nCalcLambda(_nCalcContext);

        var i87 = NextIndex();
        _nCalcContext.A = A[i87];
        _nCalcContext.B = B[i87];
        _nCalcContext.C = C[i87];
        _nCalcContext.D = D[i87];
        _nCalcContext.E = E[i87];
        _nCalcContext.F = F[i87];
        _nCalcContext.G = G[i87];
        _nCalcContext.H = H[i87];
        _nCalcContext.I = I[i87];
        _nCalcContext.J = J[i87];
        _nCalcContext.K = K[i87];
        sum += _nCalcLambda(_nCalcContext);

        var i88 = NextIndex();
        _nCalcContext.A = A[i88];
        _nCalcContext.B = B[i88];
        _nCalcContext.C = C[i88];
        _nCalcContext.D = D[i88];
        _nCalcContext.E = E[i88];
        _nCalcContext.F = F[i88];
        _nCalcContext.G = G[i88];
        _nCalcContext.H = H[i88];
        _nCalcContext.I = I[i88];
        _nCalcContext.J = J[i88];
        _nCalcContext.K = K[i88];
        sum += _nCalcLambda(_nCalcContext);

        var i89 = NextIndex();
        _nCalcContext.A = A[i89];
        _nCalcContext.B = B[i89];
        _nCalcContext.C = C[i89];
        _nCalcContext.D = D[i89];
        _nCalcContext.E = E[i89];
        _nCalcContext.F = F[i89];
        _nCalcContext.G = G[i89];
        _nCalcContext.H = H[i89];
        _nCalcContext.I = I[i89];
        _nCalcContext.J = J[i89];
        _nCalcContext.K = K[i89];
        sum += _nCalcLambda(_nCalcContext);

        var i90 = NextIndex();
        _nCalcContext.A = A[i90];
        _nCalcContext.B = B[i90];
        _nCalcContext.C = C[i90];
        _nCalcContext.D = D[i90];
        _nCalcContext.E = E[i90];
        _nCalcContext.F = F[i90];
        _nCalcContext.G = G[i90];
        _nCalcContext.H = H[i90];
        _nCalcContext.I = I[i90];
        _nCalcContext.J = J[i90];
        _nCalcContext.K = K[i90];
        sum += _nCalcLambda(_nCalcContext);

        var i91 = NextIndex();
        _nCalcContext.A = A[i91];
        _nCalcContext.B = B[i91];
        _nCalcContext.C = C[i91];
        _nCalcContext.D = D[i91];
        _nCalcContext.E = E[i91];
        _nCalcContext.F = F[i91];
        _nCalcContext.G = G[i91];
        _nCalcContext.H = H[i91];
        _nCalcContext.I = I[i91];
        _nCalcContext.J = J[i91];
        _nCalcContext.K = K[i91];
        sum += _nCalcLambda(_nCalcContext);

        var i92 = NextIndex();
        _nCalcContext.A = A[i92];
        _nCalcContext.B = B[i92];
        _nCalcContext.C = C[i92];
        _nCalcContext.D = D[i92];
        _nCalcContext.E = E[i92];
        _nCalcContext.F = F[i92];
        _nCalcContext.G = G[i92];
        _nCalcContext.H = H[i92];
        _nCalcContext.I = I[i92];
        _nCalcContext.J = J[i92];
        _nCalcContext.K = K[i92];
        sum += _nCalcLambda(_nCalcContext);

        var i93 = NextIndex();
        _nCalcContext.A = A[i93];
        _nCalcContext.B = B[i93];
        _nCalcContext.C = C[i93];
        _nCalcContext.D = D[i93];
        _nCalcContext.E = E[i93];
        _nCalcContext.F = F[i93];
        _nCalcContext.G = G[i93];
        _nCalcContext.H = H[i93];
        _nCalcContext.I = I[i93];
        _nCalcContext.J = J[i93];
        _nCalcContext.K = K[i93];
        sum += _nCalcLambda(_nCalcContext);

        var i94 = NextIndex();
        _nCalcContext.A = A[i94];
        _nCalcContext.B = B[i94];
        _nCalcContext.C = C[i94];
        _nCalcContext.D = D[i94];
        _nCalcContext.E = E[i94];
        _nCalcContext.F = F[i94];
        _nCalcContext.G = G[i94];
        _nCalcContext.H = H[i94];
        _nCalcContext.I = I[i94];
        _nCalcContext.J = J[i94];
        _nCalcContext.K = K[i94];
        sum += _nCalcLambda(_nCalcContext);

        var i95 = NextIndex();
        _nCalcContext.A = A[i95];
        _nCalcContext.B = B[i95];
        _nCalcContext.C = C[i95];
        _nCalcContext.D = D[i95];
        _nCalcContext.E = E[i95];
        _nCalcContext.F = F[i95];
        _nCalcContext.G = G[i95];
        _nCalcContext.H = H[i95];
        _nCalcContext.I = I[i95];
        _nCalcContext.J = J[i95];
        _nCalcContext.K = K[i95];
        sum += _nCalcLambda(_nCalcContext);

        var i96 = NextIndex();
        _nCalcContext.A = A[i96];
        _nCalcContext.B = B[i96];
        _nCalcContext.C = C[i96];
        _nCalcContext.D = D[i96];
        _nCalcContext.E = E[i96];
        _nCalcContext.F = F[i96];
        _nCalcContext.G = G[i96];
        _nCalcContext.H = H[i96];
        _nCalcContext.I = I[i96];
        _nCalcContext.J = J[i96];
        _nCalcContext.K = K[i96];
        sum += _nCalcLambda(_nCalcContext);

        var i97 = NextIndex();
        _nCalcContext.A = A[i97];
        _nCalcContext.B = B[i97];
        _nCalcContext.C = C[i97];
        _nCalcContext.D = D[i97];
        _nCalcContext.E = E[i97];
        _nCalcContext.F = F[i97];
        _nCalcContext.G = G[i97];
        _nCalcContext.H = H[i97];
        _nCalcContext.I = I[i97];
        _nCalcContext.J = J[i97];
        _nCalcContext.K = K[i97];
        sum += _nCalcLambda(_nCalcContext);

        var i98 = NextIndex();
        _nCalcContext.A = A[i98];
        _nCalcContext.B = B[i98];
        _nCalcContext.C = C[i98];
        _nCalcContext.D = D[i98];
        _nCalcContext.E = E[i98];
        _nCalcContext.F = F[i98];
        _nCalcContext.G = G[i98];
        _nCalcContext.H = H[i98];
        _nCalcContext.I = I[i98];
        _nCalcContext.J = J[i98];
        _nCalcContext.K = K[i98];
        sum += _nCalcLambda(_nCalcContext);

        var i99 = NextIndex();
        _nCalcContext.A = A[i99];
        _nCalcContext.B = B[i99];
        _nCalcContext.C = C[i99];
        _nCalcContext.D = D[i99];
        _nCalcContext.E = E[i99];
        _nCalcContext.F = F[i99];
        _nCalcContext.G = G[i99];
        _nCalcContext.H = H[i99];
        _nCalcContext.I = I[i99];
        _nCalcContext.J = J[i99];
        _nCalcContext.K = K[i99];
        sum += _nCalcLambda(_nCalcContext);

        var i100 = NextIndex();
        _nCalcContext.A = A[i100];
        _nCalcContext.B = B[i100];
        _nCalcContext.C = C[i100];
        _nCalcContext.D = D[i100];
        _nCalcContext.E = E[i100];
        _nCalcContext.F = F[i100];
        _nCalcContext.G = G[i100];
        _nCalcContext.H = H[i100];
        _nCalcContext.I = I[i100];
        _nCalcContext.J = J[i100];
        _nCalcContext.K = K[i100];
        sum += _nCalcLambda(_nCalcContext);

        var i101 = NextIndex();
        _nCalcContext.A = A[i101];
        _nCalcContext.B = B[i101];
        _nCalcContext.C = C[i101];
        _nCalcContext.D = D[i101];
        _nCalcContext.E = E[i101];
        _nCalcContext.F = F[i101];
        _nCalcContext.G = G[i101];
        _nCalcContext.H = H[i101];
        _nCalcContext.I = I[i101];
        _nCalcContext.J = J[i101];
        _nCalcContext.K = K[i101];
        sum += _nCalcLambda(_nCalcContext);

        var i102 = NextIndex();
        _nCalcContext.A = A[i102];
        _nCalcContext.B = B[i102];
        _nCalcContext.C = C[i102];
        _nCalcContext.D = D[i102];
        _nCalcContext.E = E[i102];
        _nCalcContext.F = F[i102];
        _nCalcContext.G = G[i102];
        _nCalcContext.H = H[i102];
        _nCalcContext.I = I[i102];
        _nCalcContext.J = J[i102];
        _nCalcContext.K = K[i102];
        sum += _nCalcLambda(_nCalcContext);

        var i103 = NextIndex();
        _nCalcContext.A = A[i103];
        _nCalcContext.B = B[i103];
        _nCalcContext.C = C[i103];
        _nCalcContext.D = D[i103];
        _nCalcContext.E = E[i103];
        _nCalcContext.F = F[i103];
        _nCalcContext.G = G[i103];
        _nCalcContext.H = H[i103];
        _nCalcContext.I = I[i103];
        _nCalcContext.J = J[i103];
        _nCalcContext.K = K[i103];
        sum += _nCalcLambda(_nCalcContext);

        var i104 = NextIndex();
        _nCalcContext.A = A[i104];
        _nCalcContext.B = B[i104];
        _nCalcContext.C = C[i104];
        _nCalcContext.D = D[i104];
        _nCalcContext.E = E[i104];
        _nCalcContext.F = F[i104];
        _nCalcContext.G = G[i104];
        _nCalcContext.H = H[i104];
        _nCalcContext.I = I[i104];
        _nCalcContext.J = J[i104];
        _nCalcContext.K = K[i104];
        sum += _nCalcLambda(_nCalcContext);

        var i105 = NextIndex();
        _nCalcContext.A = A[i105];
        _nCalcContext.B = B[i105];
        _nCalcContext.C = C[i105];
        _nCalcContext.D = D[i105];
        _nCalcContext.E = E[i105];
        _nCalcContext.F = F[i105];
        _nCalcContext.G = G[i105];
        _nCalcContext.H = H[i105];
        _nCalcContext.I = I[i105];
        _nCalcContext.J = J[i105];
        _nCalcContext.K = K[i105];
        sum += _nCalcLambda(_nCalcContext);

        var i106 = NextIndex();
        _nCalcContext.A = A[i106];
        _nCalcContext.B = B[i106];
        _nCalcContext.C = C[i106];
        _nCalcContext.D = D[i106];
        _nCalcContext.E = E[i106];
        _nCalcContext.F = F[i106];
        _nCalcContext.G = G[i106];
        _nCalcContext.H = H[i106];
        _nCalcContext.I = I[i106];
        _nCalcContext.J = J[i106];
        _nCalcContext.K = K[i106];
        sum += _nCalcLambda(_nCalcContext);

        var i107 = NextIndex();
        _nCalcContext.A = A[i107];
        _nCalcContext.B = B[i107];
        _nCalcContext.C = C[i107];
        _nCalcContext.D = D[i107];
        _nCalcContext.E = E[i107];
        _nCalcContext.F = F[i107];
        _nCalcContext.G = G[i107];
        _nCalcContext.H = H[i107];
        _nCalcContext.I = I[i107];
        _nCalcContext.J = J[i107];
        _nCalcContext.K = K[i107];
        sum += _nCalcLambda(_nCalcContext);

        var i108 = NextIndex();
        _nCalcContext.A = A[i108];
        _nCalcContext.B = B[i108];
        _nCalcContext.C = C[i108];
        _nCalcContext.D = D[i108];
        _nCalcContext.E = E[i108];
        _nCalcContext.F = F[i108];
        _nCalcContext.G = G[i108];
        _nCalcContext.H = H[i108];
        _nCalcContext.I = I[i108];
        _nCalcContext.J = J[i108];
        _nCalcContext.K = K[i108];
        sum += _nCalcLambda(_nCalcContext);

        var i109 = NextIndex();
        _nCalcContext.A = A[i109];
        _nCalcContext.B = B[i109];
        _nCalcContext.C = C[i109];
        _nCalcContext.D = D[i109];
        _nCalcContext.E = E[i109];
        _nCalcContext.F = F[i109];
        _nCalcContext.G = G[i109];
        _nCalcContext.H = H[i109];
        _nCalcContext.I = I[i109];
        _nCalcContext.J = J[i109];
        _nCalcContext.K = K[i109];
        sum += _nCalcLambda(_nCalcContext);

        var i110 = NextIndex();
        _nCalcContext.A = A[i110];
        _nCalcContext.B = B[i110];
        _nCalcContext.C = C[i110];
        _nCalcContext.D = D[i110];
        _nCalcContext.E = E[i110];
        _nCalcContext.F = F[i110];
        _nCalcContext.G = G[i110];
        _nCalcContext.H = H[i110];
        _nCalcContext.I = I[i110];
        _nCalcContext.J = J[i110];
        _nCalcContext.K = K[i110];
        sum += _nCalcLambda(_nCalcContext);

        var i111 = NextIndex();
        _nCalcContext.A = A[i111];
        _nCalcContext.B = B[i111];
        _nCalcContext.C = C[i111];
        _nCalcContext.D = D[i111];
        _nCalcContext.E = E[i111];
        _nCalcContext.F = F[i111];
        _nCalcContext.G = G[i111];
        _nCalcContext.H = H[i111];
        _nCalcContext.I = I[i111];
        _nCalcContext.J = J[i111];
        _nCalcContext.K = K[i111];
        sum += _nCalcLambda(_nCalcContext);

        var i112 = NextIndex();
        _nCalcContext.A = A[i112];
        _nCalcContext.B = B[i112];
        _nCalcContext.C = C[i112];
        _nCalcContext.D = D[i112];
        _nCalcContext.E = E[i112];
        _nCalcContext.F = F[i112];
        _nCalcContext.G = G[i112];
        _nCalcContext.H = H[i112];
        _nCalcContext.I = I[i112];
        _nCalcContext.J = J[i112];
        _nCalcContext.K = K[i112];
        sum += _nCalcLambda(_nCalcContext);

        var i113 = NextIndex();
        _nCalcContext.A = A[i113];
        _nCalcContext.B = B[i113];
        _nCalcContext.C = C[i113];
        _nCalcContext.D = D[i113];
        _nCalcContext.E = E[i113];
        _nCalcContext.F = F[i113];
        _nCalcContext.G = G[i113];
        _nCalcContext.H = H[i113];
        _nCalcContext.I = I[i113];
        _nCalcContext.J = J[i113];
        _nCalcContext.K = K[i113];
        sum += _nCalcLambda(_nCalcContext);

        var i114 = NextIndex();
        _nCalcContext.A = A[i114];
        _nCalcContext.B = B[i114];
        _nCalcContext.C = C[i114];
        _nCalcContext.D = D[i114];
        _nCalcContext.E = E[i114];
        _nCalcContext.F = F[i114];
        _nCalcContext.G = G[i114];
        _nCalcContext.H = H[i114];
        _nCalcContext.I = I[i114];
        _nCalcContext.J = J[i114];
        _nCalcContext.K = K[i114];
        sum += _nCalcLambda(_nCalcContext);

        var i115 = NextIndex();
        _nCalcContext.A = A[i115];
        _nCalcContext.B = B[i115];
        _nCalcContext.C = C[i115];
        _nCalcContext.D = D[i115];
        _nCalcContext.E = E[i115];
        _nCalcContext.F = F[i115];
        _nCalcContext.G = G[i115];
        _nCalcContext.H = H[i115];
        _nCalcContext.I = I[i115];
        _nCalcContext.J = J[i115];
        _nCalcContext.K = K[i115];
        sum += _nCalcLambda(_nCalcContext);

        var i116 = NextIndex();
        _nCalcContext.A = A[i116];
        _nCalcContext.B = B[i116];
        _nCalcContext.C = C[i116];
        _nCalcContext.D = D[i116];
        _nCalcContext.E = E[i116];
        _nCalcContext.F = F[i116];
        _nCalcContext.G = G[i116];
        _nCalcContext.H = H[i116];
        _nCalcContext.I = I[i116];
        _nCalcContext.J = J[i116];
        _nCalcContext.K = K[i116];
        sum += _nCalcLambda(_nCalcContext);

        var i117 = NextIndex();
        _nCalcContext.A = A[i117];
        _nCalcContext.B = B[i117];
        _nCalcContext.C = C[i117];
        _nCalcContext.D = D[i117];
        _nCalcContext.E = E[i117];
        _nCalcContext.F = F[i117];
        _nCalcContext.G = G[i117];
        _nCalcContext.H = H[i117];
        _nCalcContext.I = I[i117];
        _nCalcContext.J = J[i117];
        _nCalcContext.K = K[i117];
        sum += _nCalcLambda(_nCalcContext);

        var i118 = NextIndex();
        _nCalcContext.A = A[i118];
        _nCalcContext.B = B[i118];
        _nCalcContext.C = C[i118];
        _nCalcContext.D = D[i118];
        _nCalcContext.E = E[i118];
        _nCalcContext.F = F[i118];
        _nCalcContext.G = G[i118];
        _nCalcContext.H = H[i118];
        _nCalcContext.I = I[i118];
        _nCalcContext.J = J[i118];
        _nCalcContext.K = K[i118];
        sum += _nCalcLambda(_nCalcContext);

        var i119 = NextIndex();
        _nCalcContext.A = A[i119];
        _nCalcContext.B = B[i119];
        _nCalcContext.C = C[i119];
        _nCalcContext.D = D[i119];
        _nCalcContext.E = E[i119];
        _nCalcContext.F = F[i119];
        _nCalcContext.G = G[i119];
        _nCalcContext.H = H[i119];
        _nCalcContext.I = I[i119];
        _nCalcContext.J = J[i119];
        _nCalcContext.K = K[i119];
        sum += _nCalcLambda(_nCalcContext);

        var i120 = NextIndex();
        _nCalcContext.A = A[i120];
        _nCalcContext.B = B[i120];
        _nCalcContext.C = C[i120];
        _nCalcContext.D = D[i120];
        _nCalcContext.E = E[i120];
        _nCalcContext.F = F[i120];
        _nCalcContext.G = G[i120];
        _nCalcContext.H = H[i120];
        _nCalcContext.I = I[i120];
        _nCalcContext.J = J[i120];
        _nCalcContext.K = K[i120];
        sum += _nCalcLambda(_nCalcContext);

        var i121 = NextIndex();
        _nCalcContext.A = A[i121];
        _nCalcContext.B = B[i121];
        _nCalcContext.C = C[i121];
        _nCalcContext.D = D[i121];
        _nCalcContext.E = E[i121];
        _nCalcContext.F = F[i121];
        _nCalcContext.G = G[i121];
        _nCalcContext.H = H[i121];
        _nCalcContext.I = I[i121];
        _nCalcContext.J = J[i121];
        _nCalcContext.K = K[i121];
        sum += _nCalcLambda(_nCalcContext);

        var i122 = NextIndex();
        _nCalcContext.A = A[i122];
        _nCalcContext.B = B[i122];
        _nCalcContext.C = C[i122];
        _nCalcContext.D = D[i122];
        _nCalcContext.E = E[i122];
        _nCalcContext.F = F[i122];
        _nCalcContext.G = G[i122];
        _nCalcContext.H = H[i122];
        _nCalcContext.I = I[i122];
        _nCalcContext.J = J[i122];
        _nCalcContext.K = K[i122];
        sum += _nCalcLambda(_nCalcContext);

        var i123 = NextIndex();
        _nCalcContext.A = A[i123];
        _nCalcContext.B = B[i123];
        _nCalcContext.C = C[i123];
        _nCalcContext.D = D[i123];
        _nCalcContext.E = E[i123];
        _nCalcContext.F = F[i123];
        _nCalcContext.G = G[i123];
        _nCalcContext.H = H[i123];
        _nCalcContext.I = I[i123];
        _nCalcContext.J = J[i123];
        _nCalcContext.K = K[i123];
        sum += _nCalcLambda(_nCalcContext);

        var i124 = NextIndex();
        _nCalcContext.A = A[i124];
        _nCalcContext.B = B[i124];
        _nCalcContext.C = C[i124];
        _nCalcContext.D = D[i124];
        _nCalcContext.E = E[i124];
        _nCalcContext.F = F[i124];
        _nCalcContext.G = G[i124];
        _nCalcContext.H = H[i124];
        _nCalcContext.I = I[i124];
        _nCalcContext.J = J[i124];
        _nCalcContext.K = K[i124];
        sum += _nCalcLambda(_nCalcContext);

        var i125 = NextIndex();
        _nCalcContext.A = A[i125];
        _nCalcContext.B = B[i125];
        _nCalcContext.C = C[i125];
        _nCalcContext.D = D[i125];
        _nCalcContext.E = E[i125];
        _nCalcContext.F = F[i125];
        _nCalcContext.G = G[i125];
        _nCalcContext.H = H[i125];
        _nCalcContext.I = I[i125];
        _nCalcContext.J = J[i125];
        _nCalcContext.K = K[i125];
        sum += _nCalcLambda(_nCalcContext);

        var i126 = NextIndex();
        _nCalcContext.A = A[i126];
        _nCalcContext.B = B[i126];
        _nCalcContext.C = C[i126];
        _nCalcContext.D = D[i126];
        _nCalcContext.E = E[i126];
        _nCalcContext.F = F[i126];
        _nCalcContext.G = G[i126];
        _nCalcContext.H = H[i126];
        _nCalcContext.I = I[i126];
        _nCalcContext.J = J[i126];
        _nCalcContext.K = K[i126];
        sum += _nCalcLambda(_nCalcContext);

        var i127 = NextIndex();
        _nCalcContext.A = A[i127];
        _nCalcContext.B = B[i127];
        _nCalcContext.C = C[i127];
        _nCalcContext.D = D[i127];
        _nCalcContext.E = E[i127];
        _nCalcContext.F = F[i127];
        _nCalcContext.G = G[i127];
        _nCalcContext.H = H[i127];
        _nCalcContext.I = I[i127];
        _nCalcContext.J = J[i127];
        _nCalcContext.K = K[i127];
        sum += _nCalcLambda(_nCalcContext);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double Wist_Cil_FastInvoker_Unrolled128()
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

        var i16 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i16], B[i16], C[i16], D[i16], E[i16], F[i16], G[i16], H[i16], I[i16], J[i16], K[i16]);

        var i17 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i17], B[i17], C[i17], D[i17], E[i17], F[i17], G[i17], H[i17], I[i17], J[i17], K[i17]);

        var i18 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i18], B[i18], C[i18], D[i18], E[i18], F[i18], G[i18], H[i18], I[i18], J[i18], K[i18]);

        var i19 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i19], B[i19], C[i19], D[i19], E[i19], F[i19], G[i19], H[i19], I[i19], J[i19], K[i19]);

        var i20 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i20], B[i20], C[i20], D[i20], E[i20], F[i20], G[i20], H[i20], I[i20], J[i20], K[i20]);

        var i21 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i21], B[i21], C[i21], D[i21], E[i21], F[i21], G[i21], H[i21], I[i21], J[i21], K[i21]);

        var i22 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i22], B[i22], C[i22], D[i22], E[i22], F[i22], G[i22], H[i22], I[i22], J[i22], K[i22]);

        var i23 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i23], B[i23], C[i23], D[i23], E[i23], F[i23], G[i23], H[i23], I[i23], J[i23], K[i23]);

        var i24 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i24], B[i24], C[i24], D[i24], E[i24], F[i24], G[i24], H[i24], I[i24], J[i24], K[i24]);

        var i25 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i25], B[i25], C[i25], D[i25], E[i25], F[i25], G[i25], H[i25], I[i25], J[i25], K[i25]);

        var i26 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i26], B[i26], C[i26], D[i26], E[i26], F[i26], G[i26], H[i26], I[i26], J[i26], K[i26]);

        var i27 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i27], B[i27], C[i27], D[i27], E[i27], F[i27], G[i27], H[i27], I[i27], J[i27], K[i27]);

        var i28 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i28], B[i28], C[i28], D[i28], E[i28], F[i28], G[i28], H[i28], I[i28], J[i28], K[i28]);

        var i29 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i29], B[i29], C[i29], D[i29], E[i29], F[i29], G[i29], H[i29], I[i29], J[i29], K[i29]);

        var i30 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i30], B[i30], C[i30], D[i30], E[i30], F[i30], G[i30], H[i30], I[i30], J[i30], K[i30]);

        var i31 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i31], B[i31], C[i31], D[i31], E[i31], F[i31], G[i31], H[i31], I[i31], J[i31], K[i31]);

        var i32 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i32], B[i32], C[i32], D[i32], E[i32], F[i32], G[i32], H[i32], I[i32], J[i32], K[i32]);

        var i33 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i33], B[i33], C[i33], D[i33], E[i33], F[i33], G[i33], H[i33], I[i33], J[i33], K[i33]);

        var i34 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i34], B[i34], C[i34], D[i34], E[i34], F[i34], G[i34], H[i34], I[i34], J[i34], K[i34]);

        var i35 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i35], B[i35], C[i35], D[i35], E[i35], F[i35], G[i35], H[i35], I[i35], J[i35], K[i35]);

        var i36 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i36], B[i36], C[i36], D[i36], E[i36], F[i36], G[i36], H[i36], I[i36], J[i36], K[i36]);

        var i37 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i37], B[i37], C[i37], D[i37], E[i37], F[i37], G[i37], H[i37], I[i37], J[i37], K[i37]);

        var i38 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i38], B[i38], C[i38], D[i38], E[i38], F[i38], G[i38], H[i38], I[i38], J[i38], K[i38]);

        var i39 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i39], B[i39], C[i39], D[i39], E[i39], F[i39], G[i39], H[i39], I[i39], J[i39], K[i39]);

        var i40 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i40], B[i40], C[i40], D[i40], E[i40], F[i40], G[i40], H[i40], I[i40], J[i40], K[i40]);

        var i41 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i41], B[i41], C[i41], D[i41], E[i41], F[i41], G[i41], H[i41], I[i41], J[i41], K[i41]);

        var i42 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i42], B[i42], C[i42], D[i42], E[i42], F[i42], G[i42], H[i42], I[i42], J[i42], K[i42]);

        var i43 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i43], B[i43], C[i43], D[i43], E[i43], F[i43], G[i43], H[i43], I[i43], J[i43], K[i43]);

        var i44 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i44], B[i44], C[i44], D[i44], E[i44], F[i44], G[i44], H[i44], I[i44], J[i44], K[i44]);

        var i45 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i45], B[i45], C[i45], D[i45], E[i45], F[i45], G[i45], H[i45], I[i45], J[i45], K[i45]);

        var i46 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i46], B[i46], C[i46], D[i46], E[i46], F[i46], G[i46], H[i46], I[i46], J[i46], K[i46]);

        var i47 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i47], B[i47], C[i47], D[i47], E[i47], F[i47], G[i47], H[i47], I[i47], J[i47], K[i47]);

        var i48 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i48], B[i48], C[i48], D[i48], E[i48], F[i48], G[i48], H[i48], I[i48], J[i48], K[i48]);

        var i49 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i49], B[i49], C[i49], D[i49], E[i49], F[i49], G[i49], H[i49], I[i49], J[i49], K[i49]);

        var i50 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i50], B[i50], C[i50], D[i50], E[i50], F[i50], G[i50], H[i50], I[i50], J[i50], K[i50]);

        var i51 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i51], B[i51], C[i51], D[i51], E[i51], F[i51], G[i51], H[i51], I[i51], J[i51], K[i51]);

        var i52 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i52], B[i52], C[i52], D[i52], E[i52], F[i52], G[i52], H[i52], I[i52], J[i52], K[i52]);

        var i53 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i53], B[i53], C[i53], D[i53], E[i53], F[i53], G[i53], H[i53], I[i53], J[i53], K[i53]);

        var i54 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i54], B[i54], C[i54], D[i54], E[i54], F[i54], G[i54], H[i54], I[i54], J[i54], K[i54]);

        var i55 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i55], B[i55], C[i55], D[i55], E[i55], F[i55], G[i55], H[i55], I[i55], J[i55], K[i55]);

        var i56 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i56], B[i56], C[i56], D[i56], E[i56], F[i56], G[i56], H[i56], I[i56], J[i56], K[i56]);

        var i57 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i57], B[i57], C[i57], D[i57], E[i57], F[i57], G[i57], H[i57], I[i57], J[i57], K[i57]);

        var i58 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i58], B[i58], C[i58], D[i58], E[i58], F[i58], G[i58], H[i58], I[i58], J[i58], K[i58]);

        var i59 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i59], B[i59], C[i59], D[i59], E[i59], F[i59], G[i59], H[i59], I[i59], J[i59], K[i59]);

        var i60 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i60], B[i60], C[i60], D[i60], E[i60], F[i60], G[i60], H[i60], I[i60], J[i60], K[i60]);

        var i61 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i61], B[i61], C[i61], D[i61], E[i61], F[i61], G[i61], H[i61], I[i61], J[i61], K[i61]);

        var i62 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i62], B[i62], C[i62], D[i62], E[i62], F[i62], G[i62], H[i62], I[i62], J[i62], K[i62]);

        var i63 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i63], B[i63], C[i63], D[i63], E[i63], F[i63], G[i63], H[i63], I[i63], J[i63], K[i63]);

        var i64 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i64], B[i64], C[i64], D[i64], E[i64], F[i64], G[i64], H[i64], I[i64], J[i64], K[i64]);

        var i65 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i65], B[i65], C[i65], D[i65], E[i65], F[i65], G[i65], H[i65], I[i65], J[i65], K[i65]);

        var i66 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i66], B[i66], C[i66], D[i66], E[i66], F[i66], G[i66], H[i66], I[i66], J[i66], K[i66]);

        var i67 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i67], B[i67], C[i67], D[i67], E[i67], F[i67], G[i67], H[i67], I[i67], J[i67], K[i67]);

        var i68 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i68], B[i68], C[i68], D[i68], E[i68], F[i68], G[i68], H[i68], I[i68], J[i68], K[i68]);

        var i69 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i69], B[i69], C[i69], D[i69], E[i69], F[i69], G[i69], H[i69], I[i69], J[i69], K[i69]);

        var i70 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i70], B[i70], C[i70], D[i70], E[i70], F[i70], G[i70], H[i70], I[i70], J[i70], K[i70]);

        var i71 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i71], B[i71], C[i71], D[i71], E[i71], F[i71], G[i71], H[i71], I[i71], J[i71], K[i71]);

        var i72 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i72], B[i72], C[i72], D[i72], E[i72], F[i72], G[i72], H[i72], I[i72], J[i72], K[i72]);

        var i73 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i73], B[i73], C[i73], D[i73], E[i73], F[i73], G[i73], H[i73], I[i73], J[i73], K[i73]);

        var i74 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i74], B[i74], C[i74], D[i74], E[i74], F[i74], G[i74], H[i74], I[i74], J[i74], K[i74]);

        var i75 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i75], B[i75], C[i75], D[i75], E[i75], F[i75], G[i75], H[i75], I[i75], J[i75], K[i75]);

        var i76 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i76], B[i76], C[i76], D[i76], E[i76], F[i76], G[i76], H[i76], I[i76], J[i76], K[i76]);

        var i77 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i77], B[i77], C[i77], D[i77], E[i77], F[i77], G[i77], H[i77], I[i77], J[i77], K[i77]);

        var i78 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i78], B[i78], C[i78], D[i78], E[i78], F[i78], G[i78], H[i78], I[i78], J[i78], K[i78]);

        var i79 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i79], B[i79], C[i79], D[i79], E[i79], F[i79], G[i79], H[i79], I[i79], J[i79], K[i79]);

        var i80 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i80], B[i80], C[i80], D[i80], E[i80], F[i80], G[i80], H[i80], I[i80], J[i80], K[i80]);

        var i81 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i81], B[i81], C[i81], D[i81], E[i81], F[i81], G[i81], H[i81], I[i81], J[i81], K[i81]);

        var i82 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i82], B[i82], C[i82], D[i82], E[i82], F[i82], G[i82], H[i82], I[i82], J[i82], K[i82]);

        var i83 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i83], B[i83], C[i83], D[i83], E[i83], F[i83], G[i83], H[i83], I[i83], J[i83], K[i83]);

        var i84 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i84], B[i84], C[i84], D[i84], E[i84], F[i84], G[i84], H[i84], I[i84], J[i84], K[i84]);

        var i85 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i85], B[i85], C[i85], D[i85], E[i85], F[i85], G[i85], H[i85], I[i85], J[i85], K[i85]);

        var i86 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i86], B[i86], C[i86], D[i86], E[i86], F[i86], G[i86], H[i86], I[i86], J[i86], K[i86]);

        var i87 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i87], B[i87], C[i87], D[i87], E[i87], F[i87], G[i87], H[i87], I[i87], J[i87], K[i87]);

        var i88 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i88], B[i88], C[i88], D[i88], E[i88], F[i88], G[i88], H[i88], I[i88], J[i88], K[i88]);

        var i89 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i89], B[i89], C[i89], D[i89], E[i89], F[i89], G[i89], H[i89], I[i89], J[i89], K[i89]);

        var i90 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i90], B[i90], C[i90], D[i90], E[i90], F[i90], G[i90], H[i90], I[i90], J[i90], K[i90]);

        var i91 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i91], B[i91], C[i91], D[i91], E[i91], F[i91], G[i91], H[i91], I[i91], J[i91], K[i91]);

        var i92 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i92], B[i92], C[i92], D[i92], E[i92], F[i92], G[i92], H[i92], I[i92], J[i92], K[i92]);

        var i93 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i93], B[i93], C[i93], D[i93], E[i93], F[i93], G[i93], H[i93], I[i93], J[i93], K[i93]);

        var i94 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i94], B[i94], C[i94], D[i94], E[i94], F[i94], G[i94], H[i94], I[i94], J[i94], K[i94]);

        var i95 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i95], B[i95], C[i95], D[i95], E[i95], F[i95], G[i95], H[i95], I[i95], J[i95], K[i95]);

        var i96 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i96], B[i96], C[i96], D[i96], E[i96], F[i96], G[i96], H[i96], I[i96], J[i96], K[i96]);

        var i97 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i97], B[i97], C[i97], D[i97], E[i97], F[i97], G[i97], H[i97], I[i97], J[i97], K[i97]);

        var i98 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i98], B[i98], C[i98], D[i98], E[i98], F[i98], G[i98], H[i98], I[i98], J[i98], K[i98]);

        var i99 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i99], B[i99], C[i99], D[i99], E[i99], F[i99], G[i99], H[i99], I[i99], J[i99], K[i99]);

        var i100 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i100], B[i100], C[i100], D[i100], E[i100], F[i100], G[i100], H[i100], I[i100], J[i100], K[i100]);

        var i101 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i101], B[i101], C[i101], D[i101], E[i101], F[i101], G[i101], H[i101], I[i101], J[i101], K[i101]);

        var i102 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i102], B[i102], C[i102], D[i102], E[i102], F[i102], G[i102], H[i102], I[i102], J[i102], K[i102]);

        var i103 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i103], B[i103], C[i103], D[i103], E[i103], F[i103], G[i103], H[i103], I[i103], J[i103], K[i103]);

        var i104 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i104], B[i104], C[i104], D[i104], E[i104], F[i104], G[i104], H[i104], I[i104], J[i104], K[i104]);

        var i105 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i105], B[i105], C[i105], D[i105], E[i105], F[i105], G[i105], H[i105], I[i105], J[i105], K[i105]);

        var i106 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i106], B[i106], C[i106], D[i106], E[i106], F[i106], G[i106], H[i106], I[i106], J[i106], K[i106]);

        var i107 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i107], B[i107], C[i107], D[i107], E[i107], F[i107], G[i107], H[i107], I[i107], J[i107], K[i107]);

        var i108 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i108], B[i108], C[i108], D[i108], E[i108], F[i108], G[i108], H[i108], I[i108], J[i108], K[i108]);

        var i109 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i109], B[i109], C[i109], D[i109], E[i109], F[i109], G[i109], H[i109], I[i109], J[i109], K[i109]);

        var i110 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i110], B[i110], C[i110], D[i110], E[i110], F[i110], G[i110], H[i110], I[i110], J[i110], K[i110]);

        var i111 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i111], B[i111], C[i111], D[i111], E[i111], F[i111], G[i111], H[i111], I[i111], J[i111], K[i111]);

        var i112 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i112], B[i112], C[i112], D[i112], E[i112], F[i112], G[i112], H[i112], I[i112], J[i112], K[i112]);

        var i113 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i113], B[i113], C[i113], D[i113], E[i113], F[i113], G[i113], H[i113], I[i113], J[i113], K[i113]);

        var i114 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i114], B[i114], C[i114], D[i114], E[i114], F[i114], G[i114], H[i114], I[i114], J[i114], K[i114]);

        var i115 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i115], B[i115], C[i115], D[i115], E[i115], F[i115], G[i115], H[i115], I[i115], J[i115], K[i115]);

        var i116 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i116], B[i116], C[i116], D[i116], E[i116], F[i116], G[i116], H[i116], I[i116], J[i116], K[i116]);

        var i117 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i117], B[i117], C[i117], D[i117], E[i117], F[i117], G[i117], H[i117], I[i117], J[i117], K[i117]);

        var i118 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i118], B[i118], C[i118], D[i118], E[i118], F[i118], G[i118], H[i118], I[i118], J[i118], K[i118]);

        var i119 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i119], B[i119], C[i119], D[i119], E[i119], F[i119], G[i119], H[i119], I[i119], J[i119], K[i119]);

        var i120 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i120], B[i120], C[i120], D[i120], E[i120], F[i120], G[i120], H[i120], I[i120], J[i120], K[i120]);

        var i121 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i121], B[i121], C[i121], D[i121], E[i121], F[i121], G[i121], H[i121], I[i121], J[i121], K[i121]);

        var i122 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i122], B[i122], C[i122], D[i122], E[i122], F[i122], G[i122], H[i122], I[i122], J[i122], K[i122]);

        var i123 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i123], B[i123], C[i123], D[i123], E[i123], F[i123], G[i123], H[i123], I[i123], J[i123], K[i123]);

        var i124 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i124], B[i124], C[i124], D[i124], E[i124], F[i124], G[i124], H[i124], I[i124], J[i124], K[i124]);

        var i125 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i125], B[i125], C[i125], D[i125], E[i125], F[i125], G[i125], H[i125], I[i125], J[i125], K[i125]);

        var i126 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i126], B[i126], C[i126], D[i126], E[i126], F[i126], G[i126], H[i126], I[i126], J[i126], K[i126]);

        var i127 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i127], B[i127], C[i127], D[i127], E[i127], F[i127], G[i127], H[i127], I[i127], J[i127], K[i127]);

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
