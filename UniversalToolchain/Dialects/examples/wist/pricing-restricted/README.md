# Pricing restricted dialect

## What this example demonstrates

This example demonstrates a composition-constrained pricing profile. It keeps only the runtime surface needed for simple
formula evaluation over host-provided pricing inputs and native numeric values.

## Enabled modules/backends/features

- Modules: `Identifier`, `NativeTypes`, `Scopes`, `Variables`, `Whitespaces`
- Backends: `cil`, `interpreter`
- Enabled optimizer flags: `ArithmeticOptimization`, `EGraphOptimization`, `NativeCilOptimization`,
  `NativeTypesOptimization`
- User-facing CLI mode: `--backend compiler` selects the canonical `cil` backend.
- Security declaration: `security restricted`

## Intentionally excluded capabilities

- No `Arithmetic`/`Numbers` standard arithmetic stack; this profile uses `NativeTypes`.
- No `BooleanConditions`, `ComparisonConditions`, `Conditions`, `Equality`, `Loops`, or `Labels`.
- No comments, semicolon-as-new-line statement syntax, `ParametersSetter`, or C# interop.
- No hardened sandbox guarantee. This is a restricted runtime surface created by composition.

## Exact CLI commands to run it

From repository root:

```bash ci-timeout=240
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- dialect-inspect --file UniversalToolchain/Dialects/examples/wist/pricing-restricted/dialect.wistdialect
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/pricing-restricted/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/pricing-restricted/program.wist --backend interpreter
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --dialect-file UniversalToolchain/Dialects/examples/wist/pricing-restricted/dialect.wistdialect --file UniversalToolchain/Dialects/examples/wist/pricing-restricted/program.wist --backend compiler
```

## Expected behavior/result

`program.wist` evaluates:

```text
100.0 * 0.9 + 5.0
```

and returns `95`.

Programmatic host usage can provide external pricing inputs such as:

```text
price * 0.9 + fee
```

```text
(price + fee) * 0.95
```

```text
price - discount
```

## Why this exists

Pricing formulas are a compact example of keeping host-specific logic configurable while excluding unrelated language
capabilities by dialect composition. The profile documents composition constraints, not hardened sandboxing.
