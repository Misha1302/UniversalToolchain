# UniversalToolchain.Wist

[![NuGet](https://img.shields.io/nuget/vpre/UniversalToolchain.Wist.svg)](https://www.nuget.org/packages/UniversalToolchain.Wist)
[![NuGet Downloads](https://img.shields.io/nuget/dt/UniversalToolchain.Wist.svg)](https://www.nuget.org/packages/UniversalToolchain.Wist)

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

After the preview package is published, install `UniversalToolchain.Wist` `0.1.0-alpha.1` from NuGet: <https://www.nuget.org/packages/UniversalToolchain.Wist/0.1.0-alpha.1>. Until that package exists, use a local package produced by `dotnet pack` or a source checkout.

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-alpha.1
```

Requirements:

- target framework: `net10.0`;
- .NET SDK `10.0.103` or a compatible prerelease SDK accepted by the repository `global.json`.

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
    Console.WriteLine(validation.Message);
}
```

The current restricted arithmetic preset starts narrow. Statement-style bindings such as `let` are rejected by that restricted surface.

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

## Experimental SSA route

SSA is opt-in and remains an experimental compiler feature. Enable it through the facade; a physical dialect file is not required:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.Create(new WistEngineOptions
{
    Preset = WistPreset.RestrictedArithmetic,
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
- `Prefer`: attempt SSA and return to the original AIR only for known unsupported-route diagnostics;
- `Require`: fail compilation when the supported SSA route cannot complete;
- `Debug`: behave like `Require` and retain detailed stage trace entries.

`TryCompile` and `Validate` preserve the optimization report even when `Require` or `Debug` fails. Unexpected optimizer defects never become a silent `Prefer` fallback.

The current SSA route is not a sandbox, not an SSA-native backend, and not a performance guarantee. It is a verifier-gated `AIR -> SSA -> AIR` optimization boundary for the currently supported subset.

## Presets

```csharp
WistEngine.CreateRestrictedArithmetic();
WistEngine.CreateFullNative();

using var trustedInterop = WistEngine.Create(new WistEngineOptions
{
    Preset = WistPreset.FullNative,
    AllowedAssemblies = [typeof(Math).Assembly]
});

```

`CreateRestrictedArithmetic` is the recommended first-contact preset for restricted formulas.

`CreateFullNative` selects the broad language profile, but it does not implicitly expose CLR assemblies. Add only reviewed assemblies through `AllowedAssemblies`. The facade deliberately does not expose ambiguous “safe”, “trusted”, or “business rules” aliases.

## Security and trust

Restricted presets limit the selected language/runtime surface. They are not hardened sandboxes for arbitrary untrusted code. Compiled execution is a performance feature, not a sandbox boundary. Treat untrusted script execution as high risk and isolate it at the process/environment level when needed.

## Current preview scope

This facade currently exposes:

- convenience `Evaluate<T>`;
- non-throwing `Validate`;
- typed fast `Compile<TDelegate>` and `TryCompile<TDelegate>`;
- backend-neutral compiled program metadata.

The larger direction is controlled application DSLs for .NET. The current stable preview claim is restricted numeric/formula execution, validation, and typed compiled invocation for supported shapes.

## Resource limits and diagnostics

`WistEngineOptions.ResourceLimits` enforces host-owned preflight limits for source length and parameter count. `Validate` and `TryCompile` return structured `WistDiagnostic` values with stable codes, severity, stage, span when available, message, and hints. These limits do not provide execution timeouts, memory quotas, or process isolation.

CLR interop and type directives resolve only against the shipped `BasicStdLib` assembly plus the immutable host allowlist in `WistEngineOptions.AllowedAssemblies`; dialect implementation assemblies, the AppDomain, and the output directory are not discovery sources.

Only the facade reference assembly under `ref/net10.0/UniversalToolchain.Wist.dll` is the supported compile-time API boundary. Its reviewed exported surface is recorded in `PublicAPI.Shipped.txt` in the source repository. The 64 assemblies under `lib/net10.0` form the runtime closure; all except the facade are implementation dependencies and are not compatibility promises.
