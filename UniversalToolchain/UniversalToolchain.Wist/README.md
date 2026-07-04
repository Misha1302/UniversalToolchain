# UniversalToolchain.Wist

A compiler-first Wist facade for .NET formula execution.

This package is the intended first-contact API for .NET developers. It hides the compiler pipeline, dialect runtime host,
manifests, `DynamicMethod`, `IAbstractIR`, and session APIs behind a small facade.

Compiler-first. Interpreter-supported. `Compile<TDelegate>` is the primary hot-path API. `CompileFunc` remains as a
small compatibility convenience for one, two, and three arguments. `Evaluate` is the convenience one-off API.

## Requirements

- .NET SDK `10.0.103` or a compatible prerelease SDK selected by the repository `global.json`.
- Target framework: `net10.0`.

## Install

The package metadata in this repository is prepared for `UniversalToolchain.Wist` `0.1.0-preview.2`. This package-first
command works when that version is available from NuGet.org or another configured package source:

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-preview.2
```

## Fast execution: compile once, invoke many times

Use `Compile<TDelegate>` when the same formula is executed many times.

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

var formula = wist.Compile<Func<double, double, double>>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.CompiledDelegate(100.0, 5.0);
```

`Compile<TDelegate>` compiles once and returns a typed program with backend-neutral metadata. The hot delegate path does
not use dictionaries, anonymous-object reflection, session setup, backend strings, or boxing for typed primitive
arguments.

`CompileFunc` remains available for compatibility and small examples:

```csharp
var formula = wist.CompileFunc<double, double, double>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.Invoke(100.0, 5.0);
```

## One-off execution with Evaluate

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
convenience execution path. It is not the primary performance path.

## Validation without throwing

Use `Validate` when a UI, import flow, or configuration pipeline needs to check a formula before execution.

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

var validation = wist.Validate(
    "price * 0.9 + fee",
    new
    {
        price = 100.0,
        fee = 5.0
    });

if (!validation.IsValid)
{
    Console.WriteLine(validation.Message);
}
```

Use `TryCompile<TDelegate>` when compilation should return diagnostics-like result data instead of throwing:

```csharp
var compiled = wist.TryCompile<Func<double, double>>(
    "price *",
    "price");

if (!compiled.IsSuccess)
{
    Console.WriteLine(compiled.Message);
}
```

## Rule of thumb

```text
Use Evaluate for one-off execution.
Use Compile<TDelegate> for hot paths.
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
var formula = wist.Compile<Func<double, double, double>>(
    "price * 0.9 + fee",
    "price",
    "fee");

for (var i = 0; i < prices.Length; i++)
{
    results[i] = formula.CompiledDelegate(prices[i], fees[i]);
}
```

## Compiler backend and interpreter backend

The compiler backend is the default performance-oriented path for `CompileFunc` and convenience evaluation. The
interpreter backend remains important for diagnostics, debugging, fallback, education, semantic parity, and backend/module
development.

The interpreter is not the main performance claim.

## Performance model

Cold path:

```text
source -> parse -> bind -> runtime selection -> compile/execute
```

Hot path:

```text
compiled typed function -> Invoke(arg0, arg1, ...)
```

The performance claim belongs to compiled typed CIL-backed invocation. It does not apply to `Evaluate`, compile time,
every possible dialect, every module combination, or every backend.

Do not benchmark `Evaluate` inside a tight loop when evaluating runtime throughput. Benchmark compiled `Invoke`.

## Presets

```csharp
WistEngine.CreateSafeFormulas();
WistEngine.CreateBusinessRules();
WistEngine.CreateTrusted();
```

`CreateSafeFormulas` is the recommended first-contact preset for restricted formula scenarios.

In this preview, `CreateBusinessRules` is a product-oriented alias for the full native Wist profile rather than a separate
rules runtime.

`CreateTrusted` enables the trusted Wist profile and must not be used for untrusted input.

`Safe` means a restricted language/runtime surface. It does not mean arbitrary untrusted code is safe to execute inside
the current process.

## Security note

Restricted presets limit the selected language surface. They are not a hardened sandbox for arbitrary untrusted code.
Compiled execution is a performance feature, not a sandbox boundary. Treat untrusted script execution as high risk and
isolate it at the process/environment level when needed.

## Current preview scope

This initial facade intentionally exposes only:

- convenience `Evaluate<T>`;
- validation without throwing;
- typed fast `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- typed fast `CompileFunc` compatibility overloads for one, two, and three arguments;
- backend-neutral compiled program metadata.

Current preview contracts may change. Reusable object/session-based compiled artifacts, richer diagnostics, custom
function registration, dialect builder APIs, and lower-level pass authoring APIs can evolve after this facade shape is
validated.


Use `WistEngine` for application-level formula execution.
Use `WistRuntimeFacadeBuilder` only when working with lower-level Wist runtime or dialect integration scenarios.
