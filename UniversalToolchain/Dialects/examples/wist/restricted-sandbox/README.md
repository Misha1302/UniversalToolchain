# Restricted sandbox dialect

## What this example demonstrates
This example demonstrates constrained composition aimed at a narrower runtime profile.

## Enabled modules/backends/features
- Modules: `Arithmetic`, `Numbers`, `Scopes`, `Whitespaces`
- Backend: `interpreter`

## Intentionally excluded capabilities
- No compiler backend.
- No variables/identifiers, loops, labels, conditions, or interop-related capabilities.
- No claim of complete hardened sandboxing.

## Exact CLI commands to run it
From repository root:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/dialect.wistdialect
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/program.wist --mode interpreter
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/restricted-sandbox/forbidden-program.wist --mode interpreter
```

## Expected behavior/result
- `program.wist` returns `42`.
- `forbidden-program.wist` is expected to fail because variable declarations are excluded from this dialect composition.

## Why this example exists
It documents and tests how the dialect path can intentionally constrain available modules. It is a composition constraint example, not a fully hardened sandbox guarantee.
