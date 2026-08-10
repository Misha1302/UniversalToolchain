---
title: Backend Selection
description: Select the interpreter or CIL route through the canonical LanguagePlan runtime.
---

# Backend Selection

Wist currently exposes two canonical backend IDs:

- `interpreter`
- `cil`

Backend selection is part of the `LanguageDefinition` translated from `WistEngineOptions` or `.wistdialect` input. `LanguageCompiler` resolves exactly one backend route into `LanguagePlan`; `LanguageRuntime` executes that already-planned route without rediscovery.

## Public facade

```csharp
using UniversalToolchain.Wist;

using var interpreter = WistEngine.Create(new WistEngineOptions
{
    DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
    BackendId = "interpreter"
});

using var cil = WistEngine.Create(new WistEngineOptions
{
    DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
    BackendId = "cil"
});
```

A dialect file must explicitly enable the requested backend; otherwise planning fails closed.

## Choosing a backend

Use `interpreter` when startup simplicity and inspectable execution matter more than repeated-call throughput. Use `cil` when the same approved formula will be invoked repeatedly and typed compiled delegates are useful.

Do not infer backend availability from loaded assemblies, reflection metadata or DI registrations. A backend is first-class only when its typed package contribution can be selected into a deterministic `LanguagePlan` and executed through the generic `LanguageRuntime` route.

## Parity contract

When both backends are enabled, tests should compile both routes from the same semantic definition/plan basis and compare observable results. Backend-specific lowering may differ; language semantics must not.
