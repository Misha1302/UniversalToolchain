# Full default native-style dialect

## What this example demonstrates

This example shows a practical default profile that keeps the same language-level surface as `full-default`, but swaps
to the native arithmetic/type stack and enables native optimizations.

## Enabled modules/backends/features

- Modules: `BooleanConditions`, `Comments`, `ComparisonConditions`, `Conditions`, `CSharpInterop`, `Equality`,
  `Identifier`, `Labels`, `Loops`, `NativeTypes`, `Scopes`, `SemicolonAsNewLine`, `Variables`, `Whitespaces`
- Backends: `cil`, `interpreter`
- Enabled optimizer flags: `ArithmeticOptimization`, `BooleanOptimization`, `ComparisonIntrinsicOptimization`,
  `EGraphOptimization`, `LocalVariablesOptimization`, `NativeCilOptimization`, `NativeTypesOptimization`

## Intentionally excluded capabilities

- `Arithmetic` + `Numbers` are excluded to avoid mixing the standard arithmetic stack with `NativeTypes`.
- Declares `security trusted` and `capability unsafe-interop` to make trusted intent explicit.

## Exact CLI commands to run it

From repository root:

```bash
dotnet run --project apps/Wist.Cli/Wist.Cli.csproj -- dialect-inspect --file samples/dialects/wist/full-default-native/dialect.wistdialect
dotnet run --project apps/Wist.Cli/Wist.Cli.csproj -- run --dialect-file samples/dialects/wist/full-default-native/dialect.wistdialect --file samples/dialects/wist/full-default-native/program.wist --mode interpreter
dotnet run --project apps/Wist.Cli/Wist.Cli.csproj -- run --dialect-file samples/dialects/wist/full-default-native/dialect.wistdialect --file samples/dialects/wist/full-default-native/program.wist --mode compiler
```

## Expected behavior/result

`program.wist` uses native numeric literals and returns `15`.

## Why this example exists

It provides a dedicated native arithmetic baseline for dialect composition checks without conflating it with the
standard arithmetic stack.
