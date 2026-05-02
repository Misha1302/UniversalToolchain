# Minimal native arithmetic dialect

## What this example demonstrates

This is the smallest native arithmetic-oriented dialect composition in the repository.

## Enabled modules/backends/features

- Modules: `NativeTypes`, `Numbers`, `Scopes`, `Whitespaces`
- Backend: `cil`
- Enabled optimizer flags: `ArithmeticOptimization`, `EGraphOptimization`, `NativeCilOptimization`,
  `NativeTypesOptimization`
- User-facing CLI mode: `--backend compiler` selects the canonical `cil` backend.

## Intentionally excluded capabilities

- No interpreter backend.
- No variables/identifiers, loops, labels, or condition modules.
- No optimizer flags beyond default behavior of selected modules.

## Exact CLI commands to run it

From repository root:

```bash ci-timeout=240
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-native/dialect.wistdialect
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-native/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-native/program.wist --backend compiler
```

## Expected behavior/result

`program.wist` evaluates `2 + 3 * 4` and returns `14`.

## Why this example exists

It is a minimal end-to-end composition for arithmetic parsing/evaluation and a compact smoke-test target for the dialect
runtime path.
