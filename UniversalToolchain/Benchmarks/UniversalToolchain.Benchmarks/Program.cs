using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using UniversalToolchain.Benchmarks;

if (args.Contains("--self-test", StringComparer.Ordinal))
{
    BenchmarkSelfTest.Run();
    Console.WriteLine("Benchmark self-test passed.");
    return 0;
}

var config = ManualConfig
    .Create(DefaultConfig.Instance)
    .WithBuildTimeout(TimeSpan.FromMinutes(5));

var summaries = BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, config)
    .ToArray();

var isDiscoveryCommand = args.Any(static arg =>
    string.Equals(arg, "--list", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "--info", StringComparison.OrdinalIgnoreCase));

if (summaries.Length == 0)
{
    if (isDiscoveryCommand)
        return 0;

    Console.Error.WriteLine("Benchmark run failed: no benchmark summaries were produced.");
    return 1;
}

var executedBenchmarks = summaries
    .SelectMany(static summary => summary.Reports)
    .Count(static report => report.AllMeasurements.Any());

if (executedBenchmarks == 0)
{
    Console.Error.WriteLine("Benchmark run failed: BenchmarkDotNet produced no executed measurements.");
    return 1;
}

return 0;
