---
title: Installation
description: Show how to install UniversalToolchain.Wist from NuGet.org or prepare the repository locally.
---

# Installation

This page shows two installation paths:

- install the published `UniversalToolchain.Wist` package from NuGet.org for application usage;
- clone and build the repository for development, tests and documentation work.

## Package-first installation

`UniversalToolchain.Wist` is the intended first-contact package for .NET developers. It exposes the `WistEngine` facade and hides the lower-level dialect/runtime pipeline for normal formula usage.

The current preview package is:

```text
PackageId: UniversalToolchain.Wist
Version: 0.1.0-preview.2
Target framework: net10.0
```

From a clean .NET project:

```bash ci-run=false
dotnet add package UniversalToolchain.Wist --version 0.1.0-preview.2
```

Use a `net10.0` project while this preview targets .NET 10:

```xml
<TargetFramework>net10.0</TargetFramework>
```

## Minimal package smoke test

After installing the package, create a small console program:

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

var formula = wist.CompileFunc<double, double, double>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.Invoke(100.0, 5.0);
Console.WriteLine(result); // 95
```

This is the recommended first-contact shape: compile once, keep the returned typed function and call `Invoke` from the hot path.

## Trusted interop example

Use `CreateTrusted` only when the Wist source is trusted by the host application. This preset can expose broader language/runtime capabilities, including C# interop.

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

Do not use `CreateTrusted` for arbitrary user-authored code. For untrusted or semi-trusted formula input, start with `CreateSafeFormulas` and external process/resource isolation when needed.

## Repository development prerequisites

Use the repository path when you want to modify UniversalToolchain, run the full test suite, edit docs or validate packaging.

Prerequisites:

- Git.
- .NET SDK `10.0.103` or a compatible SDK accepted by `UniversalToolchain/global.json`.
- Node.js and npm for VitePress documentation.
- A checked-out working branch for the change you are validating.

The current validation baseline is .NET 10 and target framework `net10.0`. Older target frameworks are not the current compatibility target.

## Repository development steps

### 1. Check the branch

From the repository root:

```bash
git status
git branch --show-current
```

Use the branch required by your task. For normal validation, use the branch you plan to push or open a pull request from.

### 2. Restore and build the .NET solution

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore -m:1 -p:BuildInParallel=false
```

For quick local work, `dotnet build` from the repository root may also work, but the solution path is the documented validation path.

### 3. Run tests when changing behavior

For documentation-only changes, tests may not be necessary. For code, module, dialect or runtime changes, run the relevant test projects:

```bash ci-run=false
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

The Markdown command runner intentionally skips this block because it is a local validation checklist, not a small documentation smoke command. The main `.NET CI` workflow already runs the full solution test step with `dotnet test UniversalToolchain/Wist.sln -c Release --no-build` before Markdown command validation.

### 4. Install documentation dependencies

```bash
npm install
```

### 5. Run the documentation site locally

```bash ci-run=false
npm run docs:dev
```

This starts the VitePress development server for the `docs/` directory. The command keeps running until stopped manually with `Ctrl+C`, so it is intentionally skipped by Markdown command validation in CI.

### 6. Build the documentation site

```bash
npm run docs:build
```

This must pass before merging documentation changes.

## Expected result

After completing the package path:

- the host project references `UniversalToolchain.Wist`;
- `using UniversalToolchain.Wist;` resolves;
- `WistEngine.CreateSafeFormulas()` can compile and invoke a typed formula.

After completing the repository path:

- the .NET solution builds;
- the Wist CLI can run examples;
- the VitePress documentation site can be served locally;
- `npm run docs:build` produces a static documentation build without broken internal links.

## Common mistakes

- Installing the package into a project that does not target `net10.0`.
- Using `CreateTrusted` for untrusted user input.
- Expecting C# interop to be available in restricted formula presets.
- Running repository commands from inside `docs/` instead of the repository root.
- Validating a different branch from the one you plan to push or open a pull request from.
- Installing an older .NET SDK that cannot build `net10.0` projects.
- Forgetting `npm install` before `npm run docs:dev` or `npm run docs:build`.
- Treating docs build success as runtime validation. Documentation build does not replace .NET tests.

## Next

Continue with [First Program](/start/first-program).
