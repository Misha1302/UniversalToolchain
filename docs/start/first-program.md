---
title: First Program
description: Run the smallest useful Wist program from a published package or repository checkout.
audience: wist-application-developer
status: current
lastVerifiedAgainst: wist-release-readiness-2026-08-18
---

# First Program

This page shows two independently supported checks:

- package-first usage with the version currently published on NuGet.org;
- CLI/source execution from a repository checkout.

## Package-first .NET example

<!-- wist-published-install:begin -->
Install the package version exercised by the clean-room NuGet.org smoke:

```bash ci-run=false
dotnet add package UniversalToolchain.Wist \
  --version 0.1.0-alpha.1 \
  --source https://api.nuget.org/v3/index.json
```
<!-- wist-published-install:end -->

Then run a simple formula through the public facade:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateRestrictedArithmetic();

var formula = wist.Compile<Func<double, double, double>>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.CompiledDelegate(100.0, 5.0);
Console.WriteLine(result); // 95
```

`Compile<TDelegate>` validates and compiles once. For expected invalid author input, use `Validate` or `TryCompile` and keep the last-known-good program active.

The repository source may define a newer candidate than the published package. That does not make the candidate installable from NuGet.org.

## Structured diagnostics for invalid formulas

For configuration or admin UIs, consume the stable public fields on `WistDiagnostic` instead of parsing exception text. This example intentionally submits statement-style syntax to the restricted arithmetic preset:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateRestrictedArithmetic();

var validation = wist.Validate(
    "let score = usage * 0.7\nscore",
    new { usage = 100.0 });

if (validation.IsValid)
{
    throw new InvalidOperationException("Expected the restricted formula to be rejected.");
}

foreach (var diagnostic in validation.Diagnostics)
{
    Console.WriteLine($"code: {diagnostic.Code}");
    Console.WriteLine($"severity: {diagnostic.Severity}");
    Console.WriteLine($"stage: {diagnostic.Stage}");
    Console.WriteLine($"source: {diagnostic.SourceName}");

    if (diagnostic.Span is { } span)
    {
        Console.WriteLine(
            $"span: {span.StartLine}:{span.StartColumn}-{span.EndLine}:{span.EndColumn}");
    }

    Console.WriteLine($"message: {diagnostic.Message}");
    foreach (var hint in diagnostic.Hints)
    {
        Console.WriteLine($"hint: {hint.Message}");
    }
}
```

A diagnostic may have no `Span`; treat location as optional in stored records and UI models. `Code`, `Severity`, `Stage`, `SourceName`, optional `Span`, `Message` and `Hints` are the public structured contract to preserve. Validation or policy rejection means the selected language surface rejected the input; it is **not** OS/process isolation and does not turn the preset into a security sandbox. See [Diagnostics](/reference/diagnostics) for the deeper contract.

## Trusted C# interop example

Enable CLR interop only for trusted source controlled by the host application, and expose each host assembly explicitly:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.Create(new WistEngineOptions
{
    DialectSource = WistDialectSource.FromShippedPreset("full-default-native"),
    AllowedAssemblies = [typeof(Math).Assembly]
});

var calcHypotenuse = wist.Compile<Func<double, double, double>>(
    "System.Math.Sqrt(x * x + y * y)",
    "x",
    "y");

double result = calcHypotenuse.CompiledDelegate(7.0, 24.0);
Console.WriteLine(result); // 25
```

This belongs to the trusted profile, not the restricted arithmetic profile.

## Repository CLI check

Read this section when you have cloned the repository and want to validate the current source/runtime path.

### 1. Run the CIL mode quick start

From the repository root:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend cil
```

Expected output:

```text
12
```

### 2. Run the same expression through the interpreter

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend interpreter
```

Expected output:

```text
12
```

The interpreter path is useful for semantic parity checks. A dialect that does not expose `interpreter` must reject that backend alias.

### 3. Run the pricing demo

```bash
dotnet run --project UniversalToolchain/Example/Example.csproj
```

The demo compares hardcoded C# logic, the shipped `full-default-native` Wist preset and the shipped `pricing-restricted` dialect across compiler, interpreter and prepared invocation paths.

## What happened internally

For the simple expression:

```text
source -> parser -> AST -> bytecode/AIR -> selected backend -> result
```

For compiled formulas:

```text
cold path: source -> parse -> compile
hot path: typed function -> Invoke(arg0, arg1, ...)
```

The same supported source should produce the same observable result in CIL and interpreter modes when both are available.

## Common mistakes

- installing a source-candidate version that is not published on NuGet.org;
- adding a local `./artifacts/packages` feed that has not been produced by the release package gate;
- installing into a project that does not target `net10.0`;
- exposing CLR assemblies to untrusted input;
- expecting `System.Math.Sqrt(...)` interop without adding `typeof(Math).Assembly` to `AllowedAssemblies`;
- running repository CLI commands from a subdirectory;
- assuming every dialect exposes every Wist syntax feature;
- treating restricted dialects as hardened process sandboxes.

## Next

Continue with [Use-case Recipes](/start/use-case-recipes), [CLI Reference](/start/cli-reference), [Mental Model](/start/mental-model) or the [Wist Syntax Tour](/wist/syntax-tour).
