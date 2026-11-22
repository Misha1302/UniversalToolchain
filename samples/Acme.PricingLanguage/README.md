# Acme.PricingLanguage

A real non-Wist external language sample. It owns its source syntax, typed syntax artifact,
interpreter route, delegate route named `compiled`, and route runtime. No project references Wist.

Run from the repository root; no packaged release artifacts are needed.

```bash
dotnet run --project samples/Acme.PricingLanguage/Acme.PricingLanguage.csproj
```

Expected output:

```text
35.0:35.0
```

## What the routes demonstrate

`PricingExpression.Compile() => Evaluate` returns a delegate bound to the existing evaluator. It does **not** generate CIL or machine code. Both routes ultimately call `Evaluate()`, so equal output is not independent evidence of interpreter/code-generator parity or speed.

The useful distinction is structural: the interpreter executor consumes `PricingExpression`, while the route named `compiled` first transforms that artifact into `Func<decimal>` and then invokes the selected delegate executor. This demonstrates typed route planning and exact executor selection without introducing a second compiler.
