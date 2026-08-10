---
title: Custom Dialect Fast Invocation
description: Compile a custom .wistdialect once and invoke the typed delegate repeatedly.
---

# Custom Dialect Fast Invocation

A custom `.wistdialect` file uses the same canonical facade and runtime as shipped presets. There is no separate custom-dialect runtime host.

## Create from a dialect file

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.Create(new WistEngineOptions
{
    DialectSource = WistDialectSource.FromFile("my-language.wistdialect"),
    BackendId = "cil"
});
```

`WistFacadeLanguageDefinitionFactory` translates the dialect configuration into `LanguageDefinition`; `LanguageCompiler` remains the sole dependency/provider/order authority and produces the immutable `LanguagePlan` consumed by `LanguageRuntime`.

## Compile once

Use a delegate whose parameter list exactly matches the declared formula parameters:

```csharp
var formula = wist.Compile<Func<double, double, double, double>>(
    "price * factor + fee",
    "price",
    "factor",
    "fee");

for (var i = 0; i < 1000; i++)
{
    double result = formula.CompiledDelegate(100.0, 0.9, 5.0);
}
```

The compiled program is reusable. Do not rebuild a language plan or rediscover runtime components per invocation.

## Interpreter custom dialect

Set `BackendId = "interpreter"` when the dialect enables it. The selected backend still comes from the typed plan; changing loaded assemblies or runtime-manifest files cannot switch the backend after planning.

## Boundary

Runtime-manifest emission metadata may exist for tooling/package inspection, but custom-dialect execution must not depend on a reflection catalog, service locator, manual DLL copying or a second manifest planner.
