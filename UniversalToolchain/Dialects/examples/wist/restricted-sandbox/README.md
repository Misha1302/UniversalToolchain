# Composition-restricted profile (`restricted-sandbox`)

## What this example demonstrates

This example demonstrates a composition-constrained profile aimed at a narrower runtime surface. The directory keeps the historical `restricted-sandbox` preset name for compatibility, but this is not a hardened sandbox.

## Enabled modules/backends/features

- Modules: `Arithmetic`, `BooleanConditions`, `Comments`, `ComparisonConditions`, `Conditions`, `Equality`, `Numbers`,
  `Scopes`, `Whitespaces`
- Backend: `interpreter`

## Intentionally excluded capabilities

- No compiler backend.
- Explicitly excludes `Variables`, `Identifier`, `Loops`, `Labels`, `InternalPreprocessorLexemes`,
  `ParametersSetter`, `SemicolonAsNewLine`, `CSharpInterop`, and `NativeTypes`.
- No hardened sandbox guarantee. The restriction comes from selected modules and backend composition.

## Exact CLI commands to run it

From repository root:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/dialect.wistdialect
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/program.wist --backend interpreter

# ci: expect-exit=1
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/forbidden-program.wist --backend interpreter

# ci: expect-exit=1
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/forbidden-interop.wist --backend interpreter
```

## Expected behavior/result

- `program.wist` returns `true` for a restricted expression-only check.
- `forbidden-program.wist` is expected to fail because variable declarations are excluded from this dialect composition.
- `forbidden-interop.wist` is expected to fail because C# interop is excluded.

## Why this example exists

It documents and tests how the dialect path can intentionally constrain available modules. It is a composition
constraint example, not a hardened sandbox guarantee.
