using BenchmarkDotNet.Running;
using UniversalToolchain.Benchmarks;

BenchmarkRunner.Run<ExternalSimple3ExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalMedium8ExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalDeepChain6ExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalRepeatedSubexpressionsExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalWideExpression11ExecutionBenchmarks>();
BenchmarkRunner.Run<ExternalConstantsHeavyExecutionBenchmarks>();

BenchmarkRunner.Run<ExternalSimple3PreparationBenchmarks>();
BenchmarkRunner.Run<ExternalMedium8PreparationBenchmarks>();
BenchmarkRunner.Run<ExternalWideExpression11PreparationBenchmarks>();
