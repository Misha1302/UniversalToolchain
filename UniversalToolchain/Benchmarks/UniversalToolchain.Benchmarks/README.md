# UniversalToolchain benchmarks

This benchmark project is an engineering measurement surface, not a marketing claim generator.

The public benchmark suite is intentionally limited to hot execution speed of already compiled or prepared artifacts.
Wist is compared as an embeddable DSL/runtime, not as a replacement for handwritten C#.

## Public benchmark contract

Public results may be published only when:

- the benchmark is run from a clean `Release` build;
- the exact commit SHA is recorded;
- the .NET SDK/runtime, OS and CPU are recorded;
- raw BenchmarkDotNet artifacts are preserved;
- each compared engine evaluates the same formula;
- result parity is checked before measurements;
- batched benchmark methods set `OperationsPerInvoke`;
- parsing, compilation and host setup do not happen inside benchmark methods;
- legacy, generated, compilation and end-to-end benchmarks are not mixed into public execution-speed tables.

## What is measured

The external arithmetic benchmarks measure repeated invocation of already prepared artifacts:

- `CSharp_NoInliningMethod`;
- `DynamicExpresso_CompiledDelegate`;
- `NCalc_CompiledLambda`;
- `Wist_Cil_DynamicMethodFastInvoker`.

Preparation happens in `GlobalSetup`.
The benchmark method itself must not parse or compile source text.

## Current scenarios

- `ExternalSimple3ExecutionBenchmarks`;
- `ExternalMedium8ExecutionBenchmarks`;
- `ExternalDeepChain6ExecutionBenchmarks`;
- `ExternalRepeatedSubexpressions5ExecutionBenchmarks`;
- `ExternalConstantsHeavy6ExecutionBenchmarks`;
- `ExternalWideExpression10ExecutionBenchmarks`.

`ExternalWideExpression11ExecutionBenchmarks` is excluded for now because the current execution-bound native pointer API supports up to ten external arguments.

`ExternalExecutionBenchmarks/Unrolled/**` is excluded because generated unrolled benchmarks need a separate experimental contract.

`ArithmeticExecutionBenchmarks.cs` is excluded because it is a legacy internal comparison.

## Smoke command

```bash
dotnet run -c Release \
  --project UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -- \
  --job dry \
  --filter "*ExternalSimple3*"
```

## Full public execution-speed command

```bash
dotnet run -c Release \
  --project UniversalToolchain/Benchmarks/UniversalToolchain.Benchmarks/UniversalToolchain.Benchmarks.csproj \
  -- \
  --filter "*External*ExecutionBenchmarks"
```

Before publishing results, keep the generated files from:

```text
BenchmarkDotNet.Artifacts/results/
```

## Allowed claim

Good claim:

```text
In these arithmetic hot-execution scenarios, the Wist CIL backend runs a cached DynamicMethod artifact.
Its invocation cost is compared with a direct C# no-inlining baseline and with compiled artifacts from
DynamicExpresso and NCalc under the recorded environment.
```

Bad claim:

```text
Wist is faster than C#.
```
