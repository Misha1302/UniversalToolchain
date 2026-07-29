---
title: First Program
description: Run the smallest useful Wist program from a .NET host or through the CLI.
---

# First Program

This page shows the shortest practical checks for Wist:

- package-first usage from a .NET console application;
- CLI execution from a repository checkout.

## Package-first .NET example

Install the published package first:

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-alpha.4 --source ./artifacts/packages
```

For a clean-room NuGet.org check, set `PUBLISHED_WIST_VERSION` to the version shown on the package page and run `./Tools/smoke-published-wist-package.sh "$PUBLISHED_WIST_VERSION"` from a repository checkout.

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

This is the normal alpha.1 first-contact path for application developers. `Compile<TDelegate>` compiles once and returns a typed program whose delegate can be invoked repeatedly.

## Trusted C# interop example

Enable CLR interop only for trusted source code controlled by the host application, and expose each host assembly explicitly.

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

This example uses C# interop and therefore belongs to the trusted profile, not to the restricted arithmetic profile.

## Repository CLI check

Read this section when you have cloned the repository and want to validate the CLI/runtime path.

### 1. Run the CIL mode quick start

From the repository root:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend cil
```

Expected output:

```text
12
```

`cil` is the user-facing backend alias that selects the CIL backend when the active dialect exposes the CIL backend.

### 2. Run the same expression through the interpreter

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend interpreter
```

Expected output:

```text
12
```

The interpreter path is useful when validating semantic parity. If a selected dialect does not expose `interpreter`, Wistc will reject that backend alias.

### 3. Run the pricing demo

```bash
dotnet run --project UniversalToolchain/Example/Example.csproj
```

The pricing demo runs a pricing formula through hardcoded C# logic, the shipped `full-default-native` Wist preset and the shipped `pricing-restricted` dialect. It also demonstrates compiler, interpreter and fast native invocation paths.

## What happened internally

For the simple expression, the runtime path is:

```text
source -> parser -> AST -> bytecode/AIR -> selected backend -> result
```

For compiled formulas, the important distinction is:

```text
cold path: source -> parse -> compile
hot path: typed function -> Invoke(arg0, arg1, ...)
```

The same source should produce the same observable result in compiler and interpreter modes when both modes are available. This parity is one of the main correctness expectations for Wist backends.

## Common mistakes

- Installing the package into a project that does not target `net10.0`.
- Exposing CLR assemblies to untrusted user input.
- Expecting `System.Math.Sqrt(...)` interop to work without adding `typeof(Math).Assembly` to `AllowedAssemblies`.
- Running repository CLI commands from a subdirectory, causing project paths to fail.
- Using `--backend cil` with a dialect that exposes only `interpreter`.
- Assuming all dialects expose all Wist syntax. Syntax exists only when the owning module is selected.
- Treating restricted dialects as security sandboxes. They restrict composition, but they are not hardened process sandboxes.

## Next

Continue with the [Use-case Recipes](/start/use-case-recipes) for three copy-ready application scenarios. Read the [CLI Reference](/start/cli-reference) for the command surface, or continue with the [Mental Model](/start/mental-model) and the [Wist Syntax Tour](/wist/syntax-tour).
