---
title: Wist Facade API
description: Supported public entry points, result types, options and ownership rules for UniversalToolchain.Wist.
audience: wist-application-developer
status: current-alpha-reference
lastVerifiedAgainst: hardening-pr-360-2026-08-11
---

# Wist facade API

This page describes the supported compile-time facade exposed by `UniversalToolchain.Wist`. The current source candidate is `0.1.0-alpha.6`; the version currently verified from NuGet.org is listed in [Installation](/start/installation).

The authoritative exported surface is `UniversalToolchain/UniversalToolchain.Wist/PublicAPI.Shipped.txt`. Runtime implementation assemblies are not public compatibility contracts.

## Stability and package boundary

The facade types documented here are the intended application-facing surface for the named alpha candidate. Alpha versions may still make reviewed breaking changes; pin the exact package version and read its stability record before upgrading.

Only `ref/net10.0/UniversalToolchain.Wist.dll` is the supported compile-time contract. Assemblies carried under `lib/net10.0` are runtime implementation dependencies. Do not reference their types directly or treat their assembly count, names or layout as a stable application API.

## Create an engine

```csharp
using UniversalToolchain.Wist;

using var restricted = WistEngine.CreateRestrictedArithmetic();
using var fullNative = WistEngine.CreateFullNative();
```

`CreateRestrictedArithmetic()` is the recommended first-contact preset for restricted formulas.

`CreateFullNative()` selects the broader native profile, but it does not automatically expose host CLR assemblies. Use `WistEngine.Create(options)` when you need an explicit preset, backend, resource limits, host assembly allowlist, source-retention policy, diagnostics policy or experimental optimization route.

```csharp
using var engine = WistEngine.Create(new WistEngineOptions
{
    DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
    BackendId = "cil",
    ResourceLimits = new WistResourceLimits
    {
        MaxSourceLength = 16_384,
        MaxParameterCount = 16
    },
    SourceRetention = WistSourceRetentionPolicy.HashAndIdentity,
    DiagnosticExposure = WistDiagnosticExposure.Safe
});
```

Options are snapshotted when the engine is created. Changing the original options object later does not reconfigure an existing engine.

## Validate expected input failures without throwing

```csharp
var validation = engine.Validate(
    "price * discount + fee",
    new { price = 100.0, discount = 0.9, fee = 5.0 });

if (!validation.IsValid)
{
    Console.WriteLine(validation.FailureKind);
    foreach (var diagnostic in validation.Diagnostics)
        Console.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
}
```

Public overloads:

```csharp
WistValidationResult Validate(string code);
WistValidationResult Validate(string code, object sampleArguments);
```

`WistValidationResult` exposes:

- `IsValid`;
- `FailureKind` for expected failures;
- `Diagnostics`;
- `Exception` only according to `DiagnosticExposure`;
- `OptimizationReport`, including route information captured before a failure.

Structured non-throwing failures are limited to `UserInput`, `Policy` and `Unsupported`. `Infrastructure` and `Internal` are taxonomy values for fault ownership, but unexpected framework/infrastructure failures are fail-fast rather than converted to an ordinary invalid-formula result.

There is no aggregate `Message` property. Consume structured diagnostics instead of parsing exception text.

## Evaluate once

```csharp
double value = engine.Evaluate<double>(
    "price * discount + fee",
    new { price = 100.0, discount = 0.9, fee = 5.0 });
```

Public overloads:

```csharp
T Evaluate<T>(string code);
T Evaluate<T>(string code, object arguments);
T Evaluate<T>(string code, IReadOnlyDictionary<string, object?> arguments);
```

Use `Evaluate<T>` for one-off execution, diagnostics tools, tests and non-hot paths. It performs source processing for the call and throws when evaluation fails.

## Compile once, invoke repeatedly

```csharp
var program = engine.Compile<Func<double, double, double>>(
    "price * discount",
    "price",
    "discount");

double value = program.CompiledDelegate(100.0, 0.9);
```

Public methods:

```csharp
WistProgram<TDelegate> Compile<TDelegate>(
    string formula,
    params string[] parameterNames)
    where TDelegate : Delegate;

WistCompileResult<TDelegate> TryCompile<TDelegate>(
    string formula,
    params string[] parameterNames)
    where TDelegate : Delegate;
```

`Compile<TDelegate>` validates and compiles, then throws when the operation cannot complete. `TryCompile<TDelegate>` returns:

- `IsSuccess`;
- `Program` when successful;
- `FailureKind` for expected failures;
- `Diagnostics`;
- `Exception` according to `DiagnosticExposure`;
- `OptimizationReport`.

Unexpected infrastructure/internal faults are not reported as `IsSuccess == false`; they throw.

`WistProgram<TDelegate>` exposes the typed `CompiledDelegate` and `Metadata`. Metadata includes source retention/identity fields, backend, ordered parameter names and types, return type and optimization report.

The delegate signature and the ordered `parameterNames` must describe the same parameters. Do not cache or reuse a compiled program under a different parameter order, dialect, backend or options snapshot.

## Ownership and lifetime

`WistEngine` is disposable. `WistProgram<TDelegate>` is not a separate disposable owner.

Keep the engine alive for as long as any program compiled by that engine remains active:

```csharp
sealed class ActiveRule : IDisposable
{
    private readonly WistEngine _engine;

    public ActiveRule(string source)
    {
        _engine = WistEngine.CreateRestrictedArithmetic();
        Program = _engine.Compile<Func<double, double>>(source, "value");
    }

    public WistProgram<Func<double, double>> Program { get; }

    public void Dispose() => _engine.Dispose();
}
```

One `WistEngine` instance is intentionally non-concurrent. Overlapping public operations fail fast. Use a separate engine per concurrent operation stream; do not infer thread safety from generic `PerSession` lifetime. Disposal coordinates with an already admitted operation and rejects later operations.

See [Production Integration](/start/production-integration) and [Lifecycle, Concurrency and Privacy](/reference/lifecycle-concurrency-privacy).

## Engine options

`WistEngineOptions` exposes:

| Property | Meaning |
|---|---|
| `DialectSource` | Exact shipped preset, file or inline dialect source |
| `BackendId` | Canonical backend identifier, currently `cil` or `interpreter` when supported by the selected dialect |
| `AllowedAssemblies` | Immutable-at-creation host assembly allowlist for CLR interop and type directives |
| `ResourceLimits` | Host-owned source-length and parameter-count preflight limits |
| `Optimization` | Optional compiler optimization routes; SSA remains experimental |
| `SourceRetention` | `Full`, `HashAndIdentity` or `None` metadata retention policy |
| `DiagnosticExposure` | `Developer` compatibility mode or `Safe` expected-failure diagnostics |

Factory helpers:

```csharp
WistEngineOptions.FromPresetId("pricing-restricted");
WistEngineOptions.FromDialectFile("custom.wistdialect");
WistEngineOptions.FromDialectText(sourceText, "custom.wistdialect");
```

Dialect source helpers:

```csharp
WistDialectSource.FromShippedPreset("pricing-restricted");
WistDialectSource.FromFile("custom.wistdialect");
WistDialectSource.FromText(sourceText, "custom.wistdialect");
```

A backend alias that the selected dialect does not expose is rejected during engine creation.

## Source retention

`Full` is the alpha compatibility default. It preserves `SourceText` and also records `SourceSha256`/`SourceLength`.

`HashAndIdentity` drops `SourceText` while retaining SHA-256 and length. `None` drops raw source and hash while retaining length. SHA-256 is an identity aid, not secret scrubbing; lower compiler/runtime artifacts may still retain source-derived data.

## Diagnostic exposure

`Developer` is the alpha compatibility default for expected input/policy/unsupported failures and may expose the original expected exception object. `Safe` omits that object from `WistValidationResult`/`WistCompileResult` and bounds diagnostic messages.

Neither mode converts framework bugs into user validation failures. Safe mode is not a promise of cryptographic or semantic secret scrubbing.

## Resource limits

```csharp
var limits = new WistResourceLimits
{
    MaxSourceLength = 65_536,
    MaxParameterCount = 64
};
```

These are preflight limits only. They do not provide execution timeouts, memory quotas, cancellation or process isolation.

## Diagnostics

`WistDiagnostic` contains:

- stable `Code`;
- `Severity`;
- pipeline `Stage`;
- `SourceName` and `Span` when available;
- human-readable `Message`;
- structured `Hints`.

Use diagnostic codes plus `FailureKind` as compatibility keys. English message wording may become more precise during alpha. See [Diagnostics Reference](/reference/diagnostics).

## Experimental SSA options

SSA is an opt-in `AIR -> SSA -> AIR` route. It is not an SSA-native backend, a sandbox or a performance guarantee.

```csharp
using var engine = WistEngine.Create(new WistEngineOptions
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
```

Use the returned optimization report to determine whether SSA was used, whether the route fell back to AIR, which passes executed and which route diagnostics were produced.

## Trust boundary

Restricted presets reduce the selected language and runtime surface. They are not hardened in-process sandboxes for arbitrary hostile code. The host owns authorization, persistence, approval, side effects, secrets, time and memory limits, process isolation and rollback.
