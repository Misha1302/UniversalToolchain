# UniversalToolchain.Wist

**Tiny controlled rules for .NET applications.**

`UniversalToolchain.Wist` is the first-contact package for UniversalToolchain. It gives .NET applications a small facade for restricted formula execution without exposing the lower-level compiler pipeline, dialect host, manifests, `DynamicMethod`, AIR, or session APIs.

Use it when configuration starts turning into logic:

```text
admin / config / LLM suggestion
        -> tiny rule text
        -> restricted formula surface
        -> validation or rejection
        -> typed compiled delegate for hot paths
        -> your application decides the side effect
```

## Install

The package metadata in this repository is prepared for `UniversalToolchain.Wist` `0.1.0-preview.2`.

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-preview.2
```

Requirements:

- target framework: `net10.0`;
- .NET SDK `10.0.103` or a compatible prerelease SDK accepted by the repository `global.json`.

## 30-second example

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateSafeFormulas();

var rolloutScore = rules.Compile<Func<double, double, double, double>>(
    "usage * 0.7 + reliability * 0.3 - incidents * 15.0",
    "usage",
    "reliability",
    "incidents");

double score = rolloutScore.CompiledDelegate(100.0, 90.0, 1.0);
bool enableNewDashboard = score >= 80.0;
```

The formula returns data. The host application performs the action.

## Validation without throwing

Use `Validate` before storing or executing user/admin/config-supplied formulas:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateSafeFormulas();

var validation = rules.Validate(
    "let score = usage * 0.7\nscore",
    new
    {
        usage = 100.0,
        reliability = 90.0,
        incidents = 1.0
    });

if (!validation.IsValid)
{
    Console.WriteLine(validation.Message);
}
```

The current safe-formula preset starts narrow. Statement-style bindings such as `let` are rejected by that restricted surface.

## Hot path vs convenience path

Use `Evaluate<T>` for one-off previews, admin tools, tests, and non-hot paths:

```csharp
double score = rules.Evaluate<double>(
    "usage * 0.7 + reliability * 0.3",
    new
    {
        usage = 100.0,
        reliability = 90.0
    });
```

Use `Compile<TDelegate>` when the same formula is invoked repeatedly:

```csharp
var formula = rules.Compile<Func<double, double, double>>(
    "usage * weight",
    "usage",
    "weight");

for (var i = 0; i < usages.Length; i++)
{
    results[i] = formula.CompiledDelegate(usages[i], weights[i]);
}
```

Rule of thumb:

```text
Use Evaluate for one-off execution.
Use Compile<TDelegate> for hot paths.
Compilation is expensive.
Invocation is the performance-oriented path.
```

## Presets

```csharp
WistEngine.CreateRestrictedArithmetic();
WistEngine.CreateFullNativePreview();

// Compatibility aliases:
WistEngine.CreateSafeFormulas();
WistEngine.CreateBusinessRules();
WistEngine.CreateTrusted();
```

`CreateRestrictedArithmetic` is the recommended first-contact preset for restricted formulas. `CreateSafeFormulas` remains a compatibility alias for it.

`CreateFullNativePreview`, `CreateBusinessRules`, and `CreateTrusted` are broad trusted-preview entry points in this preview. Do not use them for arbitrary untrusted input.

## Security and trust

Restricted presets limit the selected language/runtime surface. They are not hardened sandboxes for arbitrary untrusted code. Compiled execution is a performance feature, not a sandbox boundary. Treat untrusted script execution as high risk and isolate it at the process/environment level when needed.

## Current preview scope

This facade currently exposes:

- convenience `Evaluate<T>`;
- non-throwing `Validate`;
- typed fast `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- typed fast `CompileFunc` compatibility overloads for one, two, and three arguments;
- backend-neutral compiled program metadata.

The larger direction is controlled application DSLs for .NET. The current stable preview claim is restricted numeric/formula execution, validation, and typed compiled invocation for supported shapes.
