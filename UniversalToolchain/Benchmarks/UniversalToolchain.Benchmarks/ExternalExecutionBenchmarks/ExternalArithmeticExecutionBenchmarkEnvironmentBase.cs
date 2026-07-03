using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BasicCilCompiler.Execution;
using BenchmarkDotNet.Attributes;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks;

public abstract class ExternalArithmeticExecutionBenchmarkEnvironmentBase
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
        var dialectFile = new WistShippedDialectFileResolver().Resolve(WistShippedDialectPresets.FullDefaultNative);
        var composition = workflow.ComposeFile(dialectFile);

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(
                $"Failed to compose dialect file: {DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition))}");

        _host = workflow.CreateHost(composition);
    }

    protected CilCompilationOutput CompileWistCilOutput(string formula, string[] bindingNames)
    {
        var host = _host ?? Thrower.InvalidOpEx<WistDialectExecutionHost>(
            "Wist host must be initialized before compilation.");

        var compiler = host.GetBackendSpecificArtifactCompiler<CilCompilationOutput>("compiler");
        var declaredBindings = CreateDeclaredBindings(bindingNames);
        var compiledArtifact = compiler.Compile(formula, declaredBindings);

        return compiledArtifact.CompilationOutput;
    }

    protected DynamicMethod CompileWistDynamicMethod(string formula, string[] bindingNames)
        => CompileWistCilOutput(formula, bindingNames).Method;

    protected OrderedDictionary<string, Type> CreateDeclaredBindings(string[] bindingNames)
    {
        var declaredBindings = new OrderedDictionary<string, Type>();

        foreach (var bindingName in bindingNames)
            declaredBindings[bindingName] = typeof(double);

        return declaredBindings;
    }

    protected void EnsureResultParityAcrossIndexes(
        Func<int, double> cSharp,
        Func<int, double> dynamicExpresso,
        Func<int, double> nCalc,
        Func<int, double> wist)
    {
        var validationIndexes = new[] { 0, 1, 17, 255, 1023, 2047, 4095 };

        foreach (var index in validationIndexes)
        {
            var cSharpResult = cSharp(index);
            var dynamicExpressoResult = dynamicExpresso(index);
            var nCalcResult = nCalc(index);
            var wistResult = wist(index);

            if (!AreEqual(cSharpResult, dynamicExpressoResult) ||
                !AreEqual(cSharpResult, nCalcResult) ||
                !AreEqual(cSharpResult, wistResult))
                Thrower.InvalidOpEx(
                    $"Result mismatch at index {index}. " +
                    $"C#: {cSharpResult}, DynamicExpresso: {dynamicExpressoResult}, NCalc: {nCalcResult}, Wist: {wistResult}.");
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
