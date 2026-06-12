# Benchmark evidence and claim boundary

## Claim used in the proposal

> In selected arithmetic hot-execution benchmarks, cached Wist CIL artifacts remain within 10% of a no-inlining C# baseline and allocate no memory during the measured execution phase.

This is deliberately narrower than saying that Wist is generally as fast as C#.

## Current verification runs

The current benchmark suite was executed with BenchmarkDotNet against the six public external arithmetic workloads.

The archived full run was produced from repository commit:

```text
705934e3bb8de35d5be257e77ff8ed68bea954f6
```

A subsequent run from a clean working tree at the LangDev materials commit reproduced the same rounded results:

```text
87952b9c77a91ebfb6deab2d953259798ae7d2e2
```

The clean confirmation result was reported by the project author after repeating the same full command without `ALLOW_DIRTY`. The raw archive preserved from the first full run has SHA-256:

```text
6cc06aed43a0f2e6a74f8d5f69dfc8522bdf8ce4b2faf1541fde42ed0d4a527f
```

Recorded environment:

- Fedora Linux 43;
- 12th Gen Intel Core i5-1235U;
- .NET SDK 10.0.108;
- .NET runtime 10.0.8;
- X64 RyuJIT x86-64-v3;
- BenchmarkDotNet 0.15.6;
- Concurrent Workstation GC.

| Workload | C# mean, ns/op | Wist CIL mean, ns/op | Wist/C# |
|---|---:|---:|---:|
| ConstantsHeavy6 | 2.709 | 2.770 | 1.02 |
| DeepChain6 | 2.665 | 2.775 | 1.04 |
| Medium8 | 2.881 | 3.105 | 1.08 |
| RepeatedSubexpressions5 | 2.094 | 2.124 | 1.01 |
| Simple3 | 1.963 | 1.957 | 1.00 |
| WideExpression10 | 4.186 | 4.134 | 0.99 |

The observed Wist/C# range in this run is approximately `0.99` to `1.08`. BenchmarkDotNet reported `Allocated = 0 B` for the shown measured execution methods.

The smoke run is not used as performance evidence. It uses BenchmarkDotNet's `Dry` job with one measured iteration and exists only to verify benchmark discovery and execution.

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

BenchmarkDotNet reported that it could not raise the benchmark process priority without additional permissions. The results are therefore presented as a reproducible local engineering measurement, not as an absolute cross-machine performance guarantee.

## Reproduction commands

Run from the repository root after a clean Release build.

Smoke verification:

```bash ci-run=false
dotnet run -c Release \
  --project UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -- \
  --job dry \
  --filter "*ExternalSimple3*"
```

Full public run:

```bash ci-run=false
dotnet run -c Release \
  --project UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -- \
  --filter "*External*ExecutionBenchmarks*"
```

Before publishing a new result, record the exact Git commit, working-tree state, SDK/runtime, OS, CPU, and preserve the generated files under `BenchmarkDotNet.Artifacts/results/`.
