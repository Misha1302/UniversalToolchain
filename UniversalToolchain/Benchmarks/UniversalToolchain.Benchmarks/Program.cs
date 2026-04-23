using BenchmarkDotNet.Running;
using UniversalToolchain.Benchmarks;
using UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks;

BenchmarkRunner.Run<Simple3Benchmarks>();
BenchmarkRunner.Run<Medium8Benchmarks>();
BenchmarkRunner.Run<DeepChain6Benchmarks>();
BenchmarkRunner.Run<RepeatedSubexpressionsBenchmarks>();
BenchmarkRunner.Run<WideExpression11Benchmarks>();

BenchmarkRunner.Run<ExternalSimple3ExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalMedium8ExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalDeepChain6ExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalRepeatedSubexpressions5ExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalWideExpression11ExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalConstantsHeavy6ExecutionBenchmarks>();
