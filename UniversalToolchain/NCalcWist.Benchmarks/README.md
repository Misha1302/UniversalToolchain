# NCalcWist.Benchmarks

BenchmarkDotNet suite focused on **NCalc vs Wist only** under layered, apples-to-apples conditions.

## Layers

- **Parse-only**: parse/build syntax representation only.
    - Wist: `ILexer.Lexemize` + `IParser.Parse`.
    - NCalc: `new Expression(...)` + parse forcing call (`GetParameterNames` / `HasErrors`).
- **Compile-only**: compile to executable form, no run.
    - Wist: `IExecutableGiver<DynamicMethod>.GetExecutable`.
    - NCalc: `Expression.ToLambda<...>()`.
- **Execute-only**: precompiled delegates/invokers created in setup; benchmark method only invokes.
- **Cold-start**: parse + compile + first execute in benchmark call.
- **Multi-thread execute-only**: one compiled expression shared across threads, fixed per-thread data slices.

## Fairness notes

- No dictionary/context allocations in benchmark hot paths.
- Expression strings are pre-generated once.
- All scenarios exist in all layers.
- Floating-point correctness checks use tolerance (`1e-9` relative).
- `ParameterHeavy20` uses 20 terms. Wist path uses pre-bound locals (`let`) because current high-performance
  `DynamicMethodInvoker` helpers in-repo are specialized for up to 11 arguments.

## NCalc default vs optimized parser mode

Two benchmark categories are provided:

- `NCalcDefault`
- `NCalcOptimized` (attempts `AppContext.SetSwitch("NCalc.EnableParlotParserCompilation", true)`).

Because this switch can be process-level, run categories in separate benchmark runs for strict isolation.

## Run

```bash
dotnet run -c Release --project UniversalToolchain/NCalcWist.Benchmarks -- --modulesPath=/path/to/wist/modules
```

Filter example:

```bash
dotnet run -c Release --project UniversalToolchain/NCalcWist.Benchmarks -- --filter "*ParseOnly*"
```

## Optional disassembly

Uncomment the `DisassemblyDiagnoser` line in `Program.cs`, then run normally.
