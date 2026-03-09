using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace NCalcWist.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        var benchmarkArgs = ExtractCustomArgs(args, out var modulesPath);
        GlobalConfig.ModulesPath = modulesPath;

        var config = DefaultConfig.Instance
            .AddDiagnoser(MemoryDiagnoser.Default);
        // Optional disassembly: run with --disasm and uncomment the line below for deeper JIT analysis.
        // config = config.AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 2)));

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(benchmarkArgs, config);
    }

    private static string[] ExtractCustomArgs(string[] args, out string? modulesPath)
    {
        const string key = "--modulesPath=";
        modulesPath = Environment.GetEnvironmentVariable("WIST_MODULES_PATH");

        var filtered = new List<string>(args.Length);
        foreach (var arg in args)
        {
            if (arg.StartsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                modulesPath = arg[key.Length..];
                continue;
            }

            filtered.Add(arg);
        }

        return filtered.ToArray();
    }
}

public static class GlobalConfig
{
    public static string? ModulesPath { get; set; }
}