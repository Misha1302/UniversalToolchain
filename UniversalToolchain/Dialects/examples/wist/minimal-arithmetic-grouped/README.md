# Minimal arithmetic grouped dialect

## What this example demonstrates

This example shows the smallest useful arithmetic-oriented dialect composition using a dialect group.

`ArithmeticCore` is a compile-time group that expands into the concrete arithmetic module aliases before runtime selection.

## Enabled modules/backends/features

- Groups: `ArithmeticCore`
- Additional modules: `Scopes`
- Expanded modules: `Arithmetic`, `Numbers`, `Whitespaces`, `Scopes`
- Backend: `interpreter`
- Enabled optimizer flags: none

## Exact CLI commands to run it

From repository root:

```bash ci-timeout=240
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-grouped/dialect.wistdialect
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-grouped/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-grouped/program.wist --mode interpreter
```

## Expected behavior/result

`program.wist` evaluates `2 + 3 * 4` and returns `14`.

## Why this example exists

It verifies that groups improve dialect ergonomics without becoming runtime components.
Runtime activation remains manifest-backed and selected from the normalized build plan.
