---
title: Custom Dialect Fast Invocation
description: Compile code from a custom .wistdialect once and invoke it repeatedly through reusable sessions or the low-level CIL DynamicMethod path.
---

# Custom Dialect Fast Invocation

This page explains how to get repeated fast invocation when a host application uses a custom `.wistdialect` file instead of a shipped `WistEngine` preset.

If you only need a shipped preset, prefer the high-level API:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateRestrictedArithmetic();

var formula = wist.Compile<Func<double, double, double>>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.CompiledDelegate(100.0, 5.0);
```

For custom dialects there is no `WistRuntimeFacade.Compile<TDelegate>(...)` method yet. The current practical path is to build a runtime from a dialect file, compile once, and reuse the compiled artifact. This is a lower-level alpha API for source/runtime authors; the stable first-contact facade remains `UniversalToolchain.Wist` plus shipped presets.

## Example dialect file

```text
dialect MyFastFormulaDialect
use Whitespaces,Numbers,Scopes,Arithmetic
backend cil,interpreter
```

The module list must include every syntax feature used by the source code.

## Build the runtime

```csharp
using UniversalToolchain.Dialects.Wist.Facade;

using var runtime = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithDialectFile("my-dialect.wistdialect")
    .Build();
```

`Run` is fine for one-off execution:

```csharp
var result = runtime.Run(
    "price * 0.9 + fee",
    new Dictionary<string, object?>
    {
        ["price"] = 100.0,
        ["fee"] = 5.0
    },
    backend: "cil");
```

Do not use `Run` inside a tight hot loop when the same code is executed repeatedly. Compile once instead.

## Recommended custom-dialect path: artifact sessions

This path avoids recompilation and does not depend on backend-specific output types.

```csharp
using BasicCore.Execution;
using UniversalToolchain.Dialects.Wist.Facade;

using var runtime = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithDialectFile("my-dialect.wistdialect")
    .Build();

var compiled = runtime.TryCompile(
    "price * 0.9 + fee",
    new Dictionary<string, Type>
    {
        ["price"] = typeof(double),
        ["fee"] = typeof(double)
    },
    backend: "cil");

if (!compiled.IsSuccess)
    throw compiled.Exception!;

var artifact = compiled.Artifact!;
var session = artifact.CreateSession();

double result = session.InvokeNamed<double>(
    new Dictionary<string, object?>
    {
        ["price"] = 100.0,
        ["fee"] = 5.0
    });
```

The artifact is the reusable compiled object. A session contains mutable argument values. Do not share the same mutable session between concurrent callers; create separate sessions for concurrent execution.

For sequential reuse, set arguments and run again:

```csharp
var session = artifact.CreateSession();

session.SetArgument("price", 100.0);
session.SetArgument("fee", 5.0);
double first = session.Run<double>();

session.SetArgument("price", 200.0);
session.SetArgument("fee", 10.0);
double second = session.Run<double>();
```

## Low-level CIL fast path

This section is an escape hatch for backend/runtime authors and performance investigations, not the package-first stable facade. Prefer artifact sessions unless you have verified the exact CIL artifact shape.

For the lowest-overhead CIL path, compile with the `cil` backend, cast the artifact to `ICompiledArtifact<DynamicMethod>`, and wrap the generated method in `DynamicMethodInvoker`.

```csharp
using System.Reflection.Emit;
using BasicCore.Compilation;
using DynamicMethodCalling.Core;
using UniversalToolchain.Dialects.Wist.Facade;

using var runtime = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithDialectFile("my-dialect.wistdialect")
    .Build();

var compiled = runtime.TryCompile(
    "price * 0.9 + fee",
    new Dictionary<string, Type>
    {
        ["price"] = typeof(double),
        ["fee"] = typeof(double)
    },
    backend: "cil");

if (!compiled.IsSuccess)
    throw compiled.Exception!;

var dynamicArtifact = (ICompiledArtifact<DynamicMethod>)compiled.Artifact!;
var dynamicMethod = dynamicArtifact.CompilationOutput;
EnsurePureExternalSignature(dynamicMethod, typeof(double), typeof(double));

var invoker = new DynamicMethodInvoker<double, double, double>(dynamicMethod);

double result = invoker.Invoke(100.0, 5.0);
```

Use this only when the generated `DynamicMethod` signature exactly matches the typed invoker. Some compiled shapes may require an execution-environment parameter before the external arguments. Artifact sessions handle that automatically; a raw `DynamicMethodInvoker<double, double, double>` does not.

Add a guard before constructing the invoker:

```csharp
using System.Reflection.Emit;

static void EnsurePureExternalSignature(DynamicMethod method, params Type[] expectedParameterTypes)
{
    var actualParameterTypes = method
        .GetParameters()
        .Select(static parameter => parameter.ParameterType)
        .ToArray();

    if (!actualParameterTypes.SequenceEqual(expectedParameterTypes))
    {
        var actual = string.Join(", ", actualParameterTypes.Select(static type => type.Name));
        var expected = string.Join(", ", expectedParameterTypes.Select(static type => type.Name));

        throw new InvalidOperationException(
            $"Compiled DynamicMethod signature does not match the requested fast invoker. Expected ({expected}), actual ({actual}). Use artifact sessions for this shape.");
    }
}
```

If this guard fails, use `artifact.CreateSession()` instead.

## Choosing the right path

| Scenario | Recommended API |
|---|---|
| Shipped preset, formula hot path | `WistEngine.Compile<TDelegate>` |
| Custom dialect, one-off execution | `runtime.Run(...)` |
| Custom dialect, repeated execution | `runtime.TryCompile(...)` + `artifact.CreateSession()` |
| Custom dialect, low-level CIL invocation | `ICompiledArtifact<DynamicMethod>` + `DynamicMethodInvoker`, after signature validation |

## Troubleshooting

If a module alias cannot be resolved, check that both files are copied to the application output directory:

```text
YourModule.dll
YourModule.dialect.runtime.json
```

If the cast to `ICompiledArtifact<DynamicMethod>` fails, check that you compiled with `backend: "cil"`. Do not use the `DynamicMethod` path with the interpreter backend.

If `Run` works but fast invocation fails, check that the selected dialect exposes the compiler backend and that the custom module supports the compiler path.

## Limitations

The CIL fast invoker path is not backend-agnostic. It assumes the compiler backend returns a `DynamicMethod` and that the generated method signature matches the typed invoker.

Fast compiled execution is not sandboxing. It improves hot-path throughput; it does not make untrusted code safe.

## Future API direction

The current code is usable but too low-level for most users. A future API should hide the `DynamicMethod` cast and expose a custom-dialect equivalent of `WistEngine.Compile<TDelegate>`:

```csharp
var formula = runtime.Compile<Func<double, double, double>>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.CompiledDelegate(100.0, 5.0);
```

Until that exists, use artifact sessions for normal custom-dialect reuse and the `DynamicMethod` path only as a low-level CIL escape hatch.

## Next

Continue with [Embedding in .NET](/build-dsls/embedding-dotnet), [Backend Selection](/build-dsls/backend-selection), and [Testing a DSL](/build-dsls/testing-dsl).
