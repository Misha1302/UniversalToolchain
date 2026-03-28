# Minimal arithmetic dialect

## What this example demonstrates

This is the smallest useful arithmetic-oriented dialect composition in the repository.

## Enabled modules/backends/features

- Modules: `Arithmetic`, `Numbers`, `Scopes`, `Whitespaces`
- Backend: `interpreter`

## Intentionally excluded capabilities

- No compiler backend.
- No variables/identifiers, loops, labels, or condition modules.
- No optimizer flags beyond default behavior of selected modules.

## Exact CLI commands to run it

From repository root:

```bash
dotnet run --project apps/Wist.Cli/Wist.Cli.csproj -- dialect-inspect --file samples/dialects/wist/minimal-arithmetic/dialect.wistdialect
dotnet run --project apps/Wist.Cli/Wist.Cli.csproj -- run --dialect-file samples/dialects/wist/minimal-arithmetic/dialect.wistdialect --file samples/dialects/wist/minimal-arithmetic/program.wist --mode interpreter
```

## Expected behavior/result

`program.wist` evaluates `2 + 3 * 4` and returns `14`.

## Why this example exists

It is a minimal end-to-end composition for arithmetic parsing/evaluation and a compact smoke-test target for the dialect
runtime path.
