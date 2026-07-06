namespace UniversalToolchain.Benchmarks;

internal static class BenchmarkSelfTest
{
    public static void Run()
    {
        foreach (var workload in Enum.GetValues<FormulaWorkload>())
            RunHotPath(workload);

        RunConvenience();
        RunCompilation();
    }

    private static void RunHotPath(FormulaWorkload workload)
    {
        var benchmark = new FormulaHotPathBenchmarks { Workload = workload };
        try
        {
            benchmark.Setup();
            AssertFinite(benchmark.CSharp_PreparedDelegate(), $"{nameof(FormulaHotPathBenchmarks)}.{nameof(benchmark.CSharp_PreparedDelegate)}[{workload}]");
            AssertFinite(benchmark.NCalc_CompiledLambda(), $"{nameof(FormulaHotPathBenchmarks)}.{nameof(benchmark.NCalc_CompiledLambda)}[{workload}]");
            AssertFinite(benchmark.Wist_CompiledDelegate(), $"{nameof(FormulaHotPathBenchmarks)}.{nameof(benchmark.Wist_CompiledDelegate)}[{workload}]");
            AssertFinite(benchmark.Wist_CompileFuncFastInvoker(), $"{nameof(FormulaHotPathBenchmarks)}.{nameof(benchmark.Wist_CompileFuncFastInvoker)}[{workload}]");
        }
        finally
        {
            benchmark.Cleanup();
        }
    }

    private static void RunConvenience()
    {
        var benchmark = new FormulaConvenienceBenchmarks();
        try
        {
            benchmark.Setup();
            AssertFinite(benchmark.CSharp_DirectFormula(), $"{nameof(FormulaConvenienceBenchmarks)}.{nameof(benchmark.CSharp_DirectFormula)}");
            AssertFinite(benchmark.Wist_CompilerEvaluateWithDictionary(), $"{nameof(FormulaConvenienceBenchmarks)}.{nameof(benchmark.Wist_CompilerEvaluateWithDictionary)}");
            AssertFinite(benchmark.Wist_InterpreterEvaluateWithDictionary(), $"{nameof(FormulaConvenienceBenchmarks)}.{nameof(benchmark.Wist_InterpreterEvaluateWithDictionary)}");
        }
        finally
        {
            benchmark.Cleanup();
        }
    }

    private static void RunCompilation()
    {
        var benchmark = new FormulaCompilationBenchmarks();
        try
        {
            benchmark.Setup();
            _ = benchmark.CSharp_PreparedDelegate();
            _ = benchmark.Wist_CompileOnExistingEngine();
            _ = benchmark.Wist_CreateEngineAndCompile();
        }
        finally
        {
            benchmark.Cleanup();
        }
    }

    private static void AssertFinite(double value, string benchmark)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new InvalidOperationException($"Benchmark self-test produced a non-finite value for {benchmark}: {value}.");
    }
}
