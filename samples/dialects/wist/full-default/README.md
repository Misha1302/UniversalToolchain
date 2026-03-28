# Full default-style dialect

## What this example demonstrates

This example shows a practical default runtime profile expressed via dialect DSL, close to the default Wist feature set
used in regular execution.

## Enabled modules/backends/features

- Modules: `Arithmetic`, `BooleanConditions`, `Comments`, `ComparisonConditions`, `Conditions`, `CSharpInterop`,
  `Equality`, `Identifier`, `Labels`, `Loops`, `Numbers`, `Scopes`, `SemicolonAsNewLine`, `Variables`, `Whitespaces`
- Backends: `cil`, `interpreter`
- Enabled optimizer flags: `BooleanOptimization`, `ComparisonIntrinsicOptimization`, `LocalVariablesOptimization`

## Intentionally excluded capabilities

- Native arithmetic/type stack (`NativeTypes`) and native optimizer set are intentionally not part of this default
  profile.
- Declares `security trusted` and `capability unsafe-interop` to make trusted intent explicit.

## Exact CLI commands to run it

From repository root:

```bash
dotnet run --project apps/Wist.Cli/Wist.Cli.csproj -- dialect-inspect --file samples/dialects/wist/full-default/dialect.wistdialect
dotnet run --project apps/Wist.Cli/Wist.Cli.csproj -- run --dialect-file samples/dialects/wist/full-default/dialect.wistdialect --file samples/dialects/wist/full-default/program.wist --mode interpreter
dotnet run --project apps/Wist.Cli/Wist.Cli.csproj -- run --dialect-file samples/dialects/wist/full-default/dialect.wistdialect --file samples/dialects/wist/full-default/program.wist --mode compiler
```

## Expected behavior/result

`program.wist` computes the sum from 1 to 5 and returns `15`.

## Why this example exists

It provides a realistic baseline dialect profile for validating DSL composition against both execution backends.
