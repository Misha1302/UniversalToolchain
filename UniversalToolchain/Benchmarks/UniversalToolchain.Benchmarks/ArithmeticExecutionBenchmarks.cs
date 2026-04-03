using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using DynamicMethodCalling.Core;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NCalc;
using NCalc.LambdaCompilation;
using UniversalToolchain.Dialects.Wist;

[MemoryDiagnoser]
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public abstract class ArithmeticExecutionBenchmarks
{
    private const int DataSize = 4096;

    private ServiceProvider? _provider;
    private WistDialectExecutionHost? _host;
    private Func<BenchContext, double>? _nCalcLambda;
    private Func<int, double>? _wistInvoker;
    private BenchContext? _context;
    private int _index;

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

    protected abstract string WistFormula { get; }
    protected abstract string NCalcFormula { get; }
    protected abstract string[] BindingNames { get; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeInputData();

        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        _provider = services.BuildServiceProvider();
        var workflow = _provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var dialectFile = Path.Combine(AppContext.BaseDirectory, "Dialects", "examples", "wist", "full-default-native", "dialect.wistdialect");
        var dialect = workflow.ComposeFile(dialectFile);

        if (!dialect.IsSuccess)
            Thrower.InvalidOpEx($"Failed to compose dialect file: {dialect.ToDeterministicText()}");

        _host = workflow.CreateHost(dialect);

        var compiler = _host.GetArtifactCompiler<DynamicMethod>("compiler");
        var declaredBindings = CreateDeclaredBindings();
        var compiledArtifact = compiler.Compile(WistFormula, declaredBindings);

        _wistInvoker = CreateWistInvoker(compiledArtifact.CompilationOutput);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<BenchContext, double>();
        _context = new BenchContext();

        EnsureResultParity();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _host?.Dispose();
        _provider?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public double CSharp_NoInliningMethod()
    {
        var i = NextIndex();
        return ExecuteCSharp(i);
    }

    [Benchmark]
    public double NCalc_Lambda()
    {
        Thrower.AssertAlways(_context != null, "Benchmark context must be initialized.");
        Thrower.AssertAlways(_nCalcLambda != null, "NCalc lambda must be initialized.");

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

        return _nCalcLambda(_context);
    }

    [Benchmark]
    public double Wist_Cil_FastInvoker()
    {
        Thrower.AssertAlways(_wistInvoker != null, "Wist invoker must be initialized.");

        var i = NextIndex();
        return _wistInvoker(i);
    }

    protected abstract double ExecuteCSharp(int index);

    protected virtual OrderedDictionary<string, Type> CreateDeclaredBindings()
    {
        var declaredBindings = new OrderedDictionary<string, Type>();

        foreach (var bindingName in BindingNames)
            declaredBindings[bindingName] = typeof(double);

        return declaredBindings;
    }

    protected virtual Func<int, double> CreateWistInvoker(DynamicMethod dynamicMethod)
    {
        return BindingNames.Length switch
        {
            3 => BuildInvoker3(dynamicMethod),
            5 => BuildInvoker5(dynamicMethod),
            6 => BuildInvoker6(dynamicMethod),
            8 => BuildInvoker8(dynamicMethod),
            11 => BuildInvoker11(dynamicMethod),
            _ => Thrower.InvalidOpEx<Func<int, double>>($"Unsupported binding count for fast invoker: {BindingNames.Length}.")
        };
    }

    private Func<int, double> BuildInvoker3(DynamicMethod dynamicMethod)
    {
        var invoker = new DynamicMethodInvoker<double, double, double, double>(dynamicMethod);
        return i => invoker.Invoke(A[i], B[i], C[i]);
    }

    private Func<int, double> BuildInvoker5(DynamicMethod dynamicMethod)
    {
        var invoker = new DynamicMethodInvoker<double, double, double, double, double, double>(dynamicMethod);
        return i => invoker.Invoke(A[i], B[i], C[i], D[i], E[i]);
    }

    private Func<int, double> BuildInvoker6(DynamicMethod dynamicMethod)
    {
        var invoker = new DynamicMethodInvoker<double, double, double, double, double, double, double>(dynamicMethod);
        return i => invoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i]);
    }

    private Func<int, double> BuildInvoker8(DynamicMethod dynamicMethod)
    {
        var invoker = new DynamicMethodInvoker<double, double, double, double, double, double, double, double, double>(dynamicMethod);
        return i => invoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i]);
    }

    private Func<int, double> BuildInvoker11(DynamicMethod dynamicMethod)
    {
        var invoker = new DynamicMethodInvoker<double, double, double, double, double, double, double, double, double, double, double, double>(dynamicMethod);
        return i => invoker.Invoke(A[i], B[i], C[i], D[i], E[i], F[i], G[i], H[i], I[i], J[i], K[i]);
    }

    private void EnsureResultParity()
    {
        Thrower.AssertAlways(_context != null, "Benchmark context must be initialized.");
        Thrower.AssertAlways(_nCalcLambda != null, "NCalc lambda must be initialized.");
        Thrower.AssertAlways(_wistInvoker != null, "Wist invoker must be initialized.");

        const int validationIndex = 0;

        _context.A = A[validationIndex];
        _context.B = B[validationIndex];
        _context.C = C[validationIndex];
        _context.D = D[validationIndex];
        _context.E = E[validationIndex];
        _context.F = F[validationIndex];
        _context.G = G[validationIndex];
        _context.H = H[validationIndex];
        _context.I = I[validationIndex];
        _context.J = J[validationIndex];
        _context.K = K[validationIndex];

        var cSharpResult = ExecuteCSharp(validationIndex);
        var nCalcResult = _nCalcLambda(_context);
        var wistResult = _wistInvoker(validationIndex);

        if (!AreEqual(cSharpResult, nCalcResult) || !AreEqual(cSharpResult, wistResult))
        {
            Thrower.InvalidOpEx(
                $"Result mismatch detected. C#: {cSharpResult}, NCalc: {nCalcResult}, Wist: {wistResult}.");
        }
    }

    private static bool AreEqual(double left, double right)
    {
        return Math.Abs(left - right) <= 1e-9;
    }

    private void InitializeInputData()
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

    private static void Fill(Random random, double[] values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = 0.1 + random.NextDouble() * 999.9;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NextIndex()
    {
        var i = _index;
        _index = (i + 1) & (DataSize - 1);
        return i;
    }
}

public class Simple3Benchmarks : ArithmeticExecutionBenchmarks
{
    protected override string WistFormula => "A + B * C / 5.0";
    protected override string NCalcFormula => "[A] + [B] * [C] / 5.0";
    protected override string[] BindingNames => ["A", "B", "C"];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethod(double a, double b, double c)
    {
        return a + b * c / 5.0;
    }

    protected override double ExecuteCSharp(int index)
    {
        return CSharp_NoInliningMethod(A[index], B[index], C[index]);
    }
}

public class Medium8Benchmarks : ArithmeticExecutionBenchmarks
{
    protected override string WistFormula => "((A + B) * (C - D) / (E + 1.0)) + F * G - H / 3.0";
    protected override string NCalcFormula => "(([A] + [B]) * ([C] - [D]) / ([E] + 1.0)) + [F] * [G] - [H] / 3.0";
    protected override string[] BindingNames => ["A", "B", "C", "D", "E", "F", "G", "H"];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethod(double a, double b, double c, double d, double e, double f, double g, double h)
    {
        return ((a + b) * (c - d) / (e + 1.0)) + f * g - h / 3.0;
    }

    protected override double ExecuteCSharp(int index)
    {
        return CSharp_NoInliningMethod(A[index], B[index], C[index], D[index], E[index], F[index], G[index], H[index]);
    }
}

public class DeepChain6Benchmarks : ArithmeticExecutionBenchmarks
{
    protected override string WistFormula => "((((A * 1.1 + B) * 1.2 + C) * 1.3 + D) * 1.4 + E) / (F + 1.0)";
    protected override string NCalcFormula => "(((([A] * 1.1 + [B]) * 1.2 + [C]) * 1.3 + [D]) * 1.4 + [E]) / ([F] + 1.0)";
    protected override string[] BindingNames => ["A", "B", "C", "D", "E", "F"];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethod(double a, double b, double c, double d, double e, double f)
    {
        return ((((a * 1.1 + b) * 1.2 + c) * 1.3 + d) * 1.4 + e) / (f + 1.0);
    }

    protected override double ExecuteCSharp(int index)
    {
        return CSharp_NoInliningMethod(A[index], B[index], C[index], D[index], E[index], F[index]);
    }
}

public class RepeatedSubexpressionsBenchmarks : ArithmeticExecutionBenchmarks
{
    protected override string WistFormula => "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)";
    protected override string NCalcFormula => "(([A] * [B]) + ([A] * [B]) + ([A] * [B]) + ([C] * [D])) / ([E] + 1.0)";
    protected override string[] BindingNames => ["A", "B", "C", "D", "E"];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethod(double a, double b, double c, double d, double e)
    {
        return ((a * b) + (a * b) + (a * b) + (c * d)) / (e + 1.0);
    }

    protected override double ExecuteCSharp(int index)
    {
        return CSharp_NoInliningMethod(A[index], B[index], C[index], D[index], E[index]);
    }
}

public class WideExpression11Benchmarks : ArithmeticExecutionBenchmarks
{
    protected override string WistFormula => "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0";
    protected override string NCalcFormula => "([A] + [B] + [C] + [D]) * ([E] - [F] + [G]) / ([H] + 1.0) + [I] * [J] - [K] / 3.0";
    protected override string[] BindingNames => ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K"];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethod(
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
        double k)
    {
        return (a + b + c + d) * (e - f + g) / (h + 1.0) + i * j - k / 3.0;
    }

    protected override double ExecuteCSharp(int index)
    {
        return CSharp_NoInliningMethod(
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
    }
}

public sealed class BenchContext
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
