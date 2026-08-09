# Minimal arithmetic grouped dialect

## What this example demonstrates

This example shows a small arithmetic dialect using a Wist group as source-level shorthand.

`ArithmeticCore` expands to concrete module aliases **before** Wist creates `LanguageDefinition`. `LanguageCompiler` then performs typed dependency closure and produces the only semantic `LanguagePlan`.

## Enabled modules/backends/features

- Group: `ArithmeticCore`
- Additional requested module: `Scopes`
- Group expansion: `Arithmetic`, `Numbers`, `Whitespaces`
- Backend: `interpreter`
- Enabled optimizer flags: none

The final plan may include typed dependencies required by those requested features; group expansion itself is not the planner.

## Exact CLI commands to run it

From repository root:

```bash ci-timeout=240
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-grouped/dialect.wistdialect
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-grouped/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-grouped/program.wist --backend interpreter
```

## Expected behavior/result

`program.wist` evaluates `2 + 3 * 4` and returns `14`.

## Why this example exists

It verifies that groups improve dialect ergonomics without becoming runtime components or a hidden composition owner. Runtime execution follows the canonical `LanguageDefinition -> LanguageCompiler -> LanguagePlan -> LanguageRuntime` path.
