# UniversalToolchain.Wist

[![NuGet](https://img.shields.io/nuget/vpre/UniversalToolchain.Wist.svg)](https://www.nuget.org/packages/UniversalToolchain.Wist)
[![NuGet Downloads](https://img.shields.io/nuget/dt/UniversalToolchain.Wist.svg)](https://www.nuget.org/packages/UniversalToolchain.Wist)

**Tiny controlled rules for .NET applications.**

`UniversalToolchain.Wist` is the first-contact facade for restricted formula execution without exposing the lower-level compiler pipeline, dialect host, manifests, `DynamicMethod`, AIR or session APIs.

```text
admin / config / LLM suggestion
        -> tiny rule text
        -> restricted formula surface
        -> validation or rejection
        -> typed compiled delegate for hot paths
        -> your application decides the side effect
```

## Artifact identity

<!-- wist-source-candidate:begin -->
This README is embedded in the source candidate `UniversalToolchain.Wist` `0.1.0-alpha.6`. That candidate is **not published on NuGet.org**. Consume it only from the reviewed feed that contains this exact package artifact. For the version currently available from NuGet.org, use the [public installation guide](https://misha1302.github.io/Wist2/start/installation).
<!-- wist-source-candidate:end -->

Requirements:

- target framework: `net10.0`;
- .NET SDK `10.0.103` or a compatible SDK accepted by the repository `global.json`.

## 30-second example

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateRestrictedArithmetic();

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

using var rules = WistEngine.CreateRestrictedArithmetic();

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
    Console.WriteLine(string.Join("; ", validation.Diagnostics.Select(diagnostic => diagnostic.Message)));
}
```

The restricted arithmetic preset intentionally rejects statement-style bindings such as `let`. `Compile` also validates and throws on failure; use `Validate` or `TryCompile` when invalid author input is expected.

## Hot path vs convenience path

Use `Evaluate<T>` for one-off trial runs, admin tools, tests and non-hot paths:

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

```text
Use Evaluate for one-off execution.
Use Compile<TDelegate> for hot paths.
Compilation is expensive.
Invocation is the performance-oriented path.
```

## Experimental SSA route

SSA is opt-in and remains an experimental compiler feature:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.Create(new WistEngineOptions
{
    DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
    Optimization = new WistOptimizationOptions
    {
        Ssa = new WistSsaOptions
        {
            Policy = WistSsaPolicy.Prefer,
            DiagnosticLevel = WistSsaDiagnosticLevel.Detailed
        }
    }
});

var compiled = rules.Compile<Func<int, int>>("value * 2 + 3", "value");
int value = compiled.CompiledDelegate(20);

var report = compiled.Metadata.OptimizationReport.Ssa;
Console.WriteLine($"used={report.UsedSsa}, fallback={report.FellBackToAir}");
Console.WriteLine(string.Join(", ", report.ExecutedPasses));
```

Policy semantics:

- `Disabled`: do not attempt the SSA route;
- `Prefer`: attempt SSA and return to original AIR only for known unsupported-route diagnostics;
- `Require`: fail compilation when the supported SSA route cannot complete;
- `Debug`: behave like `Require` and retain detailed stage trace entries.

Unexpected optimizer defects never become a silent `Prefer` fallback. The SSA route is not a sandbox, an SSA-native backend or a performance guarantee; it is a verifier-gated `AIR -> SSA -> AIR` boundary for a supported subset.

## Presets

```csharp
WistEngine.CreateRestrictedArithmetic();
WistEngine.CreateFullNative();

using var trustedInterop = WistEngine.Create(new WistEngineOptions
{
    DialectSource = WistDialectSource.FromShippedPreset("full-default-native"),
    AllowedAssemblies = [typeof(Math).Assembly]
});
```

`CreateRestrictedArithmetic` is the recommended first-contact preset. `CreateFullNative` selects the broad language profile but does not implicitly expose CLR assemblies.

## Security and trust

Restricted presets limit the selected language/runtime surface. They are not hardened sandboxes for arbitrary untrusted code. Compiled execution is a performance feature, not a sandbox boundary. Isolate hostile execution at the process/environment level.

## Current alpha scope

The facade exposes:

- convenience `Evaluate<T>`;
- non-throwing `Validate`;
- typed `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- backend-neutral compiled program metadata;
- host-owned source-length and parameter-count preflight limits;
- structured diagnostics and opt-in SSA reports.

The larger direction is controlled application DSLs for .NET. The current claim is restricted numeric/formula execution, validation and typed compiled invocation for supported shapes.

CLR interop and type directives resolve only against the shipped `BasicStdLib` assembly plus the immutable host allowlist in `WistEngineOptions.AllowedAssemblies`; dialect implementation assemblies, the AppDomain and output directory are not discovery sources.

Only the facade reference assembly under `ref/net10.0/UniversalToolchain.Wist.dll` is the supported compile-time API boundary. Runtime-closure assemblies under `lib/net10.0` are implementation dependencies, not compatibility promises.
