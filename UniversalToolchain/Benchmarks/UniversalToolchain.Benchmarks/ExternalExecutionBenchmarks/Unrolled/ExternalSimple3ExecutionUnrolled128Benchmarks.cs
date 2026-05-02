using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DynamicExpresso;
using DynamicMethodCalling.Core;
using NCalc;
using NCalc.LambdaCompilation;

namespace UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks.Unrolled;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ExternalSimple3ExecutionUnrolled128Benchmarks : ExternalArithmeticExecutionUnrolledBenchmarkEnvironmentBase
{
    private const string WistFormula = "A + B * C / 5.0";
    private const string NCalcFormula = "[A] + [B] * [C] / 5.0";
    private const string DynamicExpressoFormula = "A + B * C / 5.0";
    private Func<double, double, double, double> _dynamicExpressoDelegate = null!;

    private ExternalBenchContext3Unrolled _nCalcContext = null!;
    private Func<ExternalBenchContext3Unrolled, double> _nCalcLambda = null!;
    private DynamicMethodInvoker<double, double, double, double> _wistFastInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C"]);
        _wistFastInvoker = new DynamicMethodInvoker<double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<ExternalBenchContext3Unrolled, double>();
        _nCalcContext = new ExternalBenchContext3Unrolled();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate =
            dynamicExpressoInterpreter.ParseAsDelegate<Func<double, double, double, double>>(
                DynamicExpressoFormula,
                "A", "B", "C");

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 16)]
    public double CSharp_NoInliningMethod_Unrolled128()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i0], B[i0], C[i0]);

        var i1 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i1], B[i1], C[i1]);

        var i2 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i2], B[i2], C[i2]);

        var i3 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i3], B[i3], C[i3]);

        var i4 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i4], B[i4], C[i4]);

        var i5 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i5], B[i5], C[i5]);

        var i6 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i6], B[i6], C[i6]);

        var i7 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i7], B[i7], C[i7]);

        var i8 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i8], B[i8], C[i8]);

        var i9 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i9], B[i9], C[i9]);

        var i10 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i10], B[i10], C[i10]);

        var i11 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i11], B[i11], C[i11]);

        var i12 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i12], B[i12], C[i12]);

        var i13 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i13], B[i13], C[i13]);

        var i14 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i14], B[i14], C[i14]);

        var i15 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i15], B[i15], C[i15]);

        var i16 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i16], B[i16], C[i16]);

        var i17 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i17], B[i17], C[i17]);

        var i18 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i18], B[i18], C[i18]);

        var i19 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i19], B[i19], C[i19]);

        var i20 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i20], B[i20], C[i20]);

        var i21 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i21], B[i21], C[i21]);

        var i22 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i22], B[i22], C[i22]);

        var i23 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i23], B[i23], C[i23]);

        var i24 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i24], B[i24], C[i24]);

        var i25 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i25], B[i25], C[i25]);

        var i26 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i26], B[i26], C[i26]);

        var i27 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i27], B[i27], C[i27]);

        var i28 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i28], B[i28], C[i28]);

        var i29 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i29], B[i29], C[i29]);

        var i30 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i30], B[i30], C[i30]);

        var i31 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i31], B[i31], C[i31]);

        var i32 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i32], B[i32], C[i32]);

        var i33 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i33], B[i33], C[i33]);

        var i34 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i34], B[i34], C[i34]);

        var i35 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i35], B[i35], C[i35]);

        var i36 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i36], B[i36], C[i36]);

        var i37 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i37], B[i37], C[i37]);

        var i38 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i38], B[i38], C[i38]);

        var i39 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i39], B[i39], C[i39]);

        var i40 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i40], B[i40], C[i40]);

        var i41 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i41], B[i41], C[i41]);

        var i42 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i42], B[i42], C[i42]);

        var i43 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i43], B[i43], C[i43]);

        var i44 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i44], B[i44], C[i44]);

        var i45 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i45], B[i45], C[i45]);

        var i46 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i46], B[i46], C[i46]);

        var i47 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i47], B[i47], C[i47]);

        var i48 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i48], B[i48], C[i48]);

        var i49 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i49], B[i49], C[i49]);

        var i50 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i50], B[i50], C[i50]);

        var i51 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i51], B[i51], C[i51]);

        var i52 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i52], B[i52], C[i52]);

        var i53 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i53], B[i53], C[i53]);

        var i54 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i54], B[i54], C[i54]);

        var i55 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i55], B[i55], C[i55]);

        var i56 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i56], B[i56], C[i56]);

        var i57 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i57], B[i57], C[i57]);

        var i58 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i58], B[i58], C[i58]);

        var i59 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i59], B[i59], C[i59]);

        var i60 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i60], B[i60], C[i60]);

        var i61 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i61], B[i61], C[i61]);

        var i62 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i62], B[i62], C[i62]);

        var i63 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i63], B[i63], C[i63]);

        var i64 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i64], B[i64], C[i64]);

        var i65 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i65], B[i65], C[i65]);

        var i66 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i66], B[i66], C[i66]);

        var i67 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i67], B[i67], C[i67]);

        var i68 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i68], B[i68], C[i68]);

        var i69 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i69], B[i69], C[i69]);

        var i70 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i70], B[i70], C[i70]);

        var i71 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i71], B[i71], C[i71]);

        var i72 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i72], B[i72], C[i72]);

        var i73 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i73], B[i73], C[i73]);

        var i74 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i74], B[i74], C[i74]);

        var i75 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i75], B[i75], C[i75]);

        var i76 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i76], B[i76], C[i76]);

        var i77 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i77], B[i77], C[i77]);

        var i78 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i78], B[i78], C[i78]);

        var i79 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i79], B[i79], C[i79]);

        var i80 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i80], B[i80], C[i80]);

        var i81 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i81], B[i81], C[i81]);

        var i82 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i82], B[i82], C[i82]);

        var i83 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i83], B[i83], C[i83]);

        var i84 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i84], B[i84], C[i84]);

        var i85 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i85], B[i85], C[i85]);

        var i86 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i86], B[i86], C[i86]);

        var i87 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i87], B[i87], C[i87]);

        var i88 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i88], B[i88], C[i88]);

        var i89 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i89], B[i89], C[i89]);

        var i90 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i90], B[i90], C[i90]);

        var i91 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i91], B[i91], C[i91]);

        var i92 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i92], B[i92], C[i92]);

        var i93 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i93], B[i93], C[i93]);

        var i94 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i94], B[i94], C[i94]);

        var i95 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i95], B[i95], C[i95]);

        var i96 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i96], B[i96], C[i96]);

        var i97 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i97], B[i97], C[i97]);

        var i98 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i98], B[i98], C[i98]);

        var i99 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i99], B[i99], C[i99]);

        var i100 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i100], B[i100], C[i100]);

        var i101 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i101], B[i101], C[i101]);

        var i102 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i102], B[i102], C[i102]);

        var i103 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i103], B[i103], C[i103]);

        var i104 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i104], B[i104], C[i104]);

        var i105 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i105], B[i105], C[i105]);

        var i106 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i106], B[i106], C[i106]);

        var i107 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i107], B[i107], C[i107]);

        var i108 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i108], B[i108], C[i108]);

        var i109 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i109], B[i109], C[i109]);

        var i110 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i110], B[i110], C[i110]);

        var i111 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i111], B[i111], C[i111]);

        var i112 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i112], B[i112], C[i112]);

        var i113 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i113], B[i113], C[i113]);

        var i114 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i114], B[i114], C[i114]);

        var i115 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i115], B[i115], C[i115]);

        var i116 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i116], B[i116], C[i116]);

        var i117 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i117], B[i117], C[i117]);

        var i118 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i118], B[i118], C[i118]);

        var i119 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i119], B[i119], C[i119]);

        var i120 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i120], B[i120], C[i120]);

        var i121 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i121], B[i121], C[i121]);

        var i122 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i122], B[i122], C[i122]);

        var i123 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i123], B[i123], C[i123]);

        var i124 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i124], B[i124], C[i124]);

        var i125 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i125], B[i125], C[i125]);

        var i126 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i126], B[i126], C[i126]);

        var i127 = NextIndex();
        sum += CSharp_NoInliningMethodCore(A[i127], B[i127], C[i127]);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double DynamicExpresso_Delegate_Unrolled128()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i0], B[i0], C[i0]);

        var i1 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i1], B[i1], C[i1]);

        var i2 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i2], B[i2], C[i2]);

        var i3 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i3], B[i3], C[i3]);

        var i4 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i4], B[i4], C[i4]);

        var i5 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i5], B[i5], C[i5]);

        var i6 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i6], B[i6], C[i6]);

        var i7 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i7], B[i7], C[i7]);

        var i8 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i8], B[i8], C[i8]);

        var i9 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i9], B[i9], C[i9]);

        var i10 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i10], B[i10], C[i10]);

        var i11 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i11], B[i11], C[i11]);

        var i12 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i12], B[i12], C[i12]);

        var i13 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i13], B[i13], C[i13]);

        var i14 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i14], B[i14], C[i14]);

        var i15 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i15], B[i15], C[i15]);

        var i16 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i16], B[i16], C[i16]);

        var i17 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i17], B[i17], C[i17]);

        var i18 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i18], B[i18], C[i18]);

        var i19 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i19], B[i19], C[i19]);

        var i20 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i20], B[i20], C[i20]);

        var i21 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i21], B[i21], C[i21]);

        var i22 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i22], B[i22], C[i22]);

        var i23 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i23], B[i23], C[i23]);

        var i24 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i24], B[i24], C[i24]);

        var i25 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i25], B[i25], C[i25]);

        var i26 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i26], B[i26], C[i26]);

        var i27 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i27], B[i27], C[i27]);

        var i28 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i28], B[i28], C[i28]);

        var i29 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i29], B[i29], C[i29]);

        var i30 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i30], B[i30], C[i30]);

        var i31 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i31], B[i31], C[i31]);

        var i32 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i32], B[i32], C[i32]);

        var i33 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i33], B[i33], C[i33]);

        var i34 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i34], B[i34], C[i34]);

        var i35 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i35], B[i35], C[i35]);

        var i36 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i36], B[i36], C[i36]);

        var i37 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i37], B[i37], C[i37]);

        var i38 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i38], B[i38], C[i38]);

        var i39 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i39], B[i39], C[i39]);

        var i40 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i40], B[i40], C[i40]);

        var i41 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i41], B[i41], C[i41]);

        var i42 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i42], B[i42], C[i42]);

        var i43 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i43], B[i43], C[i43]);

        var i44 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i44], B[i44], C[i44]);

        var i45 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i45], B[i45], C[i45]);

        var i46 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i46], B[i46], C[i46]);

        var i47 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i47], B[i47], C[i47]);

        var i48 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i48], B[i48], C[i48]);

        var i49 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i49], B[i49], C[i49]);

        var i50 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i50], B[i50], C[i50]);

        var i51 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i51], B[i51], C[i51]);

        var i52 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i52], B[i52], C[i52]);

        var i53 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i53], B[i53], C[i53]);

        var i54 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i54], B[i54], C[i54]);

        var i55 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i55], B[i55], C[i55]);

        var i56 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i56], B[i56], C[i56]);

        var i57 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i57], B[i57], C[i57]);

        var i58 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i58], B[i58], C[i58]);

        var i59 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i59], B[i59], C[i59]);

        var i60 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i60], B[i60], C[i60]);

        var i61 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i61], B[i61], C[i61]);

        var i62 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i62], B[i62], C[i62]);

        var i63 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i63], B[i63], C[i63]);

        var i64 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i64], B[i64], C[i64]);

        var i65 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i65], B[i65], C[i65]);

        var i66 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i66], B[i66], C[i66]);

        var i67 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i67], B[i67], C[i67]);

        var i68 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i68], B[i68], C[i68]);

        var i69 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i69], B[i69], C[i69]);

        var i70 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i70], B[i70], C[i70]);

        var i71 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i71], B[i71], C[i71]);

        var i72 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i72], B[i72], C[i72]);

        var i73 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i73], B[i73], C[i73]);

        var i74 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i74], B[i74], C[i74]);

        var i75 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i75], B[i75], C[i75]);

        var i76 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i76], B[i76], C[i76]);

        var i77 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i77], B[i77], C[i77]);

        var i78 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i78], B[i78], C[i78]);

        var i79 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i79], B[i79], C[i79]);

        var i80 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i80], B[i80], C[i80]);

        var i81 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i81], B[i81], C[i81]);

        var i82 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i82], B[i82], C[i82]);

        var i83 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i83], B[i83], C[i83]);

        var i84 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i84], B[i84], C[i84]);

        var i85 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i85], B[i85], C[i85]);

        var i86 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i86], B[i86], C[i86]);

        var i87 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i87], B[i87], C[i87]);

        var i88 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i88], B[i88], C[i88]);

        var i89 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i89], B[i89], C[i89]);

        var i90 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i90], B[i90], C[i90]);

        var i91 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i91], B[i91], C[i91]);

        var i92 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i92], B[i92], C[i92]);

        var i93 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i93], B[i93], C[i93]);

        var i94 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i94], B[i94], C[i94]);

        var i95 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i95], B[i95], C[i95]);

        var i96 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i96], B[i96], C[i96]);

        var i97 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i97], B[i97], C[i97]);

        var i98 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i98], B[i98], C[i98]);

        var i99 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i99], B[i99], C[i99]);

        var i100 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i100], B[i100], C[i100]);

        var i101 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i101], B[i101], C[i101]);

        var i102 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i102], B[i102], C[i102]);

        var i103 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i103], B[i103], C[i103]);

        var i104 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i104], B[i104], C[i104]);

        var i105 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i105], B[i105], C[i105]);

        var i106 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i106], B[i106], C[i106]);

        var i107 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i107], B[i107], C[i107]);

        var i108 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i108], B[i108], C[i108]);

        var i109 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i109], B[i109], C[i109]);

        var i110 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i110], B[i110], C[i110]);

        var i111 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i111], B[i111], C[i111]);

        var i112 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i112], B[i112], C[i112]);

        var i113 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i113], B[i113], C[i113]);

        var i114 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i114], B[i114], C[i114]);

        var i115 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i115], B[i115], C[i115]);

        var i116 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i116], B[i116], C[i116]);

        var i117 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i117], B[i117], C[i117]);

        var i118 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i118], B[i118], C[i118]);

        var i119 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i119], B[i119], C[i119]);

        var i120 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i120], B[i120], C[i120]);

        var i121 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i121], B[i121], C[i121]);

        var i122 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i122], B[i122], C[i122]);

        var i123 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i123], B[i123], C[i123]);

        var i124 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i124], B[i124], C[i124]);

        var i125 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i125], B[i125], C[i125]);

        var i126 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i126], B[i126], C[i126]);

        var i127 = NextIndex();
        sum += _dynamicExpressoDelegate(A[i127], B[i127], C[i127]);

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
        sum += _nCalcLambda(_nCalcContext);

        var i1 = NextIndex();
        _nCalcContext.A = A[i1];
        _nCalcContext.B = B[i1];
        _nCalcContext.C = C[i1];
        sum += _nCalcLambda(_nCalcContext);

        var i2 = NextIndex();
        _nCalcContext.A = A[i2];
        _nCalcContext.B = B[i2];
        _nCalcContext.C = C[i2];
        sum += _nCalcLambda(_nCalcContext);

        var i3 = NextIndex();
        _nCalcContext.A = A[i3];
        _nCalcContext.B = B[i3];
        _nCalcContext.C = C[i3];
        sum += _nCalcLambda(_nCalcContext);

        var i4 = NextIndex();
        _nCalcContext.A = A[i4];
        _nCalcContext.B = B[i4];
        _nCalcContext.C = C[i4];
        sum += _nCalcLambda(_nCalcContext);

        var i5 = NextIndex();
        _nCalcContext.A = A[i5];
        _nCalcContext.B = B[i5];
        _nCalcContext.C = C[i5];
        sum += _nCalcLambda(_nCalcContext);

        var i6 = NextIndex();
        _nCalcContext.A = A[i6];
        _nCalcContext.B = B[i6];
        _nCalcContext.C = C[i6];
        sum += _nCalcLambda(_nCalcContext);

        var i7 = NextIndex();
        _nCalcContext.A = A[i7];
        _nCalcContext.B = B[i7];
        _nCalcContext.C = C[i7];
        sum += _nCalcLambda(_nCalcContext);

        var i8 = NextIndex();
        _nCalcContext.A = A[i8];
        _nCalcContext.B = B[i8];
        _nCalcContext.C = C[i8];
        sum += _nCalcLambda(_nCalcContext);

        var i9 = NextIndex();
        _nCalcContext.A = A[i9];
        _nCalcContext.B = B[i9];
        _nCalcContext.C = C[i9];
        sum += _nCalcLambda(_nCalcContext);

        var i10 = NextIndex();
        _nCalcContext.A = A[i10];
        _nCalcContext.B = B[i10];
        _nCalcContext.C = C[i10];
        sum += _nCalcLambda(_nCalcContext);

        var i11 = NextIndex();
        _nCalcContext.A = A[i11];
        _nCalcContext.B = B[i11];
        _nCalcContext.C = C[i11];
        sum += _nCalcLambda(_nCalcContext);

        var i12 = NextIndex();
        _nCalcContext.A = A[i12];
        _nCalcContext.B = B[i12];
        _nCalcContext.C = C[i12];
        sum += _nCalcLambda(_nCalcContext);

        var i13 = NextIndex();
        _nCalcContext.A = A[i13];
        _nCalcContext.B = B[i13];
        _nCalcContext.C = C[i13];
        sum += _nCalcLambda(_nCalcContext);

        var i14 = NextIndex();
        _nCalcContext.A = A[i14];
        _nCalcContext.B = B[i14];
        _nCalcContext.C = C[i14];
        sum += _nCalcLambda(_nCalcContext);

        var i15 = NextIndex();
        _nCalcContext.A = A[i15];
        _nCalcContext.B = B[i15];
        _nCalcContext.C = C[i15];
        sum += _nCalcLambda(_nCalcContext);

        var i16 = NextIndex();
        _nCalcContext.A = A[i16];
        _nCalcContext.B = B[i16];
        _nCalcContext.C = C[i16];
        sum += _nCalcLambda(_nCalcContext);

        var i17 = NextIndex();
        _nCalcContext.A = A[i17];
        _nCalcContext.B = B[i17];
        _nCalcContext.C = C[i17];
        sum += _nCalcLambda(_nCalcContext);

        var i18 = NextIndex();
        _nCalcContext.A = A[i18];
        _nCalcContext.B = B[i18];
        _nCalcContext.C = C[i18];
        sum += _nCalcLambda(_nCalcContext);

        var i19 = NextIndex();
        _nCalcContext.A = A[i19];
        _nCalcContext.B = B[i19];
        _nCalcContext.C = C[i19];
        sum += _nCalcLambda(_nCalcContext);

        var i20 = NextIndex();
        _nCalcContext.A = A[i20];
        _nCalcContext.B = B[i20];
        _nCalcContext.C = C[i20];
        sum += _nCalcLambda(_nCalcContext);

        var i21 = NextIndex();
        _nCalcContext.A = A[i21];
        _nCalcContext.B = B[i21];
        _nCalcContext.C = C[i21];
        sum += _nCalcLambda(_nCalcContext);

        var i22 = NextIndex();
        _nCalcContext.A = A[i22];
        _nCalcContext.B = B[i22];
        _nCalcContext.C = C[i22];
        sum += _nCalcLambda(_nCalcContext);

        var i23 = NextIndex();
        _nCalcContext.A = A[i23];
        _nCalcContext.B = B[i23];
        _nCalcContext.C = C[i23];
        sum += _nCalcLambda(_nCalcContext);

        var i24 = NextIndex();
        _nCalcContext.A = A[i24];
        _nCalcContext.B = B[i24];
        _nCalcContext.C = C[i24];
        sum += _nCalcLambda(_nCalcContext);

        var i25 = NextIndex();
        _nCalcContext.A = A[i25];
        _nCalcContext.B = B[i25];
        _nCalcContext.C = C[i25];
        sum += _nCalcLambda(_nCalcContext);

        var i26 = NextIndex();
        _nCalcContext.A = A[i26];
        _nCalcContext.B = B[i26];
        _nCalcContext.C = C[i26];
        sum += _nCalcLambda(_nCalcContext);

        var i27 = NextIndex();
        _nCalcContext.A = A[i27];
        _nCalcContext.B = B[i27];
        _nCalcContext.C = C[i27];
        sum += _nCalcLambda(_nCalcContext);

        var i28 = NextIndex();
        _nCalcContext.A = A[i28];
        _nCalcContext.B = B[i28];
        _nCalcContext.C = C[i28];
        sum += _nCalcLambda(_nCalcContext);

        var i29 = NextIndex();
        _nCalcContext.A = A[i29];
        _nCalcContext.B = B[i29];
        _nCalcContext.C = C[i29];
        sum += _nCalcLambda(_nCalcContext);

        var i30 = NextIndex();
        _nCalcContext.A = A[i30];
        _nCalcContext.B = B[i30];
        _nCalcContext.C = C[i30];
        sum += _nCalcLambda(_nCalcContext);

        var i31 = NextIndex();
        _nCalcContext.A = A[i31];
        _nCalcContext.B = B[i31];
        _nCalcContext.C = C[i31];
        sum += _nCalcLambda(_nCalcContext);

        var i32 = NextIndex();
        _nCalcContext.A = A[i32];
        _nCalcContext.B = B[i32];
        _nCalcContext.C = C[i32];
        sum += _nCalcLambda(_nCalcContext);

        var i33 = NextIndex();
        _nCalcContext.A = A[i33];
        _nCalcContext.B = B[i33];
        _nCalcContext.C = C[i33];
        sum += _nCalcLambda(_nCalcContext);

        var i34 = NextIndex();
        _nCalcContext.A = A[i34];
        _nCalcContext.B = B[i34];
        _nCalcContext.C = C[i34];
        sum += _nCalcLambda(_nCalcContext);

        var i35 = NextIndex();
        _nCalcContext.A = A[i35];
        _nCalcContext.B = B[i35];
        _nCalcContext.C = C[i35];
        sum += _nCalcLambda(_nCalcContext);

        var i36 = NextIndex();
        _nCalcContext.A = A[i36];
        _nCalcContext.B = B[i36];
        _nCalcContext.C = C[i36];
        sum += _nCalcLambda(_nCalcContext);

        var i37 = NextIndex();
        _nCalcContext.A = A[i37];
        _nCalcContext.B = B[i37];
        _nCalcContext.C = C[i37];
        sum += _nCalcLambda(_nCalcContext);

        var i38 = NextIndex();
        _nCalcContext.A = A[i38];
        _nCalcContext.B = B[i38];
        _nCalcContext.C = C[i38];
        sum += _nCalcLambda(_nCalcContext);

        var i39 = NextIndex();
        _nCalcContext.A = A[i39];
        _nCalcContext.B = B[i39];
        _nCalcContext.C = C[i39];
        sum += _nCalcLambda(_nCalcContext);

        var i40 = NextIndex();
        _nCalcContext.A = A[i40];
        _nCalcContext.B = B[i40];
        _nCalcContext.C = C[i40];
        sum += _nCalcLambda(_nCalcContext);

        var i41 = NextIndex();
        _nCalcContext.A = A[i41];
        _nCalcContext.B = B[i41];
        _nCalcContext.C = C[i41];
        sum += _nCalcLambda(_nCalcContext);

        var i42 = NextIndex();
        _nCalcContext.A = A[i42];
        _nCalcContext.B = B[i42];
        _nCalcContext.C = C[i42];
        sum += _nCalcLambda(_nCalcContext);

        var i43 = NextIndex();
        _nCalcContext.A = A[i43];
        _nCalcContext.B = B[i43];
        _nCalcContext.C = C[i43];
        sum += _nCalcLambda(_nCalcContext);

        var i44 = NextIndex();
        _nCalcContext.A = A[i44];
        _nCalcContext.B = B[i44];
        _nCalcContext.C = C[i44];
        sum += _nCalcLambda(_nCalcContext);

        var i45 = NextIndex();
        _nCalcContext.A = A[i45];
        _nCalcContext.B = B[i45];
        _nCalcContext.C = C[i45];
        sum += _nCalcLambda(_nCalcContext);

        var i46 = NextIndex();
        _nCalcContext.A = A[i46];
        _nCalcContext.B = B[i46];
        _nCalcContext.C = C[i46];
        sum += _nCalcLambda(_nCalcContext);

        var i47 = NextIndex();
        _nCalcContext.A = A[i47];
        _nCalcContext.B = B[i47];
        _nCalcContext.C = C[i47];
        sum += _nCalcLambda(_nCalcContext);

        var i48 = NextIndex();
        _nCalcContext.A = A[i48];
        _nCalcContext.B = B[i48];
        _nCalcContext.C = C[i48];
        sum += _nCalcLambda(_nCalcContext);

        var i49 = NextIndex();
        _nCalcContext.A = A[i49];
        _nCalcContext.B = B[i49];
        _nCalcContext.C = C[i49];
        sum += _nCalcLambda(_nCalcContext);

        var i50 = NextIndex();
        _nCalcContext.A = A[i50];
        _nCalcContext.B = B[i50];
        _nCalcContext.C = C[i50];
        sum += _nCalcLambda(_nCalcContext);

        var i51 = NextIndex();
        _nCalcContext.A = A[i51];
        _nCalcContext.B = B[i51];
        _nCalcContext.C = C[i51];
        sum += _nCalcLambda(_nCalcContext);

        var i52 = NextIndex();
        _nCalcContext.A = A[i52];
        _nCalcContext.B = B[i52];
        _nCalcContext.C = C[i52];
        sum += _nCalcLambda(_nCalcContext);

        var i53 = NextIndex();
        _nCalcContext.A = A[i53];
        _nCalcContext.B = B[i53];
        _nCalcContext.C = C[i53];
        sum += _nCalcLambda(_nCalcContext);

        var i54 = NextIndex();
        _nCalcContext.A = A[i54];
        _nCalcContext.B = B[i54];
        _nCalcContext.C = C[i54];
        sum += _nCalcLambda(_nCalcContext);

        var i55 = NextIndex();
        _nCalcContext.A = A[i55];
        _nCalcContext.B = B[i55];
        _nCalcContext.C = C[i55];
        sum += _nCalcLambda(_nCalcContext);

        var i56 = NextIndex();
        _nCalcContext.A = A[i56];
        _nCalcContext.B = B[i56];
        _nCalcContext.C = C[i56];
        sum += _nCalcLambda(_nCalcContext);

        var i57 = NextIndex();
        _nCalcContext.A = A[i57];
        _nCalcContext.B = B[i57];
        _nCalcContext.C = C[i57];
        sum += _nCalcLambda(_nCalcContext);

        var i58 = NextIndex();
        _nCalcContext.A = A[i58];
        _nCalcContext.B = B[i58];
        _nCalcContext.C = C[i58];
        sum += _nCalcLambda(_nCalcContext);

        var i59 = NextIndex();
        _nCalcContext.A = A[i59];
        _nCalcContext.B = B[i59];
        _nCalcContext.C = C[i59];
        sum += _nCalcLambda(_nCalcContext);

        var i60 = NextIndex();
        _nCalcContext.A = A[i60];
        _nCalcContext.B = B[i60];
        _nCalcContext.C = C[i60];
        sum += _nCalcLambda(_nCalcContext);

        var i61 = NextIndex();
        _nCalcContext.A = A[i61];
        _nCalcContext.B = B[i61];
        _nCalcContext.C = C[i61];
        sum += _nCalcLambda(_nCalcContext);

        var i62 = NextIndex();
        _nCalcContext.A = A[i62];
        _nCalcContext.B = B[i62];
        _nCalcContext.C = C[i62];
        sum += _nCalcLambda(_nCalcContext);

        var i63 = NextIndex();
        _nCalcContext.A = A[i63];
        _nCalcContext.B = B[i63];
        _nCalcContext.C = C[i63];
        sum += _nCalcLambda(_nCalcContext);

        var i64 = NextIndex();
        _nCalcContext.A = A[i64];
        _nCalcContext.B = B[i64];
        _nCalcContext.C = C[i64];
        sum += _nCalcLambda(_nCalcContext);

        var i65 = NextIndex();
        _nCalcContext.A = A[i65];
        _nCalcContext.B = B[i65];
        _nCalcContext.C = C[i65];
        sum += _nCalcLambda(_nCalcContext);

        var i66 = NextIndex();
        _nCalcContext.A = A[i66];
        _nCalcContext.B = B[i66];
        _nCalcContext.C = C[i66];
        sum += _nCalcLambda(_nCalcContext);

        var i67 = NextIndex();
        _nCalcContext.A = A[i67];
        _nCalcContext.B = B[i67];
        _nCalcContext.C = C[i67];
        sum += _nCalcLambda(_nCalcContext);

        var i68 = NextIndex();
        _nCalcContext.A = A[i68];
        _nCalcContext.B = B[i68];
        _nCalcContext.C = C[i68];
        sum += _nCalcLambda(_nCalcContext);

        var i69 = NextIndex();
        _nCalcContext.A = A[i69];
        _nCalcContext.B = B[i69];
        _nCalcContext.C = C[i69];
        sum += _nCalcLambda(_nCalcContext);

        var i70 = NextIndex();
        _nCalcContext.A = A[i70];
        _nCalcContext.B = B[i70];
        _nCalcContext.C = C[i70];
        sum += _nCalcLambda(_nCalcContext);

        var i71 = NextIndex();
        _nCalcContext.A = A[i71];
        _nCalcContext.B = B[i71];
        _nCalcContext.C = C[i71];
        sum += _nCalcLambda(_nCalcContext);

        var i72 = NextIndex();
        _nCalcContext.A = A[i72];
        _nCalcContext.B = B[i72];
        _nCalcContext.C = C[i72];
        sum += _nCalcLambda(_nCalcContext);

        var i73 = NextIndex();
        _nCalcContext.A = A[i73];
        _nCalcContext.B = B[i73];
        _nCalcContext.C = C[i73];
        sum += _nCalcLambda(_nCalcContext);

        var i74 = NextIndex();
        _nCalcContext.A = A[i74];
        _nCalcContext.B = B[i74];
        _nCalcContext.C = C[i74];
        sum += _nCalcLambda(_nCalcContext);

        var i75 = NextIndex();
        _nCalcContext.A = A[i75];
        _nCalcContext.B = B[i75];
        _nCalcContext.C = C[i75];
        sum += _nCalcLambda(_nCalcContext);

        var i76 = NextIndex();
        _nCalcContext.A = A[i76];
        _nCalcContext.B = B[i76];
        _nCalcContext.C = C[i76];
        sum += _nCalcLambda(_nCalcContext);

        var i77 = NextIndex();
        _nCalcContext.A = A[i77];
        _nCalcContext.B = B[i77];
        _nCalcContext.C = C[i77];
        sum += _nCalcLambda(_nCalcContext);

        var i78 = NextIndex();
        _nCalcContext.A = A[i78];
        _nCalcContext.B = B[i78];
        _nCalcContext.C = C[i78];
        sum += _nCalcLambda(_nCalcContext);

        var i79 = NextIndex();
        _nCalcContext.A = A[i79];
        _nCalcContext.B = B[i79];
        _nCalcContext.C = C[i79];
        sum += _nCalcLambda(_nCalcContext);

        var i80 = NextIndex();
        _nCalcContext.A = A[i80];
        _nCalcContext.B = B[i80];
        _nCalcContext.C = C[i80];
        sum += _nCalcLambda(_nCalcContext);

        var i81 = NextIndex();
        _nCalcContext.A = A[i81];
        _nCalcContext.B = B[i81];
        _nCalcContext.C = C[i81];
        sum += _nCalcLambda(_nCalcContext);

        var i82 = NextIndex();
        _nCalcContext.A = A[i82];
        _nCalcContext.B = B[i82];
        _nCalcContext.C = C[i82];
        sum += _nCalcLambda(_nCalcContext);

        var i83 = NextIndex();
        _nCalcContext.A = A[i83];
        _nCalcContext.B = B[i83];
        _nCalcContext.C = C[i83];
        sum += _nCalcLambda(_nCalcContext);

        var i84 = NextIndex();
        _nCalcContext.A = A[i84];
        _nCalcContext.B = B[i84];
        _nCalcContext.C = C[i84];
        sum += _nCalcLambda(_nCalcContext);

        var i85 = NextIndex();
        _nCalcContext.A = A[i85];
        _nCalcContext.B = B[i85];
        _nCalcContext.C = C[i85];
        sum += _nCalcLambda(_nCalcContext);

        var i86 = NextIndex();
        _nCalcContext.A = A[i86];
        _nCalcContext.B = B[i86];
        _nCalcContext.C = C[i86];
        sum += _nCalcLambda(_nCalcContext);

        var i87 = NextIndex();
        _nCalcContext.A = A[i87];
        _nCalcContext.B = B[i87];
        _nCalcContext.C = C[i87];
        sum += _nCalcLambda(_nCalcContext);

        var i88 = NextIndex();
        _nCalcContext.A = A[i88];
        _nCalcContext.B = B[i88];
        _nCalcContext.C = C[i88];
        sum += _nCalcLambda(_nCalcContext);

        var i89 = NextIndex();
        _nCalcContext.A = A[i89];
        _nCalcContext.B = B[i89];
        _nCalcContext.C = C[i89];
        sum += _nCalcLambda(_nCalcContext);

        var i90 = NextIndex();
        _nCalcContext.A = A[i90];
        _nCalcContext.B = B[i90];
        _nCalcContext.C = C[i90];
        sum += _nCalcLambda(_nCalcContext);

        var i91 = NextIndex();
        _nCalcContext.A = A[i91];
        _nCalcContext.B = B[i91];
        _nCalcContext.C = C[i91];
        sum += _nCalcLambda(_nCalcContext);

        var i92 = NextIndex();
        _nCalcContext.A = A[i92];
        _nCalcContext.B = B[i92];
        _nCalcContext.C = C[i92];
        sum += _nCalcLambda(_nCalcContext);

        var i93 = NextIndex();
        _nCalcContext.A = A[i93];
        _nCalcContext.B = B[i93];
        _nCalcContext.C = C[i93];
        sum += _nCalcLambda(_nCalcContext);

        var i94 = NextIndex();
        _nCalcContext.A = A[i94];
        _nCalcContext.B = B[i94];
        _nCalcContext.C = C[i94];
        sum += _nCalcLambda(_nCalcContext);

        var i95 = NextIndex();
        _nCalcContext.A = A[i95];
        _nCalcContext.B = B[i95];
        _nCalcContext.C = C[i95];
        sum += _nCalcLambda(_nCalcContext);

        var i96 = NextIndex();
        _nCalcContext.A = A[i96];
        _nCalcContext.B = B[i96];
        _nCalcContext.C = C[i96];
        sum += _nCalcLambda(_nCalcContext);

        var i97 = NextIndex();
        _nCalcContext.A = A[i97];
        _nCalcContext.B = B[i97];
        _nCalcContext.C = C[i97];
        sum += _nCalcLambda(_nCalcContext);

        var i98 = NextIndex();
        _nCalcContext.A = A[i98];
        _nCalcContext.B = B[i98];
        _nCalcContext.C = C[i98];
        sum += _nCalcLambda(_nCalcContext);

        var i99 = NextIndex();
        _nCalcContext.A = A[i99];
        _nCalcContext.B = B[i99];
        _nCalcContext.C = C[i99];
        sum += _nCalcLambda(_nCalcContext);

        var i100 = NextIndex();
        _nCalcContext.A = A[i100];
        _nCalcContext.B = B[i100];
        _nCalcContext.C = C[i100];
        sum += _nCalcLambda(_nCalcContext);

        var i101 = NextIndex();
        _nCalcContext.A = A[i101];
        _nCalcContext.B = B[i101];
        _nCalcContext.C = C[i101];
        sum += _nCalcLambda(_nCalcContext);

        var i102 = NextIndex();
        _nCalcContext.A = A[i102];
        _nCalcContext.B = B[i102];
        _nCalcContext.C = C[i102];
        sum += _nCalcLambda(_nCalcContext);

        var i103 = NextIndex();
        _nCalcContext.A = A[i103];
        _nCalcContext.B = B[i103];
        _nCalcContext.C = C[i103];
        sum += _nCalcLambda(_nCalcContext);

        var i104 = NextIndex();
        _nCalcContext.A = A[i104];
        _nCalcContext.B = B[i104];
        _nCalcContext.C = C[i104];
        sum += _nCalcLambda(_nCalcContext);

        var i105 = NextIndex();
        _nCalcContext.A = A[i105];
        _nCalcContext.B = B[i105];
        _nCalcContext.C = C[i105];
        sum += _nCalcLambda(_nCalcContext);

        var i106 = NextIndex();
        _nCalcContext.A = A[i106];
        _nCalcContext.B = B[i106];
        _nCalcContext.C = C[i106];
        sum += _nCalcLambda(_nCalcContext);

        var i107 = NextIndex();
        _nCalcContext.A = A[i107];
        _nCalcContext.B = B[i107];
        _nCalcContext.C = C[i107];
        sum += _nCalcLambda(_nCalcContext);

        var i108 = NextIndex();
        _nCalcContext.A = A[i108];
        _nCalcContext.B = B[i108];
        _nCalcContext.C = C[i108];
        sum += _nCalcLambda(_nCalcContext);

        var i109 = NextIndex();
        _nCalcContext.A = A[i109];
        _nCalcContext.B = B[i109];
        _nCalcContext.C = C[i109];
        sum += _nCalcLambda(_nCalcContext);

        var i110 = NextIndex();
        _nCalcContext.A = A[i110];
        _nCalcContext.B = B[i110];
        _nCalcContext.C = C[i110];
        sum += _nCalcLambda(_nCalcContext);

        var i111 = NextIndex();
        _nCalcContext.A = A[i111];
        _nCalcContext.B = B[i111];
        _nCalcContext.C = C[i111];
        sum += _nCalcLambda(_nCalcContext);

        var i112 = NextIndex();
        _nCalcContext.A = A[i112];
        _nCalcContext.B = B[i112];
        _nCalcContext.C = C[i112];
        sum += _nCalcLambda(_nCalcContext);

        var i113 = NextIndex();
        _nCalcContext.A = A[i113];
        _nCalcContext.B = B[i113];
        _nCalcContext.C = C[i113];
        sum += _nCalcLambda(_nCalcContext);

        var i114 = NextIndex();
        _nCalcContext.A = A[i114];
        _nCalcContext.B = B[i114];
        _nCalcContext.C = C[i114];
        sum += _nCalcLambda(_nCalcContext);

        var i115 = NextIndex();
        _nCalcContext.A = A[i115];
        _nCalcContext.B = B[i115];
        _nCalcContext.C = C[i115];
        sum += _nCalcLambda(_nCalcContext);

        var i116 = NextIndex();
        _nCalcContext.A = A[i116];
        _nCalcContext.B = B[i116];
        _nCalcContext.C = C[i116];
        sum += _nCalcLambda(_nCalcContext);

        var i117 = NextIndex();
        _nCalcContext.A = A[i117];
        _nCalcContext.B = B[i117];
        _nCalcContext.C = C[i117];
        sum += _nCalcLambda(_nCalcContext);

        var i118 = NextIndex();
        _nCalcContext.A = A[i118];
        _nCalcContext.B = B[i118];
        _nCalcContext.C = C[i118];
        sum += _nCalcLambda(_nCalcContext);

        var i119 = NextIndex();
        _nCalcContext.A = A[i119];
        _nCalcContext.B = B[i119];
        _nCalcContext.C = C[i119];
        sum += _nCalcLambda(_nCalcContext);

        var i120 = NextIndex();
        _nCalcContext.A = A[i120];
        _nCalcContext.B = B[i120];
        _nCalcContext.C = C[i120];
        sum += _nCalcLambda(_nCalcContext);

        var i121 = NextIndex();
        _nCalcContext.A = A[i121];
        _nCalcContext.B = B[i121];
        _nCalcContext.C = C[i121];
        sum += _nCalcLambda(_nCalcContext);

        var i122 = NextIndex();
        _nCalcContext.A = A[i122];
        _nCalcContext.B = B[i122];
        _nCalcContext.C = C[i122];
        sum += _nCalcLambda(_nCalcContext);

        var i123 = NextIndex();
        _nCalcContext.A = A[i123];
        _nCalcContext.B = B[i123];
        _nCalcContext.C = C[i123];
        sum += _nCalcLambda(_nCalcContext);

        var i124 = NextIndex();
        _nCalcContext.A = A[i124];
        _nCalcContext.B = B[i124];
        _nCalcContext.C = C[i124];
        sum += _nCalcLambda(_nCalcContext);

        var i125 = NextIndex();
        _nCalcContext.A = A[i125];
        _nCalcContext.B = B[i125];
        _nCalcContext.C = C[i125];
        sum += _nCalcLambda(_nCalcContext);

        var i126 = NextIndex();
        _nCalcContext.A = A[i126];
        _nCalcContext.B = B[i126];
        _nCalcContext.C = C[i126];
        sum += _nCalcLambda(_nCalcContext);

        var i127 = NextIndex();
        _nCalcContext.A = A[i127];
        _nCalcContext.B = B[i127];
        _nCalcContext.C = C[i127];
        sum += _nCalcLambda(_nCalcContext);

        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public double Wist_Cil_FastInvoker_Unrolled128()
    {
        var sum = 0.0;

        var i0 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i0], B[i0], C[i0]);

        var i1 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i1], B[i1], C[i1]);

        var i2 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i2], B[i2], C[i2]);

        var i3 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i3], B[i3], C[i3]);

        var i4 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i4], B[i4], C[i4]);

        var i5 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i5], B[i5], C[i5]);

        var i6 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i6], B[i6], C[i6]);

        var i7 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i7], B[i7], C[i7]);

        var i8 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i8], B[i8], C[i8]);

        var i9 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i9], B[i9], C[i9]);

        var i10 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i10], B[i10], C[i10]);

        var i11 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i11], B[i11], C[i11]);

        var i12 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i12], B[i12], C[i12]);

        var i13 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i13], B[i13], C[i13]);

        var i14 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i14], B[i14], C[i14]);

        var i15 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i15], B[i15], C[i15]);

        var i16 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i16], B[i16], C[i16]);

        var i17 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i17], B[i17], C[i17]);

        var i18 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i18], B[i18], C[i18]);

        var i19 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i19], B[i19], C[i19]);

        var i20 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i20], B[i20], C[i20]);

        var i21 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i21], B[i21], C[i21]);

        var i22 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i22], B[i22], C[i22]);

        var i23 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i23], B[i23], C[i23]);

        var i24 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i24], B[i24], C[i24]);

        var i25 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i25], B[i25], C[i25]);

        var i26 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i26], B[i26], C[i26]);

        var i27 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i27], B[i27], C[i27]);

        var i28 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i28], B[i28], C[i28]);

        var i29 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i29], B[i29], C[i29]);

        var i30 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i30], B[i30], C[i30]);

        var i31 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i31], B[i31], C[i31]);

        var i32 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i32], B[i32], C[i32]);

        var i33 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i33], B[i33], C[i33]);

        var i34 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i34], B[i34], C[i34]);

        var i35 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i35], B[i35], C[i35]);

        var i36 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i36], B[i36], C[i36]);

        var i37 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i37], B[i37], C[i37]);

        var i38 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i38], B[i38], C[i38]);

        var i39 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i39], B[i39], C[i39]);

        var i40 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i40], B[i40], C[i40]);

        var i41 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i41], B[i41], C[i41]);

        var i42 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i42], B[i42], C[i42]);

        var i43 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i43], B[i43], C[i43]);

        var i44 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i44], B[i44], C[i44]);

        var i45 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i45], B[i45], C[i45]);

        var i46 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i46], B[i46], C[i46]);

        var i47 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i47], B[i47], C[i47]);

        var i48 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i48], B[i48], C[i48]);

        var i49 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i49], B[i49], C[i49]);

        var i50 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i50], B[i50], C[i50]);

        var i51 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i51], B[i51], C[i51]);

        var i52 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i52], B[i52], C[i52]);

        var i53 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i53], B[i53], C[i53]);

        var i54 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i54], B[i54], C[i54]);

        var i55 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i55], B[i55], C[i55]);

        var i56 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i56], B[i56], C[i56]);

        var i57 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i57], B[i57], C[i57]);

        var i58 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i58], B[i58], C[i58]);

        var i59 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i59], B[i59], C[i59]);

        var i60 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i60], B[i60], C[i60]);

        var i61 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i61], B[i61], C[i61]);

        var i62 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i62], B[i62], C[i62]);

        var i63 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i63], B[i63], C[i63]);

        var i64 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i64], B[i64], C[i64]);

        var i65 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i65], B[i65], C[i65]);

        var i66 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i66], B[i66], C[i66]);

        var i67 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i67], B[i67], C[i67]);

        var i68 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i68], B[i68], C[i68]);

        var i69 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i69], B[i69], C[i69]);

        var i70 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i70], B[i70], C[i70]);

        var i71 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i71], B[i71], C[i71]);

        var i72 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i72], B[i72], C[i72]);

        var i73 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i73], B[i73], C[i73]);

        var i74 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i74], B[i74], C[i74]);

        var i75 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i75], B[i75], C[i75]);

        var i76 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i76], B[i76], C[i76]);

        var i77 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i77], B[i77], C[i77]);

        var i78 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i78], B[i78], C[i78]);

        var i79 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i79], B[i79], C[i79]);

        var i80 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i80], B[i80], C[i80]);

        var i81 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i81], B[i81], C[i81]);

        var i82 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i82], B[i82], C[i82]);

        var i83 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i83], B[i83], C[i83]);

        var i84 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i84], B[i84], C[i84]);

        var i85 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i85], B[i85], C[i85]);

        var i86 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i86], B[i86], C[i86]);

        var i87 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i87], B[i87], C[i87]);

        var i88 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i88], B[i88], C[i88]);

        var i89 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i89], B[i89], C[i89]);

        var i90 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i90], B[i90], C[i90]);

        var i91 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i91], B[i91], C[i91]);

        var i92 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i92], B[i92], C[i92]);

        var i93 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i93], B[i93], C[i93]);

        var i94 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i94], B[i94], C[i94]);

        var i95 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i95], B[i95], C[i95]);

        var i96 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i96], B[i96], C[i96]);

        var i97 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i97], B[i97], C[i97]);

        var i98 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i98], B[i98], C[i98]);

        var i99 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i99], B[i99], C[i99]);

        var i100 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i100], B[i100], C[i100]);

        var i101 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i101], B[i101], C[i101]);

        var i102 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i102], B[i102], C[i102]);

        var i103 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i103], B[i103], C[i103]);

        var i104 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i104], B[i104], C[i104]);

        var i105 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i105], B[i105], C[i105]);

        var i106 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i106], B[i106], C[i106]);

        var i107 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i107], B[i107], C[i107]);

        var i108 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i108], B[i108], C[i108]);

        var i109 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i109], B[i109], C[i109]);

        var i110 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i110], B[i110], C[i110]);

        var i111 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i111], B[i111], C[i111]);

        var i112 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i112], B[i112], C[i112]);

        var i113 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i113], B[i113], C[i113]);

        var i114 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i114], B[i114], C[i114]);

        var i115 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i115], B[i115], C[i115]);

        var i116 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i116], B[i116], C[i116]);

        var i117 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i117], B[i117], C[i117]);

        var i118 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i118], B[i118], C[i118]);

        var i119 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i119], B[i119], C[i119]);

        var i120 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i120], B[i120], C[i120]);

        var i121 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i121], B[i121], C[i121]);

        var i122 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i122], B[i122], C[i122]);

        var i123 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i123], B[i123], C[i123]);

        var i124 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i124], B[i124], C[i124]);

        var i125 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i125], B[i125], C[i125]);

        var i126 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i126], B[i126], C[i126]);

        var i127 = NextIndex();
        sum += _wistFastInvoker.Invoke(A[i127], B[i127], C[i127]);

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c)
        => a + b * c / 5.0;

    private double CSharpAt(int index)
        => CSharp_NoInliningMethodCore(A[index], B[index], C[index]);

    private double DynamicExpressoAt(int index)
        => _dynamicExpressoDelegate(A[index], B[index], C[index]);

    private double NCalcAt(int index)
    {
        _nCalcContext.A = A[index];
        _nCalcContext.B = B[index];
        _nCalcContext.C = C[index];
        return _nCalcLambda(_nCalcContext);
    }

    private double WistAt(int index)
        => _wistFastInvoker.Invoke(A[index], B[index], C[index]);
}