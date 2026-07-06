# UniversalToolchain Benchmarks

This project is a measurement surface, not marketing proof.

## Suites

| Suite | What it measures | What it deliberately excludes |
|---|---|---|
| `FormulaHotPathBenchmarks` | Steady-state invocation of already prepared formula artifacts. | Engine creation, parsing, dialect composition, compilation. |
| `FormulaConvenienceBenchmarks` | Public `Evaluate` convenience overhead with dictionary arguments. | Hot-path execution claims. |
| `FormulaCompilationBenchmarks` | Wist engine creation and formula compilation costs. | Runtime throughput claims. |

## Rules

- Every benchmark suite performs parity checks before BenchmarkDotNet starts measuring.
- Hot-path benchmarks use `OperationsPerInvoke` so one reported operation means one logical formula evaluation.
- Do not compare cold/convenience numbers with hot compiled delegate numbers as if they measured the same thing.
- Do not claim that Wist is generally faster than C# from these benchmarks.
- Preserve raw BenchmarkDotNet artifacts with the commit SHA, SDK/runtime, CPU, OS and working tree state before publishing numbers.

## Smoke

```bash
unset PLATFORM
export DOTNET_CLI_HOME="$PWD/.dotnet-home"
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

## Full run

```bash
unset PLATFORM
export DOTNET_CLI_HOME="$PWD/.dotnet-home"
dotnet run -c Release \
  --project UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -- \
  --filter "*Formula*"
```
