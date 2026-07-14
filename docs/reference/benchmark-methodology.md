---
title: Benchmark Methodology
description: Reproducible benchmark rules and claim boundaries.
---

# Benchmark Methodology

Benchmark results are engineering evidence, not marketing copy.

## Required Before Publishing

Record:

- repository archive or commit SHA;
- clean/dirty working-tree state;
- .NET SDK/runtime version;
- OS and CPU;
- BenchmarkDotNet version/config;
- build configuration;
- warmup/iteration/launch settings;
- raw artifact path;
- correctness/parity check status.

## Current Suites

| Suite | Purpose | Valid claim boundary |
|---|---|---|
| `FormulaHotPathBenchmarks` | Steady-state invocation of prepared formula artifacts. | Hot execution only: no parse, no dialect composition, no compilation. |
| `FormulaConvenienceBenchmarks` | Public `Evaluate` convenience cost with dictionary arguments. | End-user convenience overhead only. Do not compare it to hot compiled delegates as execution-speed evidence. |
| `FormulaCompilationBenchmarks` | Engine creation and formula compilation cost. | Cold/warm compilation cost only. Do not use for throughput claims. |

## Measurement Rules

- Parity must be checked before measurement starts.
- A hot benchmark method that loops internally must set `OperationsPerInvoke` to the loop count.
- Hot execution, convenience execution and cold compilation must remain separate benchmark classes.
- Baselines must have the same workload shape and comparable call boundary.
- Report raw BenchmarkDotNet artifacts, not rounded tables alone.
- Treat the `Dry` job as discovery/runtime smoke only, never as performance evidence.

## Allowed Claims

Allowed:

> In the recorded prepared hot-execution scenario, the Wist compiled delegate invocation path was measured against the listed baseline under the listed environment.

Allowed:

> In the recorded convenience scenario, `Evaluate` includes per-call runtime overhead and should not be used as the hot path.

Forbidden:

> UniversalToolchain is faster than C#.

Forbidden:

> Alpha benchmark results prove production performance.

## Offline Restore

When a local NuGet global-packages archive is supplied, restore the benchmark project without relying on NuGet.org:

```bash ci-run=false
unset PLATFORM
export DOTNET_CLI_HOME="$PWD/.dotnet-home"
export NUGET_PACKAGES="/path/to/unpacked/packages"
dotnet restore UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  --source "$NUGET_PACKAGES" \
  --ignore-failed-sources \
  -p:Platform="Any CPU"
dotnet build UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -c Release \
  --no-restore \
  -p:Platform="Any CPU"
```

## Current Gap

The current public benchmark surface is intentionally small. Function-call-heavy, branch-heavy, allocation-heavy and
large-dialect scenarios need dedicated BenchmarkDotNet classes before they can support public performance claims.
