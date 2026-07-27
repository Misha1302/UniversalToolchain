# FunctionCalls SafeMath dialect

## What this example demonstrates

This example demonstrates a neutral FunctionCalls + SafeMath profile without the removed rules feature. It shows how source-level function calls such as `clamp(...)` can execute through both the interpreter and CIL compiler paths while keeping the supported function set explicit.

## Enabled modules/backends/features

- Modules: `Arithmetic`, `BooleanConditions`, `Comments`, `ComparisonConditions`, `Conditions`, `Equality`, `FunctionCalls`, `Identifier`, `Numbers`, `SafeMathFunctions`, `Scopes`, `SemicolonAsNewLine`, `Variables`, `Whitespaces`
- Backends: `cil`, `interpreter`
- Optimizers: `BooleanOptimization`, `ComparisonIntrinsicOptimization`
- Security posture: `restricted`

## Exact CLI commands to run it

From repository root:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/function-calls-safe-math/dialect.wistdialect
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/function-calls-safe-math/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/function-calls-safe-math/program.wist --backend interpreter
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/function-calls-safe-math/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/function-calls-safe-math/program.wist --backend cil
```

## Expected behavior/result

`program.wist` computes:

```text
base = 100.0 * 3.0 = 300.0
discountValue = clamp(base * 0.15, 0.0, 50.0) = 45.0
result = base - discountValue = 255.0
```

Both supported backends should return `255` / `255.0` and stay semantically equivalent.

## Why this example exists

It protects the current function-call MVP after rule declarations were removed from this branch. The example should remain a pure dialect/runtime composition sample: no rule-local parser, no raw-source rule declaration scanner, and no hidden backend-specific function-call shortcut.
