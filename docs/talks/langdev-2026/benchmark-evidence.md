# Benchmark evidence and claim boundary

## Current status

The old external/generated arithmetic benchmark suite has been retired. It mixed too much benchmark scaffolding with
public claims and was too easy to misread as a broad “Wist is near C#” statement.

The current benchmark surface is intentionally smaller and split by measurement story:

- `FormulaHotPathBenchmarks` — already prepared formula artifacts only;
- `FormulaConvenienceBenchmarks` — public `Evaluate` convenience overhead;
- `FormulaCompilationBenchmarks` — engine creation and formula compilation cost.

No historic table should be reused as current performance evidence unless the exact commit, raw BenchmarkDotNet artifacts,
SDK/runtime, CPU, OS and clean working-tree state are preserved next to the claim.

## Claim boundary

Allowed:

> In the recorded prepared hot-execution scenario, the Wist compiled delegate invocation path was measured against the listed baseline under the listed environment.

Forbidden:

> UniversalToolchain is faster than C#.

Forbidden:

> The alpha benchmark suite proves production performance.

## What is measured now

`FormulaHotPathBenchmarks` measures repeated invocation of already prepared artifacts:

- `CSharp_PreparedDelegate`;
- `NCalc_CompiledLambda`;
- `Wist_CompiledDelegate`;
- `Wist_CompiledDelegateFastInvoker`.

Parsing, compilation, dialect composition and host setup occur outside the hot benchmark method.

`FormulaConvenienceBenchmarks` intentionally measures per-call `Evaluate` overhead and must not be compared directly with
hot compiled delegate execution.

`FormulaCompilationBenchmarks` intentionally measures compilation/setup cost and must not be used for hot-throughput claims.

## Reproduction commands

Run from the repository root.

Smoke verification:

```bash ci-run=false
unset PLATFORM
dotnet restore UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj -p:Platform="Any CPU"
dotnet build UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj -c Release --no-restore -p:Platform="Any CPU"
dotnet run -c Release --no-build \
  --project UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -- \
  --self-test

dotnet run -c Release --no-build \
  --project UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -- \
  --job dry \
  --filter "*FormulaHotPathBenchmarks*"
```

Full public run:

```bash ci-run=false
unset PLATFORM
dotnet run -c Release \
  --project UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -- \
  --filter "*Formula*"
```

Before publishing a new result, record the exact Git commit, working-tree state, SDK/runtime, OS, CPU, and preserve the
generated files under `BenchmarkDotNet.Artifacts/results/`.
