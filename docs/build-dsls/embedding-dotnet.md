---
title: Embedding in .NET
description: Use the canonical WistEngine facade from a .NET host.
---

# Embedding in .NET

Application code should enter Wist through `UniversalToolchain.Wist`. The facade constructs one typed `LanguageDefinition`, compiles it once with `LanguageCompiler`, creates one immutable `LanguagePlan`, and materializes one `LanguageRuntime` for that plan.

## Shipped preset

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateRestrictedArithmetic();

double result = wist.Evaluate<double>(
    "price * 0.9 + fee",
    new { price = 100.0, fee = 5.0 });
```

## Explicit preset or dialect file

```csharp
using UniversalToolchain.Wist;

using var preset = WistEngine.Create(WistEngineOptions.FromPresetId("pricing-restricted"));

using var custom = WistEngine.Create(new WistEngineOptions
{
    DialectSource = WistDialectSource.FromFile("my-language.wistdialect"),
    BackendId = "cil"
});
```

The dialect source configures the language definition; it does not create another planner or runtime-selection layer.

## Hot paths

Compile once and reuse the typed delegate:

```csharp
var formula = wist.Compile<Func<double, double, double>>(
    "price * factor",
    "price",
    "factor");

double value = formula.CompiledDelegate(100.0, 0.9);
```

Use `Evaluate<T>` for one-off execution and `Compile<TDelegate>` for repeated invocation.

## Host boundary

The host owns inputs, storage, timeouts/process isolation and side effects. Wist validates/executes the formula and returns data. `AllowedAssemblies` is an explicit CLR-interop allowlist; DI and runtime-manifest metadata are not security boundaries.
