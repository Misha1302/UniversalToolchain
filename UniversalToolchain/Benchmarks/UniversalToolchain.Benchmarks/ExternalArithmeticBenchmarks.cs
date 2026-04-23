using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BasicCore.Compilation;
using BasicCore.Execution;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DynamicExpresso;
using DynamicMethodCalling.Core;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using NCalc;
using NCalc.LambdaCompilation;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public abstract class ExternalArithmeticBenchmarkEnvironmentBase
{
    protected const int DataSize = 4096;
    protected const int InnerCount = 1024;
    protected const int PrepareInnerCount = 16;
    private WistDialectExecutionHost? _host;
    private ServiceProvider? _provider;
    private int _index;
    protected double[] A = []; protected double[] B = []; protected double[] C = []; protected double[] D = []; protected double[] E = [];
    protected double[] F = []; protected double[] G = []; protected double[] H = []; protected double[] I = []; protected double[] J = []; protected double[] K = [];
    protected void InitializeInputData(){A=new double[DataSize];B=new double[DataSize];C=new double[DataSize];D=new double[DataSize];E=new double[DataSize];F=new double[DataSize];G=new double[DataSize];H=new double[DataSize];I=new double[DataSize];J=new double[DataSize];K=new double[DataSize];var random=new Random(42);Fill(random,A);Fill(random,B);Fill(random,C);Fill(random,D);Fill(random,E);Fill(random,F);Fill(random,G);Fill(random,H);Fill(random,I);Fill(random,J);Fill(random,K);}    
    protected void CreateProviderAndHost(){var services=new ServiceCollection();services.AddWistDialectServices();services.AddWistCilBackend();services.AddWistInterpreterBackend();_provider=services.BuildServiceProvider();var workflow=_provider.GetRequiredService<WistDialectExecutionWorkflow>();var dialectFile=new WistShippedDialectFileResolver().Resolve(WistShippedDialectPresets.FullDefaultNative);var dialect=workflow.ComposeFile(dialectFile);if(!dialect.IsSuccess)Thrower.InvalidOpEx($"Failed to compose dialect file: {DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(dialect))}");_host=workflow.CreateHost(dialect);}    
    protected ICompiledArtifact<DynamicMethod> CompileWistCil(string formula,string[] bindingNames){var host=_host??Thrower.InvalidOpEx<WistDialectExecutionHost>("Wist host must be initialized before compilation.");return host.GetArtifactCompiler<DynamicMethod>("compiler").Compile(formula,CreateDeclaredBindings(bindingNames));}
    protected ICompiledArtifact<IAbstractIR> CompileWistInterpreter(string formula,string[] bindingNames){var host=_host??Thrower.InvalidOpEx<WistDialectExecutionHost>("Wist host must be initialized before compilation.");return host.GetArtifactCompiler<IAbstractIR>("interpreter").Compile(formula,CreateDeclaredBindings(bindingNames));}
    protected OrderedDictionary<string,Type> CreateDeclaredBindings(string[] names){var d=new OrderedDictionary<string,Type>();foreach(var n in names)d[n]=typeof(double);return d;}
    protected static void ValidateParity(params Func<int,double>[] e){var idx=new[]{0,1,17,255,1023,2047,4095};foreach(var i in idx){var expected=e[0](i);for(var k=1;k<e.Length;k++){var c=e[k](i);if(!AreEqual(expected,c))Thrower.InvalidOpEx($"Parity mismatch at index {i}. Expected: {expected}, candidate: {c}, evaluator index: {k}.");}}}
    [MethodImpl(MethodImplOptions.AggressiveInlining)] protected int NextIndex(){var i=_index;_index=i+1&DataSize-1;return i;}
    [GlobalCleanup] public void GlobalCleanup(){_host?.Dispose();_provider?.Dispose();}
    private static bool AreEqual(double l,double r){const double a=1e-9;const double re=1e-12;var d=Math.Abs(l-r);var s=Math.Max(1.0,Math.Max(Math.Abs(l),Math.Abs(r)));return d<=Math.Max(a,re*s);}    
    private static void Fill(Random random,double[] values){for(var i=0;i<values.Length;i++)values[i]=0.1+random.NextDouble()*999.9;}
}

public class ExternalSimple3ExecutionBenchmarks : ExternalArithmeticBenchmarkEnvironmentBase
{
    private const string UtFormula = "A + B * C / 5.0";
    private const string NCalcFormula = "[A] + [B] * [C] / 5.0";
    private const string DynamicExpressoFormula = "A + B * C / 5.0";
    private BenchContext3 _context = null!;
    private Func<BenchContext3, double> _nCalcLambda = null!;
    private Func<double, double, double, double> _dynamicExpressoDelegate = null!;
    private DynamicMethodInvoker<double, double, double, double> _wistFastInvoker = null!;
    private ICompiledArtifactSession _wistCilSession = null!;
    private ICompiledArtifactSession _wistInterpreterSession = null!;
    [GlobalSetup]
    public void GlobalSetup(){InitializeInputData();CreateProviderAndHost();var bindingNames=new[]{"A", "B", "C"};var wistCilArtifact=CompileWistCil(UtFormula,bindingNames);_wistFastInvoker=new DynamicMethodInvoker<double, double, double, double>(wistCilArtifact.CompilationOutput);_wistCilSession=wistCilArtifact.CreateSession();var wistInterpreterArtifact=CompileWistInterpreter(UtFormula,bindingNames);_wistInterpreterSession=wistInterpreterArtifact.CreateSession();_context=new BenchContext3();_nCalcLambda=new Expression(NCalcFormula).ToLambda<BenchContext3, double>();var dynamicInterpreter=new Interpreter();_dynamicExpressoDelegate=dynamicInterpreter.ParseAsDelegate<Func<double, double, double, double>>(DynamicExpressoFormula, bindingNames);ValidateParity(CSharpAt, DynamicExpressoAt, NCalcAt, WistCompilerAt, WistFastInvokerAt, WistInterpreterAt);}
    [Benchmark(Baseline = true)] public double CSharp_NoInliningMethod(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=CSharp_NoInliningMethodCore(A[i], B[i], C[i]);}return sum;}
    [Benchmark] public double DynamicExpresso_Delegate(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_dynamicExpressoDelegate(A[i], B[i], C[i]);}return sum;}
    [Benchmark] public double NCalc_Lambda(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            sum+=_nCalcLambda(_context);}return sum;}
    [Benchmark] public double Wist_Cil_DynamicMethodInvoke(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistCilSession.SetArgument(0, A[i]);
            _wistCilSession.SetArgument(1, B[i]);
            _wistCilSession.SetArgument(2, C[i]);
            sum+=Convert.ToDouble(_wistCilSession.Run());}return sum;}
    [Benchmark] public double Wist_Cil_FastInvoker(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_wistFastInvoker.Invoke(A[i], B[i], C[i]);}return sum;}
    [Benchmark] public double Wist_Interpreter(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistInterpreterSession.SetArgument(0, A[i]);
            _wistInterpreterSession.SetArgument(1, B[i]);
            _wistInterpreterSession.SetArgument(2, C[i]);
            sum+=Convert.ToDouble(_wistInterpreterSession.Run());}return sum;}
    [MethodImpl(MethodImplOptions.NoInlining)] private static double CSharp_NoInliningMethodCore(double a, double b, double c) => a + b * c / 5.0;
    private double CSharpAt(int index)=>CSharp_NoInliningMethodCore(A[index], B[index], C[index]);
    private double DynamicExpressoAt(int index)=>_dynamicExpressoDelegate(A[index], B[index], C[index]);
    private double NCalcAt(int index){        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        return _nCalcLambda(_context);}
    private double WistCompilerAt(int index){        _wistCilSession.SetArgument(0, A[index]);
        _wistCilSession.SetArgument(1, B[index]);
        _wistCilSession.SetArgument(2, C[index]);
        return Convert.ToDouble(_wistCilSession.Run());}
    private double WistFastInvokerAt(int index)=>_wistFastInvoker.Invoke(A[index], B[index], C[index]);
    private double WistInterpreterAt(int index){        _wistInterpreterSession.SetArgument(0, A[index]);
        _wistInterpreterSession.SetArgument(1, B[index]);
        _wistInterpreterSession.SetArgument(2, C[index]);
        return Convert.ToDouble(_wistInterpreterSession.Run());}
}

public class ExternalMedium8ExecutionBenchmarks : ExternalArithmeticBenchmarkEnvironmentBase
{
    private const string UtFormula = "((A + B) * (C - D) / (E + 1.0)) + F * G - H / 3.0";
    private const string NCalcFormula = "(([A] + [B]) * ([C] - [D]) / ([E] + 1.0)) + [F] * [G] - [H] / 3.0";
    private const string DynamicExpressoFormula = "((A + B) * (C - D) / (E + 1.0)) + F * G - H / 3.0";
    private BenchContext8 _context = null!;
    private Func<BenchContext8, double> _nCalcLambda = null!;
    private Func<double, double, double, double, double, double, double, double, double> _dynamicExpressoDelegate = null!;
    private DynamicMethodInvoker<double, double, double, double, double, double, double, double, double> _wistFastInvoker = null!;
    private ICompiledArtifactSession _wistCilSession = null!;
    private ICompiledArtifactSession _wistInterpreterSession = null!;
    [GlobalSetup]
    public void GlobalSetup(){InitializeInputData();CreateProviderAndHost();var bindingNames=new[]{"A", "B", "C", "D", "E", "F", "G", "H"};var wistCilArtifact=CompileWistCil(UtFormula,bindingNames);_wistFastInvoker=new DynamicMethodInvoker<double, double, double, double, double, double, double, double, double>(wistCilArtifact.CompilationOutput);_wistCilSession=wistCilArtifact.CreateSession();var wistInterpreterArtifact=CompileWistInterpreter(UtFormula,bindingNames);_wistInterpreterSession=wistInterpreterArtifact.CreateSession();_context=new BenchContext8();_nCalcLambda=new Expression(NCalcFormula).ToLambda<BenchContext8, double>();var dynamicInterpreter=new Interpreter();_dynamicExpressoDelegate=dynamicInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double, double, double>>(DynamicExpressoFormula, bindingNames);ValidateParity(CSharpAt, DynamicExpressoAt, NCalcAt, WistCompilerAt, WistFastInvokerAt, WistInterpreterAt);}
    [Benchmark(Baseline = true)] public double CSharp_NoInliningMethod(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i]);}return sum;}
    [Benchmark] public double DynamicExpresso_Delegate(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_dynamicExpressoDelegate(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i]);}return sum;}
    [Benchmark] public double NCalc_Lambda(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            _context.D = D[i];
            _context.E = E[i];
            _context.F = F[i];
            _context.G = G[i];
            _context.H = H[i];
            sum+=_nCalcLambda(_context);}return sum;}
    [Benchmark] public double Wist_Cil_DynamicMethodInvoke(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistCilSession.SetArgument(0, A[i]);
            _wistCilSession.SetArgument(1, B[i]);
            _wistCilSession.SetArgument(2, C[i]);
            _wistCilSession.SetArgument(3, D[i]);
            _wistCilSession.SetArgument(4, E[i]);
            _wistCilSession.SetArgument(5, F[i]);
            _wistCilSession.SetArgument(6, G[i]);
            _wistCilSession.SetArgument(7, H[i]);
            sum+=Convert.ToDouble(_wistCilSession.Run());}return sum;}
    [Benchmark] public double Wist_Cil_FastInvoker(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_wistFastInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i]);}return sum;}
    [Benchmark] public double Wist_Interpreter(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistInterpreterSession.SetArgument(0, A[i]);
            _wistInterpreterSession.SetArgument(1, B[i]);
            _wistInterpreterSession.SetArgument(2, C[i]);
            _wistInterpreterSession.SetArgument(3, D[i]);
            _wistInterpreterSession.SetArgument(4, E[i]);
            _wistInterpreterSession.SetArgument(5, F[i]);
            _wistInterpreterSession.SetArgument(6, G[i]);
            _wistInterpreterSession.SetArgument(7, H[i]);
            sum+=Convert.ToDouble(_wistInterpreterSession.Run());}return sum;}
    [MethodImpl(MethodImplOptions.NoInlining)] private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f, double g, double h) => (a + b) * (c - d) / (e + 1.0) + f * g - h / 3.0;
    private double CSharpAt(int index)=>CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index]);
    private double DynamicExpressoAt(int index)=>_dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index]);
    private double NCalcAt(int index){        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        _context.D = D[index];
        _context.E = E[index];
        _context.F = F[index];
        _context.G = G[index];
        _context.H = H[index];
        return _nCalcLambda(_context);}
    private double WistCompilerAt(int index){        _wistCilSession.SetArgument(0, A[index]);
        _wistCilSession.SetArgument(1, B[index]);
        _wistCilSession.SetArgument(2, C[index]);
        _wistCilSession.SetArgument(3, D[index]);
        _wistCilSession.SetArgument(4, E[index]);
        _wistCilSession.SetArgument(5, F[index]);
        _wistCilSession.SetArgument(6, G[index]);
        _wistCilSession.SetArgument(7, H[index]);
        return Convert.ToDouble(_wistCilSession.Run());}
    private double WistFastInvokerAt(int index)=>_wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index]);
    private double WistInterpreterAt(int index){        _wistInterpreterSession.SetArgument(0, A[index]);
        _wistInterpreterSession.SetArgument(1, B[index]);
        _wistInterpreterSession.SetArgument(2, C[index]);
        _wistInterpreterSession.SetArgument(3, D[index]);
        _wistInterpreterSession.SetArgument(4, E[index]);
        _wistInterpreterSession.SetArgument(5, F[index]);
        _wistInterpreterSession.SetArgument(6, G[index]);
        _wistInterpreterSession.SetArgument(7, H[index]);
        return Convert.ToDouble(_wistInterpreterSession.Run());}
}

public class ExternalDeepChain6ExecutionBenchmarks : ExternalArithmeticBenchmarkEnvironmentBase
{
    private const string UtFormula = "((((A * 1.1 + B) * 1.2 + C) * 1.3 + D) * 1.4 + E) / (F + 1.0)";
    private const string NCalcFormula = "(((([A] * 1.1 + [B]) * 1.2 + [C]) * 1.3 + [D]) * 1.4 + [E]) / ([F] + 1.0)";
    private const string DynamicExpressoFormula = "((((A * 1.1 + B) * 1.2 + C) * 1.3 + D) * 1.4 + E) / (F + 1.0)";
    private BenchContext6 _context = null!;
    private Func<BenchContext6, double> _nCalcLambda = null!;
    private Func<double, double, double, double, double, double, double> _dynamicExpressoDelegate = null!;
    private DynamicMethodInvoker<double, double, double, double, double, double, double> _wistFastInvoker = null!;
    private ICompiledArtifactSession _wistCilSession = null!;
    private ICompiledArtifactSession _wistInterpreterSession = null!;
    [GlobalSetup]
    public void GlobalSetup(){InitializeInputData();CreateProviderAndHost();var bindingNames=new[]{"A", "B", "C", "D", "E", "F"};var wistCilArtifact=CompileWistCil(UtFormula,bindingNames);_wistFastInvoker=new DynamicMethodInvoker<double, double, double, double, double, double, double>(wistCilArtifact.CompilationOutput);_wistCilSession=wistCilArtifact.CreateSession();var wistInterpreterArtifact=CompileWistInterpreter(UtFormula,bindingNames);_wistInterpreterSession=wistInterpreterArtifact.CreateSession();_context=new BenchContext6();_nCalcLambda=new Expression(NCalcFormula).ToLambda<BenchContext6, double>();var dynamicInterpreter=new Interpreter();_dynamicExpressoDelegate=dynamicInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double>>(DynamicExpressoFormula, bindingNames);ValidateParity(CSharpAt, DynamicExpressoAt, NCalcAt, WistCompilerAt, WistFastInvokerAt, WistInterpreterAt);}
    [Benchmark(Baseline = true)] public double CSharp_NoInliningMethod(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i], F[i]);}return sum;}
    [Benchmark] public double DynamicExpresso_Delegate(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_dynamicExpressoDelegate(A[i], B[i], C[i], D[i], E[i], F[i]);}return sum;}
    [Benchmark] public double NCalc_Lambda(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            _context.D = D[i];
            _context.E = E[i];
            _context.F = F[i];
            sum+=_nCalcLambda(_context);}return sum;}
    [Benchmark] public double Wist_Cil_DynamicMethodInvoke(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistCilSession.SetArgument(0, A[i]);
            _wistCilSession.SetArgument(1, B[i]);
            _wistCilSession.SetArgument(2, C[i]);
            _wistCilSession.SetArgument(3, D[i]);
            _wistCilSession.SetArgument(4, E[i]);
            _wistCilSession.SetArgument(5, F[i]);
            sum+=Convert.ToDouble(_wistCilSession.Run());}return sum;}
    [Benchmark] public double Wist_Cil_FastInvoker(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_wistFastInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i]);}return sum;}
    [Benchmark] public double Wist_Interpreter(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistInterpreterSession.SetArgument(0, A[i]);
            _wistInterpreterSession.SetArgument(1, B[i]);
            _wistInterpreterSession.SetArgument(2, C[i]);
            _wistInterpreterSession.SetArgument(3, D[i]);
            _wistInterpreterSession.SetArgument(4, E[i]);
            _wistInterpreterSession.SetArgument(5, F[i]);
            sum+=Convert.ToDouble(_wistInterpreterSession.Run());}return sum;}
    [MethodImpl(MethodImplOptions.NoInlining)] private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f) => ((((a * 1.1 + b) * 1.2 + c) * 1.3 + d) * 1.4 + e) / (f + 1.0);
    private double CSharpAt(int index)=>CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index]);
    private double DynamicExpressoAt(int index)=>_dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index], F[index]);
    private double NCalcAt(int index){        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        _context.D = D[index];
        _context.E = E[index];
        _context.F = F[index];
        return _nCalcLambda(_context);}
    private double WistCompilerAt(int index){        _wistCilSession.SetArgument(0, A[index]);
        _wistCilSession.SetArgument(1, B[index]);
        _wistCilSession.SetArgument(2, C[index]);
        _wistCilSession.SetArgument(3, D[index]);
        _wistCilSession.SetArgument(4, E[index]);
        _wistCilSession.SetArgument(5, F[index]);
        return Convert.ToDouble(_wistCilSession.Run());}
    private double WistFastInvokerAt(int index)=>_wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index]);
    private double WistInterpreterAt(int index){        _wistInterpreterSession.SetArgument(0, A[index]);
        _wistInterpreterSession.SetArgument(1, B[index]);
        _wistInterpreterSession.SetArgument(2, C[index]);
        _wistInterpreterSession.SetArgument(3, D[index]);
        _wistInterpreterSession.SetArgument(4, E[index]);
        _wistInterpreterSession.SetArgument(5, F[index]);
        return Convert.ToDouble(_wistInterpreterSession.Run());}
}

public class ExternalRepeatedSubexpressionsExecutionBenchmarks : ExternalArithmeticBenchmarkEnvironmentBase
{
    private const string UtFormula = "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)";
    private const string NCalcFormula = "(([A] * [B]) + ([A] * [B]) + ([A] * [B]) + ([C] * [D])) / ([E] + 1.0)";
    private const string DynamicExpressoFormula = "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)";
    private BenchContext5 _context = null!;
    private Func<BenchContext5, double> _nCalcLambda = null!;
    private Func<double, double, double, double, double, double> _dynamicExpressoDelegate = null!;
    private DynamicMethodInvoker<double, double, double, double, double, double> _wistFastInvoker = null!;
    private ICompiledArtifactSession _wistCilSession = null!;
    private ICompiledArtifactSession _wistInterpreterSession = null!;
    [GlobalSetup]
    public void GlobalSetup(){InitializeInputData();CreateProviderAndHost();var bindingNames=new[]{"A", "B", "C", "D", "E"};var wistCilArtifact=CompileWistCil(UtFormula,bindingNames);_wistFastInvoker=new DynamicMethodInvoker<double, double, double, double, double, double>(wistCilArtifact.CompilationOutput);_wistCilSession=wistCilArtifact.CreateSession();var wistInterpreterArtifact=CompileWistInterpreter(UtFormula,bindingNames);_wistInterpreterSession=wistInterpreterArtifact.CreateSession();_context=new BenchContext5();_nCalcLambda=new Expression(NCalcFormula).ToLambda<BenchContext5, double>();var dynamicInterpreter=new Interpreter();_dynamicExpressoDelegate=dynamicInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double>>(DynamicExpressoFormula, bindingNames);ValidateParity(CSharpAt, DynamicExpressoAt, NCalcAt, WistCompilerAt, WistFastInvokerAt, WistInterpreterAt);}
    [Benchmark(Baseline = true)] public double CSharp_NoInliningMethod(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i]);}return sum;}
    [Benchmark] public double DynamicExpresso_Delegate(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_dynamicExpressoDelegate(A[i], B[i], C[i], D[i], E[i]);}return sum;}
    [Benchmark] public double NCalc_Lambda(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            _context.D = D[i];
            _context.E = E[i];
            sum+=_nCalcLambda(_context);}return sum;}
    [Benchmark] public double Wist_Cil_DynamicMethodInvoke(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistCilSession.SetArgument(0, A[i]);
            _wistCilSession.SetArgument(1, B[i]);
            _wistCilSession.SetArgument(2, C[i]);
            _wistCilSession.SetArgument(3, D[i]);
            _wistCilSession.SetArgument(4, E[i]);
            sum+=Convert.ToDouble(_wistCilSession.Run());}return sum;}
    [Benchmark] public double Wist_Cil_FastInvoker(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_wistFastInvoker.Invoke(A[i], B[i], C[i], D[i], E[i]);}return sum;}
    [Benchmark] public double Wist_Interpreter(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistInterpreterSession.SetArgument(0, A[i]);
            _wistInterpreterSession.SetArgument(1, B[i]);
            _wistInterpreterSession.SetArgument(2, C[i]);
            _wistInterpreterSession.SetArgument(3, D[i]);
            _wistInterpreterSession.SetArgument(4, E[i]);
            sum+=Convert.ToDouble(_wistInterpreterSession.Run());}return sum;}
    [MethodImpl(MethodImplOptions.NoInlining)] private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e) => (a * b + a * b + a * b + c * d) / (e + 1.0);
    private double CSharpAt(int index)=>CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index]);
    private double DynamicExpressoAt(int index)=>_dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index]);
    private double NCalcAt(int index){        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        _context.D = D[index];
        _context.E = E[index];
        return _nCalcLambda(_context);}
    private double WistCompilerAt(int index){        _wistCilSession.SetArgument(0, A[index]);
        _wistCilSession.SetArgument(1, B[index]);
        _wistCilSession.SetArgument(2, C[index]);
        _wistCilSession.SetArgument(3, D[index]);
        _wistCilSession.SetArgument(4, E[index]);
        return Convert.ToDouble(_wistCilSession.Run());}
    private double WistFastInvokerAt(int index)=>_wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index]);
    private double WistInterpreterAt(int index){        _wistInterpreterSession.SetArgument(0, A[index]);
        _wistInterpreterSession.SetArgument(1, B[index]);
        _wistInterpreterSession.SetArgument(2, C[index]);
        _wistInterpreterSession.SetArgument(3, D[index]);
        _wistInterpreterSession.SetArgument(4, E[index]);
        return Convert.ToDouble(_wistInterpreterSession.Run());}
}

public class ExternalWideExpression11ExecutionBenchmarks : ExternalArithmeticBenchmarkEnvironmentBase
{
    private const string UtFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    private const string NCalcFormula = "([A] + [B] + [C] + [D]) * ([E] - [F] + [G]) / ([H] + 1.0) + [I] * [J] - [K] / 3.0";
    private const string DynamicExpressoFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    private BenchContext11 _context = null!;
    private Func<BenchContext11, double> _nCalcLambda = null!;
    private Func<double, double, double, double, double, double, double, double, double, double, double, double> _dynamicExpressoDelegate = null!;
    private DynamicMethodInvoker<double, double, double, double, double, double, double, double, double, double, double, double> _wistFastInvoker = null!;
    private ICompiledArtifactSession _wistCilSession = null!;
    private ICompiledArtifactSession _wistInterpreterSession = null!;
    [GlobalSetup]
    public void GlobalSetup(){InitializeInputData();CreateProviderAndHost();var bindingNames=new[]{"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K"};var wistCilArtifact=CompileWistCil(UtFormula,bindingNames);_wistFastInvoker=new DynamicMethodInvoker<double, double, double, double, double, double, double, double, double, double, double, double>(wistCilArtifact.CompilationOutput);_wistCilSession=wistCilArtifact.CreateSession();var wistInterpreterArtifact=CompileWistInterpreter(UtFormula,bindingNames);_wistInterpreterSession=wistInterpreterArtifact.CreateSession();_context=new BenchContext11();_nCalcLambda=new Expression(NCalcFormula).ToLambda<BenchContext11, double>();var dynamicInterpreter=new Interpreter();_dynamicExpressoDelegate=dynamicInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double, double, double, double, double, double>>(DynamicExpressoFormula, bindingNames);ValidateParity(CSharpAt, DynamicExpressoAt, NCalcAt, WistCompilerAt, WistFastInvokerAt, WistInterpreterAt);}
    [Benchmark(Baseline = true)] public double CSharp_NoInliningMethod(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i], K[i]);}return sum;}
    [Benchmark] public double DynamicExpresso_Delegate(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_dynamicExpressoDelegate(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i], K[i]);}return sum;}
    [Benchmark] public double NCalc_Lambda(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            _context.D = D[i];
            _context.E = E[i];
            _context.F = F[i];
            _context.G = G[i];
            _context.H = H[i];
            _context.I = I[i];
            _context.J = J[i];
            _context.K = K[i];
            sum+=_nCalcLambda(_context);}return sum;}
    [Benchmark] public double Wist_Cil_DynamicMethodInvoke(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistCilSession.SetArgument(0, A[i]);
            _wistCilSession.SetArgument(1, B[i]);
            _wistCilSession.SetArgument(2, C[i]);
            _wistCilSession.SetArgument(3, D[i]);
            _wistCilSession.SetArgument(4, E[i]);
            _wistCilSession.SetArgument(5, F[i]);
            _wistCilSession.SetArgument(6, G[i]);
            _wistCilSession.SetArgument(7, H[i]);
            _wistCilSession.SetArgument(8, I[i]);
            _wistCilSession.SetArgument(9, J[i]);
            _wistCilSession.SetArgument(10, K[i]);
            sum+=Convert.ToDouble(_wistCilSession.Run());}return sum;}
    [Benchmark] public double Wist_Cil_FastInvoker(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_wistFastInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i], K[i]);}return sum;}
    [Benchmark] public double Wist_Interpreter(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistInterpreterSession.SetArgument(0, A[i]);
            _wistInterpreterSession.SetArgument(1, B[i]);
            _wistInterpreterSession.SetArgument(2, C[i]);
            _wistInterpreterSession.SetArgument(3, D[i]);
            _wistInterpreterSession.SetArgument(4, E[i]);
            _wistInterpreterSession.SetArgument(5, F[i]);
            _wistInterpreterSession.SetArgument(6, G[i]);
            _wistInterpreterSession.SetArgument(7, H[i]);
            _wistInterpreterSession.SetArgument(8, I[i]);
            _wistInterpreterSession.SetArgument(9, J[i]);
            _wistInterpreterSession.SetArgument(10, K[i]);
            sum+=Convert.ToDouble(_wistInterpreterSession.Run());}return sum;}
    [MethodImpl(MethodImplOptions.NoInlining)] private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f, double g, double h, double i, double j, double k) => (a + b + c + d) * (e - f + g) / (h + 1.0) + i * j - k / 3.0;
    private double CSharpAt(int index)=>CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);
    private double DynamicExpressoAt(int index)=>_dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);
    private double NCalcAt(int index){        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        _context.D = D[index];
        _context.E = E[index];
        _context.F = F[index];
        _context.G = G[index];
        _context.H = H[index];
        _context.I = I[index];
        _context.J = J[index];
        _context.K = K[index];
        return _nCalcLambda(_context);}
    private double WistCompilerAt(int index){        _wistCilSession.SetArgument(0, A[index]);
        _wistCilSession.SetArgument(1, B[index]);
        _wistCilSession.SetArgument(2, C[index]);
        _wistCilSession.SetArgument(3, D[index]);
        _wistCilSession.SetArgument(4, E[index]);
        _wistCilSession.SetArgument(5, F[index]);
        _wistCilSession.SetArgument(6, G[index]);
        _wistCilSession.SetArgument(7, H[index]);
        _wistCilSession.SetArgument(8, I[index]);
        _wistCilSession.SetArgument(9, J[index]);
        _wistCilSession.SetArgument(10, K[index]);
        return Convert.ToDouble(_wistCilSession.Run());}
    private double WistFastInvokerAt(int index)=>_wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);
    private double WistInterpreterAt(int index){        _wistInterpreterSession.SetArgument(0, A[index]);
        _wistInterpreterSession.SetArgument(1, B[index]);
        _wistInterpreterSession.SetArgument(2, C[index]);
        _wistInterpreterSession.SetArgument(3, D[index]);
        _wistInterpreterSession.SetArgument(4, E[index]);
        _wistInterpreterSession.SetArgument(5, F[index]);
        _wistInterpreterSession.SetArgument(6, G[index]);
        _wistInterpreterSession.SetArgument(7, H[index]);
        _wistInterpreterSession.SetArgument(8, I[index]);
        _wistInterpreterSession.SetArgument(9, J[index]);
        _wistInterpreterSession.SetArgument(10, K[index]);
        return Convert.ToDouble(_wistInterpreterSession.Run());}
}

public class ExternalConstantsHeavyExecutionBenchmarks : ExternalArithmeticBenchmarkEnvironmentBase
{
    private const string UtFormula = "(A * 1.5 + B * 2.0 - C * 3.0 + D / 4.0 + E / 5.0) * 0.75 + F";
    private const string NCalcFormula = "([A] * 1.5 + [B] * 2.0 - [C] * 3.0 + [D] / 4.0 + [E] / 5.0) * 0.75 + [F]";
    private const string DynamicExpressoFormula = "(A * 1.5 + B * 2.0 - C * 3.0 + D / 4.0 + E / 5.0) * 0.75 + F";
    private BenchContext6 _context = null!;
    private Func<BenchContext6, double> _nCalcLambda = null!;
    private Func<double, double, double, double, double, double, double> _dynamicExpressoDelegate = null!;
    private DynamicMethodInvoker<double, double, double, double, double, double, double> _wistFastInvoker = null!;
    private ICompiledArtifactSession _wistCilSession = null!;
    private ICompiledArtifactSession _wistInterpreterSession = null!;
    [GlobalSetup]
    public void GlobalSetup(){InitializeInputData();CreateProviderAndHost();var bindingNames=new[]{"A", "B", "C", "D", "E", "F"};var wistCilArtifact=CompileWistCil(UtFormula,bindingNames);_wistFastInvoker=new DynamicMethodInvoker<double, double, double, double, double, double, double>(wistCilArtifact.CompilationOutput);_wistCilSession=wistCilArtifact.CreateSession();var wistInterpreterArtifact=CompileWistInterpreter(UtFormula,bindingNames);_wistInterpreterSession=wistInterpreterArtifact.CreateSession();_context=new BenchContext6();_nCalcLambda=new Expression(NCalcFormula).ToLambda<BenchContext6, double>();var dynamicInterpreter=new Interpreter();_dynamicExpressoDelegate=dynamicInterpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double>>(DynamicExpressoFormula, bindingNames);ValidateParity(CSharpAt, DynamicExpressoAt, NCalcAt, WistCompilerAt, WistFastInvokerAt, WistInterpreterAt);}
    [Benchmark(Baseline = true)] public double CSharp_NoInliningMethod(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i], F[i]);}return sum;}
    [Benchmark] public double DynamicExpresso_Delegate(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_dynamicExpressoDelegate(A[i], B[i], C[i], D[i], E[i], F[i]);}return sum;}
    [Benchmark] public double NCalc_Lambda(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            _context.D = D[i];
            _context.E = E[i];
            _context.F = F[i];
            sum+=_nCalcLambda(_context);}return sum;}
    [Benchmark] public double Wist_Cil_DynamicMethodInvoke(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistCilSession.SetArgument(0, A[i]);
            _wistCilSession.SetArgument(1, B[i]);
            _wistCilSession.SetArgument(2, C[i]);
            _wistCilSession.SetArgument(3, D[i]);
            _wistCilSession.SetArgument(4, E[i]);
            _wistCilSession.SetArgument(5, F[i]);
            sum+=Convert.ToDouble(_wistCilSession.Run());}return sum;}
    [Benchmark] public double Wist_Cil_FastInvoker(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();sum+=_wistFastInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i]);}return sum;}
    [Benchmark] public double Wist_Interpreter(){var sum=0.0;for(var k=0;k<InnerCount;k++){var i=NextIndex();            _wistInterpreterSession.SetArgument(0, A[i]);
            _wistInterpreterSession.SetArgument(1, B[i]);
            _wistInterpreterSession.SetArgument(2, C[i]);
            _wistInterpreterSession.SetArgument(3, D[i]);
            _wistInterpreterSession.SetArgument(4, E[i]);
            _wistInterpreterSession.SetArgument(5, F[i]);
            sum+=Convert.ToDouble(_wistInterpreterSession.Run());}return sum;}
    [MethodImpl(MethodImplOptions.NoInlining)] private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f) => (a * 1.5 + b * 2.0 - c * 3.0 + d / 4.0 + e / 5.0) * 0.75 + f;
    private double CSharpAt(int index)=>CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index]);
    private double DynamicExpressoAt(int index)=>_dynamicExpressoDelegate(A[index], B[index], C[index], D[index], E[index], F[index]);
    private double NCalcAt(int index){        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        _context.D = D[index];
        _context.E = E[index];
        _context.F = F[index];
        return _nCalcLambda(_context);}
    private double WistCompilerAt(int index){        _wistCilSession.SetArgument(0, A[index]);
        _wistCilSession.SetArgument(1, B[index]);
        _wistCilSession.SetArgument(2, C[index]);
        _wistCilSession.SetArgument(3, D[index]);
        _wistCilSession.SetArgument(4, E[index]);
        _wistCilSession.SetArgument(5, F[index]);
        return Convert.ToDouble(_wistCilSession.Run());}
    private double WistFastInvokerAt(int index)=>_wistFastInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index]);
    private double WistInterpreterAt(int index){        _wistInterpreterSession.SetArgument(0, A[index]);
        _wistInterpreterSession.SetArgument(1, B[index]);
        _wistInterpreterSession.SetArgument(2, C[index]);
        _wistInterpreterSession.SetArgument(3, D[index]);
        _wistInterpreterSession.SetArgument(4, E[index]);
        _wistInterpreterSession.SetArgument(5, F[index]);
        return Convert.ToDouble(_wistInterpreterSession.Run());}
}

public class ExternalSimple3PreparationBenchmarks : ExternalArithmeticBenchmarkEnvironmentBase
{
    private const string UtFormula = "A + B * C / 5.0";
    private const string NCalcFormula = "[A] + [B] * [C] / 5.0";
    private const string DynamicExpressoFormula = "A + B * C / 5.0";
    private static readonly string[] DynamicParameterNames = [ "A", "B", "C" ];
    [GlobalSetup] public void GlobalSetup(){InitializeInputData();CreateProviderAndHost();}
    [Benchmark(Baseline = true)] public int CSharp_BaselineDelegatePrepare(){for(var i=0;i<PrepareInnerCount;i++) _ = (Func<double, double, double, double>)CSharp_NoInliningMethodCore; return PrepareInnerCount;}
    [Benchmark] public int DynamicExpresso_DelegatePrepare(){var interpreter=new Interpreter();for(var i=0;i<PrepareInnerCount;i++) _=interpreter.ParseAsDelegate<Func<double, double, double, double>>(DynamicExpressoFormula, DynamicParameterNames); return PrepareInnerCount;}
    [Benchmark] public int NCalc_LambdaPrepare(){for(var i=0;i<PrepareInnerCount;i++) _=new Expression(NCalcFormula).ToLambda<BenchContext3, double>(); return PrepareInnerCount;}
    [Benchmark] public int Wist_Cil_Prepare(){var bindingNames=new[]{"A", "B", "C"};for(var i=0;i<PrepareInnerCount;i++) _=CompileWistCil(UtFormula,bindingNames); return PrepareInnerCount;}
    [Benchmark] public int Wist_Interpreter_Prepare(){var bindingNames=new[]{"A", "B", "C"};for(var i=0;i<PrepareInnerCount;i++) _=CompileWistInterpreter(UtFormula,bindingNames); return PrepareInnerCount;}
    [MethodImpl(MethodImplOptions.NoInlining)] private static double CSharp_NoInliningMethodCore(double a, double b, double c) => a + b * c / 5.0;
}

public class ExternalMedium8PreparationBenchmarks : ExternalArithmeticBenchmarkEnvironmentBase
{
    private const string UtFormula = "((A + B) * (C - D) / (E + 1.0)) + F * G - H / 3.0";
    private const string NCalcFormula = "(([A] + [B]) * ([C] - [D]) / ([E] + 1.0)) + [F] * [G] - [H] / 3.0";
    private const string DynamicExpressoFormula = "((A + B) * (C - D) / (E + 1.0)) + F * G - H / 3.0";
    private static readonly string[] DynamicParameterNames = [ "A", "B", "C", "D", "E", "F", "G", "H" ];
    [GlobalSetup] public void GlobalSetup(){InitializeInputData();CreateProviderAndHost();}
    [Benchmark(Baseline = true)] public int CSharp_BaselineDelegatePrepare(){for(var i=0;i<PrepareInnerCount;i++) _ = (Func<double, double, double, double, double, double, double, double, double>)CSharp_NoInliningMethodCore; return PrepareInnerCount;}
    [Benchmark] public int DynamicExpresso_DelegatePrepare(){var interpreter=new Interpreter();for(var i=0;i<PrepareInnerCount;i++) _=interpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double, double, double>>(DynamicExpressoFormula, DynamicParameterNames); return PrepareInnerCount;}
    [Benchmark] public int NCalc_LambdaPrepare(){for(var i=0;i<PrepareInnerCount;i++) _=new Expression(NCalcFormula).ToLambda<BenchContext8, double>(); return PrepareInnerCount;}
    [Benchmark] public int Wist_Cil_Prepare(){var bindingNames=new[]{"A", "B", "C", "D", "E", "F", "G", "H"};for(var i=0;i<PrepareInnerCount;i++) _=CompileWistCil(UtFormula,bindingNames); return PrepareInnerCount;}
    [Benchmark] public int Wist_Interpreter_Prepare(){var bindingNames=new[]{"A", "B", "C", "D", "E", "F", "G", "H"};for(var i=0;i<PrepareInnerCount;i++) _=CompileWistInterpreter(UtFormula,bindingNames); return PrepareInnerCount;}
    [MethodImpl(MethodImplOptions.NoInlining)] private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f, double g, double h) => (a + b) * (c - d) / (e + 1.0) + f * g - h / 3.0;
}

public class ExternalWideExpression11PreparationBenchmarks : ExternalArithmeticBenchmarkEnvironmentBase
{
    private const string UtFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    private const string NCalcFormula = "([A] + [B] + [C] + [D]) * ([E] - [F] + [G]) / ([H] + 1.0) + [I] * [J] - [K] / 3.0";
    private const string DynamicExpressoFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    private static readonly string[] DynamicParameterNames = [ "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" ];
    [GlobalSetup] public void GlobalSetup(){InitializeInputData();CreateProviderAndHost();}
    [Benchmark(Baseline = true)] public int CSharp_BaselineDelegatePrepare(){for(var i=0;i<PrepareInnerCount;i++) _ = (Func<double, double, double, double, double, double, double, double, double, double, double, double>)CSharp_NoInliningMethodCore; return PrepareInnerCount;}
    [Benchmark] public int DynamicExpresso_DelegatePrepare(){var interpreter=new Interpreter();for(var i=0;i<PrepareInnerCount;i++) _=interpreter.ParseAsDelegate<Func<double, double, double, double, double, double, double, double, double, double, double, double>>(DynamicExpressoFormula, DynamicParameterNames); return PrepareInnerCount;}
    [Benchmark] public int NCalc_LambdaPrepare(){for(var i=0;i<PrepareInnerCount;i++) _=new Expression(NCalcFormula).ToLambda<BenchContext11, double>(); return PrepareInnerCount;}
    [Benchmark] public int Wist_Cil_Prepare(){var bindingNames=new[]{"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K"};for(var i=0;i<PrepareInnerCount;i++) _=CompileWistCil(UtFormula,bindingNames); return PrepareInnerCount;}
    [Benchmark] public int Wist_Interpreter_Prepare(){var bindingNames=new[]{"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K"};for(var i=0;i<PrepareInnerCount;i++) _=CompileWistInterpreter(UtFormula,bindingNames); return PrepareInnerCount;}
    [MethodImpl(MethodImplOptions.NoInlining)] private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f, double g, double h, double i, double j, double k) => (a + b + c + d) * (e - f + g) / (h + 1.0) + i * j - k / 3.0;
}

public sealed class BenchContext3 { public double A { get; set; } public double B { get; set; } public double C { get; set; } }
public sealed class BenchContext5 { public double A { get; set; } public double B { get; set; } public double C { get; set; } public double D { get; set; } public double E { get; set; } }
public sealed class BenchContext6 { public double A { get; set; } public double B { get; set; } public double C { get; set; } public double D { get; set; } public double E { get; set; } public double F { get; set; } }
public sealed class BenchContext8 { public double A { get; set; } public double B { get; set; } public double C { get; set; } public double D { get; set; } public double E { get; set; } public double F { get; set; } public double G { get; set; } public double H { get; set; } }
public sealed class BenchContext11 { public double A { get; set; } public double B { get; set; } public double C { get; set; } public double D { get; set; } public double E { get; set; } public double F { get; set; } public double G { get; set; } public double H { get; set; } public double I { get; set; } public double J { get; set; } public double K { get; set; } }
