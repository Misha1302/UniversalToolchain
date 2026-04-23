using BenchmarkDotNet.Running;
using UniversalToolchain.Benchmarks;
using UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks;
using UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks.Unrolled16;

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

BenchmarkRunner.Run<ExternalSimple3ExecutionUnrolled16Benchmarks>();
BenchmarkRunner.Run<ExternalMedium8ExecutionUnrolled16Benchmarks>();
BenchmarkRunner.Run<ExternalDeepChain6ExecutionUnrolled16Benchmarks>();
BenchmarkRunner.Run<ExternalRepeatedSubexpressions5ExecutionUnrolled16Benchmarks>();
BenchmarkRunner.Run<ExternalWideExpression11ExecutionUnrolled16Benchmarks>();
BenchmarkRunner.Run<ExternalConstantsHeavy6ExecutionUnrolled16Benchmarks>();
