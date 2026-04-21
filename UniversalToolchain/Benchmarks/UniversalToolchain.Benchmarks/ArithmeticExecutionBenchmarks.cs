using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DynamicMethodCalling.Core;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NCalc;
using NCalc.LambdaCompilation;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public abstract class ArithmeticBenchmarkEnvironmentBase
{
    protected const int DataSize = 4096;
    private WistDialectExecutionHost? _host;
    private int _index;

    private ServiceProvider? _provider;

    protected double[] A = [];
    protected double[] B = [];
    protected double[] C = [];
    protected double[] D = [];
    protected double[] E = [];
    protected double[] F = [];
    protected double[] G = [];
    protected double[] H = [];
    protected double[] I = [];
    protected double[] J = [];
    protected double[] K = [];

    protected void InitializeInputData()
    {
        A = new double[DataSize];
        B = new double[DataSize];
        C = new double[DataSize];
        D = new double[DataSize];
        E = new double[DataSize];
        F = new double[DataSize];
        G = new double[DataSize];
        H = new double[DataSize];
        I = new double[DataSize];
        J = new double[DataSize];
        K = new double[DataSize];

        var random = new Random(42);
        Fill(random, A);
        Fill(random, B);
        Fill(random, C);
        Fill(random, D);
        Fill(random, E);
        Fill(random, F);
        Fill(random, G);
        Fill(random, H);
        Fill(random, I);
        Fill(random, J);
        Fill(random, K);
    }

    protected void CreateProviderAndHost()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        _provider = services.BuildServiceProvider();
        var workflow = _provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var dialectFile = Path.Combine(AppContext.BaseDirectory, "Dialects", "examples", "wist", "full-default-native", "dialect.wistdialect");
        var dialect = workflow.ComposeFile(dialectFile);

        if (!dialect.IsSuccess)
            Thrower.InvalidOpEx($"Failed to compose dialect file: {UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(dialect))}");

        _host = workflow.CreateHost(dialect);
    }

    protected DynamicMethod CompileWistDynamicMethod(string formula, string[] bindingNames)
    {
        var host = _host ?? Thrower.InvalidOpEx<WistDialectExecutionHost>("Wist host must be initialized before compilation.");
        var compiler = host.GetArtifactCompiler<DynamicMethod>("compiler");
        var declaredBindings = CreateDeclaredBindings(bindingNames);
        var compiledArtifact = compiler.Compile(formula, declaredBindings);
        return compiledArtifact.CompilationOutput;
    }

    protected OrderedDictionary<string, Type> CreateDeclaredBindings(string[] bindingNames)
    {
        var declaredBindings = new OrderedDictionary<string, Type>();

        foreach (var bindingName in bindingNames)
            declaredBindings[bindingName] = typeof(double);

        return declaredBindings;
    }

    protected void EnsureResultParityAcrossIndexes(
        Func<int, double> cSharp,
        Func<int, double> nCalc,
        Func<int, double> wist)
    {
        var validationIndexes = new[] { 0, 1, 17, 255, 1023, 2047, 4095 };

        foreach (var index in validationIndexes)
        {
            var cSharpResult = cSharp(index);
            var nCalcResult = nCalc(index);
            var wistResult = wist(index);

            if (!AreEqual(cSharpResult, nCalcResult) || !AreEqual(cSharpResult, wistResult))
                Thrower.InvalidOpEx(
                    $"Result mismatch detected at index {index}. C#: {cSharpResult}, NCalc: {nCalcResult}, Wist: {wistResult}.");
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _host?.Dispose();
        _provider?.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int NextIndex()
    {
        var i = _index;
        _index = i + 1 & DataSize - 1;
        return i;
    }

    private static bool AreEqual(double left, double right)
    {
        const double absoluteEpsilon = 1e-9;
        const double relativeEpsilon = 1e-12;
        var delta = Math.Abs(left - right);
        var scale = Math.Max(1.0, Math.Max(Math.Abs(left), Math.Abs(right)));

        return delta <= Math.Max(absoluteEpsilon, relativeEpsilon * scale);
    }

    private static void Fill(Random random, double[] values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = 0.1 + random.NextDouble() * 999.9;
    }
}

public class Simple3Benchmarks : ArithmeticBenchmarkEnvironmentBase
{
    private const string WistFormula = "A + B * C / 5.0";
    private const string NCalcFormula = "[A] + [B] * [C] / 5.0";
    private const int InnerCount = 1024;
    private BenchContext3 _context = null!;
    private Func<BenchContext3, double> _nCalcLambda = null!;

    private DynamicMethodInvoker<double, double, double, double> _wistInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C"]);
        _wistInvoker = new DynamicMethodInvoker<double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<BenchContext3, double>();
        _context = new BenchContext3();

        EnsureResultParityAcrossIndexes(CSharpAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true)]
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

    [Benchmark]
    public double NCalc_Lambda()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            sum += _nCalcLambda(_context);
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
            sum += _wistInvoker.Invoke(A[i], B[i], C[i]);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c) => a + b * c / 5.0;

    private double CSharpAt(int index) => CSharp_NoInliningMethodCore(A[index], B[index], C[index]);

    private double NCalcAt(int index)
    {
        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        return _nCalcLambda(_context);
    }

    private double WistAt(int index) => _wistInvoker.Invoke(A[index], B[index], C[index]);
}

public class Medium8Benchmarks : ArithmeticBenchmarkEnvironmentBase
{
    private const string WistFormula = "((A + B) * (C - D) / (E + 1.0)) + F * G - H / 3.0";
    private const string NCalcFormula = "(([A] + [B]) * ([C] - [D]) / ([E] + 1.0)) + [F] * [G] - [H] / 3.0";
    private const int InnerCount = 1024;
    private BenchContext8 _context = null!;
    private Func<BenchContext8, double> _nCalcLambda = null!;

    private DynamicMethodInvoker<double, double, double, double, double, double, double, double, double> _wistInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C", "D", "E", "F", "G", "H"]);
        _wistInvoker = new DynamicMethodInvoker<double, double, double, double, double, double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<BenchContext8, double>();
        _context = new BenchContext8();

        EnsureResultParityAcrossIndexes(CSharpAt, NCalcAt, WistAt);
    }

    [Benchmark(Baseline = true)]
    public double CSharp_NoInliningMethod()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            sum += CSharp_NoInliningMethodCore(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i]);
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
            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            _context.D = D[i];
            _context.E = E[i];
            _context.F = F[i];
            _context.G = G[i];
            _context.H = H[i];
            sum += _nCalcLambda(_context);
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
            sum += _wistInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i]);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f, double g, double h) => (a + b) * (c - d) / (e + 1.0) + f * g - h / 3.0;

    private double CSharpAt(int index) => CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index]);

    private double NCalcAt(int index)
    {
        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        _context.D = D[index];
        _context.E = E[index];
        _context.F = F[index];
        _context.G = G[index];
        _context.H = H[index];
        return _nCalcLambda(_context);
    }

    private double WistAt(int index) => _wistInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index]);
}

public class DeepChain6Benchmarks : ArithmeticBenchmarkEnvironmentBase
{
    private const string WistFormula = "((((A * 1.1 + B) * 1.2 + C) * 1.3 + D) * 1.4 + E) / (F + 1.0)";
    private const string NCalcFormula = "(((([A] * 1.1 + [B]) * 1.2 + [C]) * 1.3 + [D]) * 1.4 + [E]) / ([F] + 1.0)";
    private const int InnerCount = 1024;
    private BenchContext6 _context = null!;
    private Func<BenchContext6, double> _nCalcLambda = null!;

    private DynamicMethodInvoker<double, double, double, double, double, double, double> _wistInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C", "D", "E", "F"]);
        _wistInvoker = new DynamicMethodInvoker<double, double, double, double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<BenchContext6, double>();
        _context = new BenchContext6();

        EnsureResultParityAcrossIndexes(CSharpAt, NCalcAt, WistAt);
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
    public double NCalc_Lambda()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            _context.D = D[i];
            _context.E = E[i];
            _context.F = F[i];
            sum += _nCalcLambda(_context);
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
            sum += _wistInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i]);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e, double f) => ((((a * 1.1 + b) * 1.2 + c) * 1.3 + d) * 1.4 + e) / (f + 1.0);

    private double CSharpAt(int index) => CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index], F[index]);

    private double NCalcAt(int index)
    {
        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        _context.D = D[index];
        _context.E = E[index];
        _context.F = F[index];
        return _nCalcLambda(_context);
    }

    private double WistAt(int index) => _wistInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index]);
}

public class RepeatedSubexpressionsBenchmarks : ArithmeticBenchmarkEnvironmentBase
{
    private const string WistFormula = "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)";
    private const string NCalcFormula = "(([A] * [B]) + ([A] * [B]) + ([A] * [B]) + ([C] * [D])) / ([E] + 1.0)";
    private const int InnerCount = 1024;
    private BenchContext5 _context = null!;
    private Func<BenchContext5, double> _nCalcLambda = null!;

    private DynamicMethodInvoker<double, double, double, double, double, double> _wistInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C", "D", "E"]);
        _wistInvoker = new DynamicMethodInvoker<double, double, double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<BenchContext5, double>();
        _context = new BenchContext5();

        EnsureResultParityAcrossIndexes(CSharpAt, NCalcAt, WistAt);
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
    public double NCalc_Lambda()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            _context.A = A[i];
            _context.B = B[i];
            _context.C = C[i];
            _context.D = D[i];
            _context.E = E[i];
            sum += _nCalcLambda(_context);
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
            sum += _wistInvoker.Invoke(A[i], B[i], C[i], D[i], E[i]);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(double a, double b, double c, double d, double e) => (a * b + a * b + a * b + c * d) / (e + 1.0);

    private double CSharpAt(int index) => CSharp_NoInliningMethodCore(A[index], B[index], C[index], D[index], E[index]);

    private double NCalcAt(int index)
    {
        _context.A = A[index];
        _context.B = B[index];
        _context.C = C[index];
        _context.D = D[index];
        _context.E = E[index];
        return _nCalcLambda(_context);
    }

    private double WistAt(int index) => _wistInvoker.Invoke(A[index], B[index], C[index], D[index], E[index]);
}

public class WideExpression11Benchmarks : ArithmeticBenchmarkEnvironmentBase
{
    private const string WistFormula = "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    private const string NCalcFormula = "([A] + [B] + [C] + [D]) * ([E] - [F] + [G]) / ([H] + 1.0) + [I] * [J] - [K] / 3.0";
    private const int InnerCount = 1024;
    private BenchContext11 _context = null!;
    private Func<BenchContext11, double> _nCalcLambda = null!;

    private DynamicMethodInvoker<double, double, double, double, double, double, double, double, double, double, double, double> _wistInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K"]);
        _wistInvoker = new DynamicMethodInvoker<double, double, double, double, double, double, double, double, double, double, double, double>(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<BenchContext11, double>();
        _context = new BenchContext11();

        EnsureResultParityAcrossIndexes(CSharpAt, NCalcAt, WistAt);
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
    public double NCalc_Lambda()
    {
        var sum = 0.0;
        for (var k = 0; k < InnerCount; k++)
        {
            var i = NextIndex();
            _context.A = A[i];
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
            sum += _nCalcLambda(_context);
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
            sum += _wistInvoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i], K[i]);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore(
        double a,
        double b,
        double c,
        double d,
        double e,
        double f,
        double g,
        double h,
        double i,
        double j,
        double k) =>
        (a + b + c + d) * (e - f + g) / (h + 1.0) + i * j - k / 3.0;

    private double CSharpAt(int index) =>
        CSharp_NoInliningMethodCore(
            A[index],
            B[index],
            C[index],
            D[index],
            E[index],
            F[index],
            G[index],
            H[index],
            I[index],
            J[index],
            K[index]);

    private double NCalcAt(int index)
    {
        _context.A = A[index];
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
        return _nCalcLambda(_context);
    }

    private double WistAt(int index) => _wistInvoker.Invoke(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index], I[index], J[index], K[index]);
}

public sealed class BenchContext3
{
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }
}

public sealed class BenchContext5
{
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }
    public double D { get; set; }
    public double E { get; set; }
}

public sealed class BenchContext6
{
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }
    public double D { get; set; }
    public double E { get; set; }
    public double F { get; set; }
}

public sealed class BenchContext8
{
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }
    public double D { get; set; }
    public double E { get; set; }
    public double F { get; set; }
    public double G { get; set; }
    public double H { get; set; }
}

public sealed class BenchContext11
{
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }
    public double D { get; set; }
    public double E { get; set; }
    public double F { get; set; }
    public double G { get; set; }
    public double H { get; set; }
    public double I { get; set; }
    public double J { get; set; }
    public double K { get; set; }
}