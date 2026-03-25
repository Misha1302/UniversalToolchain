# Full default native-style dialect

## What this example demonstrates
This example shows a practical default profile that keeps the same language-level surface as `full-default`, but swaps to the native arithmetic/type stack and enables native optimizations.

## Enabled modules/backends/features
- Modules: `BooleanConditions`, `Comments`, `ComparisonConditions`, `Conditions`, `CSharpInterop`, `Equality`, `Identifier`, `Labels`, `Loops`, `NativeTypes`, `Scopes`, `SemicolonAsNewLine`, `Variables`, `Whitespaces`
- Backends: `cil`, `interpreter`
- Enabled optimizer flags: `ArithmeticOptimization`, `EGraphOptimization`, `LocalVariablesOptimization`, `NativeCilOptimization`, `NativeTypesOptimization`

## Intentionally excluded capabilities
- `Arithmetic` + `Numbers` are excluded to avoid mixing the standard arithmetic stack with `NativeTypes`.
- No security/capability directives are used in this runnable example.

## Exact CLI commands to run it
From repository root:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/full-default-native/dialect.wistdialect
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default-native/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default-native/program.wist --mode interpreter
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/full-default-native/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/full-default-native/program.wist --mode compiler
```

## Expected behavior/result
`program.wist` computes the sum from 1 to 5 and returns `15`.

## Why this example exists
It provides a dedicated native arithmetic baseline for dialect composition checks without conflating it with the standard arithmetic stack.
