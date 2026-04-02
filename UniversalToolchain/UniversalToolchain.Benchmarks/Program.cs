using BenchmarkDotNet.Running;
using UniversalToolchain.Benchmarks.Benchmarks;

BenchmarkRunner.Run<ArithmeticExecutionBenchmarks>();
BenchmarkRunner.Run<DecimalExecutionBenchmarks>();
BenchmarkRunner.Run<BooleanExecutionBenchmarks>();
BenchmarkRunner.Run<ScaleExecutionBenchmarks>();
