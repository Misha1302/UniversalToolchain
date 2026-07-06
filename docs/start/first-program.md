---
title: First Program
description: Run the smallest useful Wist program from a .NET host or through the CLI.
---

# First Program

This page shows the shortest practical checks for Wist:

- package-first usage from a .NET console application;
- validation before execution;
- CLI execution from a repository checkout.

## Package-first .NET example

Install the package first:

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-preview.2
```

Then compile a small controlled formula through the public facade:

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

Console.WriteLine(score);              // 82
Console.WriteLine(enableNewDashboard); // True
```

This is the normal first-contact path for application developers: compile once, keep the returned typed function, and call the compiled delegate from the hot path.

## Validate before storing or executing rules

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

Console.WriteLine(validation.IsValid); // false
Console.WriteLine(validation.Message);
```

The restricted safe-formula preset does not enable statement-style bindings such as `let`. If your application needs them, choose or build a dialect that explicitly includes those capabilities.

## Trusted C# interop example

Use `CreateTrusted` only for trusted source code controlled by the host application.

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateTrusted();

var calcHypotenuse = wist.CompileFunc<double, double, double>(
    "System.Math.Sqrt(x * x + y * y)",
    "x",
    "y");

double result = calcHypotenuse.Invoke(7.0, 24.0);
Console.WriteLine(result); // 25
```

This example uses C# interop and therefore belongs to the trusted profile, not to the restricted safe-formula profile.

## Repository CLI check

Read this section when you have cloned the repository and want to validate the CLI/runtime path.

### 1. Run the compiler mode quick start

From the repository root:

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend compiler
```

Expected output:

```text
12
```

`compiler` is the user-facing backend alias that selects the CIL backend when the active dialect exposes the CIL backend.

### 2. Run the same expression through the interpreter

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --eval "(2 + 2) * 3" --backend interpreter
```

Expected output:

```text
12
```

The interpreter path is useful when validating semantic parity. If a selected dialect does not expose `interpreter`, Wistc will reject that backend alias.

### 3. Run the showcase demo

```bash
dotnet run --project UniversalToolchain/Example/Example.csproj
```

The showcase demo runs a user/LLM-suggested numeric decision formula through the safe Wist facade, compiles it into a typed delegate, and demonstrates restricted-surface rejection before execution.

## What happened internally

For the simple expression, the runtime path is:

```text
source -> parser -> AST -> bytecode/AIR -> selected backend -> result
```

For `Compile<TDelegate>`, the important distinction is:

```text
cold path: source -> parse -> compile
hot path: typed delegate -> Invoke(arg0, arg1, ...)
```

The same source should produce the same observable result in compiler and interpreter modes when both modes are available. This parity is one of the main correctness expectations for Wist backends.

## Common mistakes

- Installing the package into a project that does not target `net10.0`.
- Using `CreateTrusted` for untrusted user input.
- Expecting `System.Math.Sqrt(...)` interop to work in restricted safe-formula presets.
- Running repository CLI commands from a subdirectory, causing project paths to fail.
- Using `--backend compiler` with a dialect that exposes only `interpreter`.
- Assuming all dialects expose all Wist syntax. Syntax exists only when the owning module is selected.
- Treating restricted dialects as security sandboxes. They restrict composition, but they are not hardened process sandboxes.

## Next

Read the [Showcase](/start/showcase), the [CLI Reference](/start/cli-reference), the [Mental Model](/start/mental-model), or the [Wist Syntax Tour](/wist/syntax-tour).
