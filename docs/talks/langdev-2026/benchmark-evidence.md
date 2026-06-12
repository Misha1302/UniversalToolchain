# Benchmark evidence and claim boundary

## Claim used in the proposal

> In selected arithmetic hot-execution benchmarks, cached Wist CIL artifacts remain within 10% of a no-inlining C# baseline and allocate no memory during the measured execution phase.

This is deliberately narrower than saying that Wist is generally as fast as C#.

## Recorded run

The numbers below come from the project article's BenchmarkDotNet run at repository commit:

```text
687677c61454f5c51ceb7620ccbf831cee3b2e05
```

Recorded environment:

- Fedora Linux 43;
- 12th Gen Intel Core i5-1235U;
- .NET SDK 10.0.104;
- .NET runtime 10.0.4;
- X64 RyuJIT x86-64-v3;
- BenchmarkDotNet 0.15.6;
- Concurrent Workstation GC.

| Workload | C# mean, ns/op | Wist CIL mean, ns/op | Dynamic Expresso, ns/op | NCalc, ns/op | Wist/C# |
|---|---:|---:|---:|---:|---:|
| ConstantsHeavy6 | 2.633 | 2.692 | 2.749 | 4.272 | 1.02 |
| DeepChain6 | 2.643 | 2.714 | 2.790 | 4.204 | 1.03 |
| Medium8 | 2.801 | 3.055 | 3.103 | 4.406 | 1.09 |
| RepeatedSubexpressions5 | 2.028 | 2.062 | 2.242 | 3.238 | 1.02 |
| Simple3 | 1.945 | 1.905 | 1.940 | 2.518 | 0.98 |
| WideExpression10 | 4.114 | 3.980 | 3.777 | 5.426 | 0.97 |

BenchmarkDotNet reported `Allocated = 0 B` for the shown measured execution methods.

## What is measured

The public external arithmetic suite measures repeated invocation of already prepared artifacts:

- `CSharp_NoInliningMethod`;
- `DynamicExpresso_CompiledDelegate`;
- `NCalc_CompiledLambda`;
- `Wist_Cil_DynamicMethodFastInvoker`.

Parsing, compilation, dialect composition, and host setup occur outside the benchmark method.

## What is not proven

These results do not prove:

- that Wist is always faster than C# or other engines;
- that parsing or compilation is free;
- that control flow, calls, or arbitrary DSL programs have the same ratio;
- that the same nanosecond-level result will reproduce on every CPU or runtime build;
- that restricted dialects are security sandboxes.

The specialization architecture extends beyond arithmetic, but the published `within 10%` number is attached only to the listed arithmetic hot-execution scenarios.

## Reproduction command

Run from the repository root after a clean Release build:

```bash ci-run=false
dotnet run -c Release \
  --project UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -- \
  --filter "*External*ExecutionBenchmarks"
```

Before publishing a new result, record the exact Git commit, SDK/runtime, OS, CPU, and preserve the generated files under `BenchmarkDotNet.Artifacts/results/`.
