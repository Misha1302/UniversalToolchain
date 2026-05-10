using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

var config = ManualConfig
    .Create(DefaultConfig.Instance)
    .WithBuildTimeout(TimeSpan.FromMinutes(5));

var summaries = BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, config)
    .ToArray();

if (summaries.Length == 0)
{
    Console.Error.WriteLine("Benchmark smoke failed: no benchmark summaries were produced.");
    return 1;
}

var executedBenchmarks = summaries
    .SelectMany(static summary => summary.Reports)
    .Count(static report => report.AllMeasurements.Any());

if (executedBenchmarks == 0)
{
    Console.Error.WriteLine("Benchmark smoke failed: BenchmarkDotNet produced no executed benchmark measurements.");
    return 1;
}

return 0;
