using BenchmarkDotNet.Running;
using UniversalToolchain.Benchmarks;

BenchmarkRunner.Run<Simple3Benchmarks>();
BenchmarkRunner.Run<Medium8Benchmarks>();
BenchmarkRunner.Run<DeepChain6Benchmarks>();
BenchmarkRunner.Run<RepeatedSubexpressionsBenchmarks>();
BenchmarkRunner.Run<WideExpression11Benchmarks>();