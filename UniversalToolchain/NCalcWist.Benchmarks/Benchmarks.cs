namespace NCalcWist.Benchmarks;

public enum ScenarioId
{
    ConstantArithmetic,
    TwoParameterIntAddition,
    TwoParameterDoubleComplex,
    MathHeavy,
    Conditional,
    ParameterHeavy20,
    PathologicalParseStress
}

public sealed record ScenarioSpec(
    ScenarioId Id,
    string WistCode,
    string NCalcCode,
    OrderedDictionary<string, Type> WistParams,
    Func<double> WistRun,
    Func<double> NCalcRun,
    Func<object> NCalcParseOnly,
    Func<object> NCalcCompileOnly,
    double Expected,
    bool IsFloatingPoint
);

public static class BenchState
{
    public static readonly ServiceProvider Provider = new ServiceCollection()
        .AddWistServices(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native, string.IsNullOrWhiteSpace(GlobalConfig.ModulesPath) ? null : GlobalConfig.ModulesPath)
        .BuildServiceProvider();

    public static readonly IExecutableGiver<DynamicMethod> WistExecutableGiver = Provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();
    private static readonly Func<ILexer> _lexerFactory = Provider.GetRequiredService<Func<ILexer>>();
    private static readonly Func<IParser> _parserFactory = Provider.GetRequiredService<Func<IParser>>();
    private static readonly IReadOnlyList<IFrontendCoreModule> _frontendModules = Provider.GetServices<IFrontendCoreModule>().ToList();

    private static readonly BinaryIntContext _intCtx = new() { A = 7, B = 3 };
    private static readonly BinaryDoubleContext _doubleCtx = new() { A = 10.25, B = 4.75 };
    private static readonly Heavy20Context _heavyCtx = Heavy20Context.Create();

    private static readonly Lazy<DynamicMethodInvoker<int>> _wConst = new(() => new DynamicMethodInvoker<int>(WistExecutableGiver.GetExecutable("3 + 4 * 5")));
    private static readonly Lazy<DynamicMethodInvoker<int, int, int>> _wIntAdd = new(() => new DynamicMethodInvoker<int, int, int>(WistExecutableGiver.GetExecutable("a + b", new OrderedDictionary<string, Type> { { "a", typeof(int) }, { "b", typeof(int) } })));
    private static readonly Lazy<DynamicMethodInvoker<double, double, double>> _wDoubleComplex = new(() => new DynamicMethodInvoker<double, double, double>(WistExecutableGiver.GetExecutable("(a*3.0 + b*2.0) / (a - b + 1.0)", new OrderedDictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } })));
    private static readonly Lazy<DynamicMethodInvoker<double, double, double>> _wMath = new(() => new DynamicMethodInvoker<double, double, double>(WistExecutableGiver.GetExecutable("System.Math.Pow(a, 1.5) + System.Math.Sqrt(b)", new OrderedDictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } })));
    private static readonly Lazy<DynamicMethodInvoker<int, int, int>> _wCond = new(() => new DynamicMethodInvoker<int, int, int>(WistExecutableGiver.GetExecutable("if a > b a else b", new OrderedDictionary<string, Type> { { "a", typeof(int) }, { "b", typeof(int) } })));
    private static readonly Lazy<DynamicMethodInvoker<double>> _wHeavy = new(() => new DynamicMethodInvoker<double>(WistExecutableGiver.GetExecutable(ScenarioFactory.ParameterHeavyWist, [])));
    private static readonly Lazy<DynamicMethodInvoker<int>> _wParseStress = new(() => new DynamicMethodInvoker<int>(WistExecutableGiver.GetExecutable(ScenarioFactory.PathologicalWist, [])));

    private static readonly Lazy<Func<int>> _nConst = new(() => new Expression("3 + 4 * 5").ToLambda<int>());
    private static readonly Lazy<Func<BinaryIntContext, int>> _nIntAdd = new(() => new Expression("[A] + [B]").ToLambda<BinaryIntContext, int>());
    private static readonly Lazy<Func<BinaryDoubleContext, double>> _nDoubleComplex = new(() => new Expression("([A]*3.0 + [B]*2.0) / ([A] - [B] + 1.0)").ToLambda<BinaryDoubleContext, double>());
    private static readonly Lazy<Func<BinaryDoubleContext, double>> _nMath = new(() => new Expression("Pow([A], 1.5) + Sqrt([B])").ToLambda<BinaryDoubleContext, double>());
    private static readonly Lazy<Func<BinaryIntContext, int>> _nCond = new(() => new Expression("if([A] > [B], [A], [B])").ToLambda<BinaryIntContext, int>());
    private static readonly Lazy<Func<Heavy20Context, double>> _nHeavy = new(() => new Expression(ScenarioFactory.ParameterHeavyNCalc).ToLambda<Heavy20Context, double>());
    private static readonly Lazy<Func<int>> _nParseStress = new(() => new Expression(ScenarioFactory.PathologicalNCalc).ToLambda<int>());

    public static readonly IReadOnlyDictionary<ScenarioId, ScenarioSpec> Scenarios = new Dictionary<ScenarioId, ScenarioSpec>
    {
        [ScenarioId.ConstantArithmetic] = new(
            ScenarioId.ConstantArithmetic,
            "3 + 4 * 5",
            "3 + 4 * 5",
            [],
            () => _wConst.Value.Invoke(),
            () => _nConst.Value(),
            () => new Expression("3 + 4 * 5").HasErrors(),
            () => new Expression("3 + 4 * 5").ToLambda<int>(),
            23,
            false),
        [ScenarioId.TwoParameterIntAddition] = new(
            ScenarioId.TwoParameterIntAddition,
            "a + b",
            "[A] + [B]",
            new OrderedDictionary<string, Type> { { "a", typeof(int) }, { "b", typeof(int) } },
            () => _wIntAdd.Value.Invoke(_intCtx.A, _intCtx.B),
            () => _nIntAdd.Value(_intCtx),
            () => new Expression("[A] + [B]").GetParameterNames(),
            () => new Expression("[A] + [B]").ToLambda<BinaryIntContext, int>(),
            10,
            false),
        [ScenarioId.TwoParameterDoubleComplex] = new(
            ScenarioId.TwoParameterDoubleComplex,
            "(a*3.0 + b*2.0) / (a - b + 1.0)",
            "([A]*3.0 + [B]*2.0) / ([A] - [B] + 1.0)",
            new OrderedDictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } },
            () => _wDoubleComplex.Value.Invoke(_doubleCtx.A, _doubleCtx.B),
            () => _nDoubleComplex.Value(_doubleCtx),
            () => new Expression("([A]*3.0 + [B]*2.0) / ([A] - [B] + 1.0)").GetParameterNames(),
            () => new Expression("([A]*3.0 + [B]*2.0) / ([A] - [B] + 1.0)").ToLambda<BinaryDoubleContext, double>(),
            0,
            true),
        [ScenarioId.MathHeavy] = new(
            ScenarioId.MathHeavy,
            "System.Math.Pow(a, 1.5) + System.Math.Sqrt(b)",
            "Pow([A], 1.5) + Sqrt([B])",
            new OrderedDictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } },
            () => _wMath.Value.Invoke(_doubleCtx.A, _doubleCtx.B),
            () => _nMath.Value(_doubleCtx),
            () => new Expression("Pow([A], 1.5) + Sqrt([B])").GetParameterNames(),
            () => new Expression("Pow([A], 1.5) + Sqrt([B])").ToLambda<BinaryDoubleContext, double>(),
            0,
            true),
        [ScenarioId.Conditional] = new(
            ScenarioId.Conditional,
            "if a > b a else b",
            "if([A] > [B], [A], [B])",
            new OrderedDictionary<string, Type> { { "a", typeof(int) }, { "b", typeof(int) } },
            () => _wCond.Value.Invoke(_intCtx.A, _intCtx.B),
            () => _nCond.Value(_intCtx),
            () => new Expression("if([A] > [B], [A], [B])").GetParameterNames(),
            () => new Expression("if([A] > [B], [A], [B])").ToLambda<BinaryIntContext, int>(),
            7,
            false),
        [ScenarioId.ParameterHeavy20] = new(
            ScenarioId.ParameterHeavy20,
            ScenarioFactory.ParameterHeavyWist,
            ScenarioFactory.ParameterHeavyNCalc,
            [],
            () => _wHeavy.Value.Invoke(),
            () => _nHeavy.Value(_heavyCtx),
            () => new Expression(ScenarioFactory.ParameterHeavyNCalc).GetParameterNames(),
            () => new Expression(ScenarioFactory.ParameterHeavyNCalc).ToLambda<Heavy20Context, double>(),
            0,
            true),
        [ScenarioId.PathologicalParseStress] = new(
            ScenarioId.PathologicalParseStress,
            ScenarioFactory.PathologicalWist,
            ScenarioFactory.PathologicalNCalc,
            [],
            () => _wParseStress.Value.Invoke(),
            () => _nParseStress.Value(),
            () => new Expression(ScenarioFactory.PathologicalNCalc).GetParameterNames(),
            () => new Expression(ScenarioFactory.PathologicalNCalc).ToLambda<int>(),
            0,
            false)
    };


    public static int WistParseOnlyHash(string code)
    {
        // Closest fair boundary available through public Wist APIs: text preprocessing + lexer + parser.
        // We intentionally stop before AST->bytecode translation and compilation.
        var lexer = _lexerFactory();
        var parser = _parserFactory();

        var processedCode = _frontendModules.Aggregate(code, (current, module) => module.ProcessText(current));
        foreach (var module in _frontendModules)
            module.InitLexer(lexer);

        var lexemes = lexer.Lexemize(processedCode);
        var processedLexemes = _frontendModules.Aggregate(lexemes, (current, module) => module.ProcessLexemes(current));
        foreach (var module in _frontendModules)
            module.InitParser(parser);

        var ast = parser.Parse(processedLexemes);
        return ast.GetHashCode();
    }
}

[MemoryDiagnoser]
public class ParseOnlyBenchmarks
{
    [Params(ScenarioId.ConstantArithmetic, ScenarioId.TwoParameterIntAddition, ScenarioId.TwoParameterDoubleComplex, ScenarioId.MathHeavy, ScenarioId.Conditional, ScenarioId.ParameterHeavy20, ScenarioId.PathologicalParseStress)]
    public ScenarioId Scenario { get; set; }

    [Benchmark(Baseline = true)]
    public int Wist_ParseOnly()
    {
        var spec = BenchState.Scenarios[Scenario];
        return BenchState.WistParseOnlyHash(spec.WistCode);
    }

    [Benchmark]
    public int NCalc_ParseOnly()
    {
        var spec = BenchState.Scenarios[Scenario];
        return spec.NCalcParseOnly().GetHashCode();
    }
}

[MemoryDiagnoser]
public class CompileOnlyBenchmarks
{
    [Params(ScenarioId.ConstantArithmetic, ScenarioId.TwoParameterIntAddition, ScenarioId.TwoParameterDoubleComplex, ScenarioId.MathHeavy, ScenarioId.Conditional, ScenarioId.ParameterHeavy20, ScenarioId.PathologicalParseStress)]
    public ScenarioId Scenario { get; set; }

    [Benchmark(Baseline = true)]
    public DynamicMethod Wist_CompileOnly()
    {
        var spec = BenchState.Scenarios[Scenario];
        return BenchState.WistExecutableGiver.GetExecutable(spec.WistCode, spec.WistParams);
    }

    [Benchmark]
    public object NCalc_CompileOnly()
    {
        var spec = BenchState.Scenarios[Scenario];
        return spec.NCalcCompileOnly();
    }
}

[MemoryDiagnoser]
public class ExecuteOnlyBenchmarks
{
    [Params(ScenarioId.ConstantArithmetic, ScenarioId.TwoParameterIntAddition, ScenarioId.TwoParameterDoubleComplex, ScenarioId.MathHeavy, ScenarioId.Conditional, ScenarioId.ParameterHeavy20, ScenarioId.PathologicalParseStress)]
    public ScenarioId Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        ValidateScenario(Scenario);
    }

    [Benchmark(Baseline = true)]
    public double Wist_ExecuteOnly() => BenchState.Scenarios[Scenario].WistRun();

    [Benchmark]
    public double NCalc_ExecuteOnly() => BenchState.Scenarios[Scenario].NCalcRun();

    private static void ValidateScenario(ScenarioId scenario)
    {
        var spec = BenchState.Scenarios[scenario];
        var w = spec.WistRun();
        var n = spec.NCalcRun();
        if (spec.IsFloatingPoint)
        {
            var diff = Math.Abs(w - n);
            var scale = Math.Max(1.0, Math.Max(Math.Abs(w), Math.Abs(n)));
            if (diff > 1e-9 * scale)
                Thrower.InvalidOpEx($"Mismatch for {scenario}: Wist={w}, NCalc={n}");
        }
        else if (Math.Abs(w - n) > 1e-9)
        {
            Thrower.InvalidOpEx($"Mismatch for {scenario}: Wist={w}, NCalc={n}");
        }
    }
}

[MemoryDiagnoser]
public class ColdStartBenchmarks
{
    [Params(ScenarioId.ConstantArithmetic, ScenarioId.TwoParameterIntAddition, ScenarioId.TwoParameterDoubleComplex, ScenarioId.MathHeavy, ScenarioId.Conditional, ScenarioId.ParameterHeavy20, ScenarioId.PathologicalParseStress)]
    public ScenarioId Scenario { get; set; }

    [Benchmark(Baseline = true)]
    public double Wist_ColdStart()
    {
        var spec = BenchState.Scenarios[Scenario];
        var method = BenchState.WistExecutableGiver.GetExecutable(spec.WistCode, spec.WistParams);
        return RunWistOnce(spec, method);
    }

    [Benchmark]
    public double NCalc_ColdStart()
    {
        var spec = BenchState.Scenarios[Scenario];
        _ = spec.NCalcParseOnly();
        return spec.NCalcRun();
    }

    private static double RunWistOnce(ScenarioSpec spec, DynamicMethod method)
    {
        return spec.Id switch
        {
            ScenarioId.ConstantArithmetic => new DynamicMethodInvoker<int>(method).Invoke(),
            ScenarioId.TwoParameterIntAddition => new DynamicMethodInvoker<int, int, int>(method).Invoke(7, 3),
            ScenarioId.TwoParameterDoubleComplex => new DynamicMethodInvoker<double, double, double>(method).Invoke(10.25, 4.75),
            ScenarioId.MathHeavy => new DynamicMethodInvoker<double, double, double>(method).Invoke(10.25, 4.75),
            ScenarioId.Conditional => new DynamicMethodInvoker<int, int, int>(method).Invoke(7, 3),
            ScenarioId.ParameterHeavy20 => new DynamicMethodInvoker<double>(method).Invoke(),
            ScenarioId.PathologicalParseStress => new DynamicMethodInvoker<int>(method).Invoke(),
            _ => Thrower.ArgumentOutOfRange<ScenarioSpec>(nameof(id), $"Unknown scenario id: {id}")
        };
    }
}

[MemoryDiagnoser]
[BenchmarkCategory("NCalcDefault")]
public class MultiThreadExecuteDefaultBenchmarks
{
    private static readonly BinaryDoubleContext[] _nData = Enumerable.Range(0, 2048)
        .Select(i => new BinaryDoubleContext { A = 10 + i % 7, B = 2 + i % 5 })
        .ToArray();

    private static readonly (double A, double B)[] _wData = _nData.Select(x => (x.A, x.B)).ToArray();
    private static readonly DynamicMethodInvoker<double, double, double> _wInvoker = new(BenchState.WistExecutableGiver.GetExecutable("(a*3.0 + b*2.0) / (a - b + 1.0)", new OrderedDictionary<string, Type> { { "a", typeof(double) }, { "b", typeof(double) } }));
    private static readonly Func<BinaryDoubleContext, double> _nInvoker = new Expression("([A]*3.0 + [B]*2.0) / ([A] - [B] + 1.0)").ToLambda<BinaryDoubleContext, double>();

    [Params(2, 4)]
    public int Threads { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var w = _wInvoker.Invoke(_wData[0].A, _wData[0].B);
        var n = _nInvoker(_nData[0]);
        if (Math.Abs(w - n) > 1e-9)
            Thrower.InvalidOpEx("MT validation failed");
    }

    [Benchmark(Baseline = true)]
    public double Wist_ExecuteSharedCompiled()
    {
        return RunParallel(i => _wInvoker.Invoke(_wData[i].A, _wData[i].B));
    }

    [Benchmark]
    public double NCalc_ExecuteSharedCompiled()
    {
        return RunParallel(i => _nInvoker(_nData[i]));
    }

    private double RunParallel(Func<int, double> invoker)
    {
        var chunk = _wData.Length / Threads;
        var sums = new double[Threads];
        Parallel.For(0, Threads, worker =>
        {
            var start = worker * chunk;
            var end = worker == Threads - 1 ? _wData.Length : start + chunk;
            double acc = 0;
            for (var i = start; i < end; i++)
                acc += invoker(i);
            sums[worker] = acc;
        });

        return sums.Sum();
    }
}

[MemoryDiagnoser]
[BenchmarkCategory("NCalcOptimized")]
public class MultiThreadExecuteOptimizedBenchmarks : MultiThreadExecuteDefaultBenchmarks
{
    static MultiThreadExecuteOptimizedBenchmarks()
    {
        // If this switch is supported by the installed NCalc version, run this benchmark with a filter that only includes this class.
        // Parser compilation is a process-level option; separate process execution is the fairest way to compare default vs optimized.
        AppContext.SetSwitch("NCalc.EnableParlotParserCompilation", true);
    }
}

public static class ScenarioFactory
{
    public static readonly string ParameterHeavyWist = BuildWistHeavy();
    public static readonly string ParameterHeavyNCalc = BuildNCalcHeavy();
    public static readonly string PathologicalWist = BuildPathological("1", 5);
    public static readonly string PathologicalNCalc = BuildPathological("1", 5);

    private static string BuildWistHeavy()
    {
        var lets = string.Join('\n', Enumerable.Range(0, 20).Select(i => $"let p{i} = {i + 1}.0"));
        var sum = string.Join(" + ", Enumerable.Range(0, 20).Select(i => $"p{i}"));
        return $"{lets}\n{sum}";
    }

    private static string BuildNCalcHeavy()
    {
        return string.Join(" + ", Enumerable.Range(0, 20).Select(i => $"[P{i}]"));
    }

    private static string BuildPathological(string start, int depth) =>
        // Deeply nested parentheses can trigger parser pathologies in some engines/versions.
        // Use a long linear chain to keep stress high while guaranteeing termination.
        string.Join(" + ", Enumerable.Repeat(start, depth + 1));
}

public sealed class BinaryIntContext
{
    public int A { get; init; }
    public int B { get; init; }
}

public sealed class BinaryDoubleContext
{
    public double A { get; init; }
    public double B { get; init; }
}

public sealed class Heavy20Context
{
    public double P0 { get; init; }
    public double P1 { get; init; }
    public double P2 { get; init; }
    public double P3 { get; init; }
    public double P4 { get; init; }
    public double P5 { get; init; }
    public double P6 { get; init; }
    public double P7 { get; init; }
    public double P8 { get; init; }
    public double P9 { get; init; }
    public double P10 { get; init; }
    public double P11 { get; init; }
    public double P12 { get; init; }
    public double P13 { get; init; }
    public double P14 { get; init; }
    public double P15 { get; init; }
    public double P16 { get; init; }
    public double P17 { get; init; }
    public double P18 { get; init; }
    public double P19 { get; init; }

    public static Heavy20Context Create() => new()
    {
        P0 = 1, P1 = 2, P2 = 3, P3 = 4, P4 = 5, P5 = 6, P6 = 7, P7 = 8, P8 = 9, P9 = 10,
        P10 = 11, P11 = 12, P12 = 13, P13 = 14, P14 = 15, P15 = 16, P16 = 17, P17 = 18, P18 = 19, P19 = 20
    };
}