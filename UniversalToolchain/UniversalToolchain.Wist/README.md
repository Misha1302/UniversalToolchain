# UniversalToolchain.Wist

A Wist-first .NET DSL/runtime facade for evaluating restricted formulas and compiling typed fast functions.

This package is the intended first-contact API for .NET developers. It hides the compiler pipeline, dialect runtime
host, manifests, `DynamicMethod`, `IAbstractIR`, and session APIs behind a small facade.

## Install

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-preview.1
```

## One-off execution

Use `Evaluate` when a formula is executed rarely or when onboarding matters more than hot-path speed.

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

double result = wist.Evaluate<double>(
    "price * 0.9 + fee",
    new
    {
        price = 100.0,
        fee = 5.0
    });
```

`Evaluate` is intentionally convenient. It may inspect anonymous objects, map argument names, and run through the
convenience execution path.

## Fast execution

Use `CompileFunc` when the same formula is executed many times.

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

var formula = wist.CompileFunc<double, double, double>(
    "price * 0.9 + fee",
    "price",
    "fee");

for (var i = 0; i < prices.Length; i++)
{
    results[i] = formula.Invoke(prices[i], fees[i]);
}
```

`CompileFunc` compiles once and returns a typed function. The hot `Invoke` path does not use dictionaries,
anonymous-object reflection, session setup, backend strings, or boxing for typed primitive arguments.

## Rule of thumb

```text
Use Evaluate for one-off execution.
Use CompileFunc for hot paths.
Compilation is expensive.
Invocation is fast.
```

Avoid this in production hot loops:

```csharp
for (var i = 0; i < prices.Length; i++)
{
    results[i] = wist.Evaluate<double>(
        "price * 0.9 + fee",
        new { price = prices[i], fee = fees[i] });
}
```

Prefer this:

```csharp
var formula = wist.CompileFunc<double, double, double>(
    "price * 0.9 + fee",
    "price",
    "fee");

for (var i = 0; i < prices.Length; i++)
{
    results[i] = formula.Invoke(prices[i], fees[i]);
}
```

## Presets

```csharp
WistEngine.CreateSafeFormulas();
WistEngine.CreateBusinessRules();
WistEngine.CreateTrusted();
```

`CreateSafeFormulas` is the recommended first-contact preset for restricted formula scenarios.

`CreateTrusted` enables the trusted Wist profile and must not be used for untrusted input.

## Security note

Restricted presets limit the selected language surface. They are not a hardened sandbox for arbitrary untrusted code.
Treat untrusted script execution as high risk and isolate it at the process/environment level when needed.

## Current scope

This initial facade intentionally exposes only:

- convenience `Evaluate<T>`;
- validation without throwing;
- typed fast `CompileFunc` overloads for one, two, and three arguments.

Wider arities and reusable object/session-based compiled artifacts can be added after the first API shape is validated.


Use `WistEngine` for application-level formula execution.
Use `WistRuntimeFacadeBuilder` only when working with lower-level Wist runtime or dialect integration scenarios.
