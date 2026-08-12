---
title: Installation
description: Install the published Wist package or prepare the current repository source safely.
audience: wist-application-developer
status: current
lastVerifiedAgainst: wist-release-state-2026-08-12
---

# Installation

There are three different operations that must not be confused:

1. install the package currently published on NuGet.org;
2. build and run the current repository source;
3. produce a reviewed release-candidate package bundle as a maintainer.

## Install the published package

<!-- wist-published-install:begin -->
The clean-room package smoke currently verifies `UniversalToolchain.Wist` `0.1.0-alpha.1` from the NuGet.org v3 feed:

```bash ci-run=false
dotnet add package UniversalToolchain.Wist \
  --version 0.1.0-alpha.1 \
  --source https://api.nuget.org/v3/index.json
```
<!-- wist-published-install:end -->

Use a `net10.0` project while the published alpha targets .NET 10:

```xml
<TargetFramework>net10.0</TargetFramework>
```

The package page is <https://www.nuget.org/packages/UniversalToolchain.Wist>.

### Clean-room published-package check

The repository smoke script creates a temporary `net10.0` project, isolates NuGet caches, restores only from NuGet.org, compiles and evaluates formulas and verifies a rejected statement-style rule:

```bash ci-run=false
./Tools/smoke-published-wist-package.sh 0.1.0-alpha.1
```

Expected final line:

```text
Published UniversalToolchain.Wist 0.1.0-alpha.1 smoke passed.
```

## Current source candidate

<!-- wist-source-candidate:begin -->
The current source project defines `UniversalToolchain.Wist` `0.1.0-alpha.7`. This candidate is **not published on NuGet.org**. The repository does not present `./artifacts/packages` as a public feed: that directory exists only after the baseline-bearing maintainer packaging gate succeeds.
<!-- wist-source-candidate:end -->

To evaluate the current implementation, clone the repository and build/run it as source. Do not change the published install command to `0.1.0-alpha.7` unless you were given a reviewed feed containing that exact package.

## Minimal package smoke test

After installing the published package, create a small console program:

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

`Compile<TDelegate>` validates and compiles once. Keep the returned typed program and call `CompiledDelegate` from the hot path. Use `Validate` or `TryCompile` for expected authoring failures that should not throw.

## Trusted interop example

Use the full native profile only when the Wist source is trusted by the host application. CLR interop is available only for assemblies explicitly selected by the host:

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

Do not expose CLR assemblies to arbitrary user-authored code. Restricted composition is not a hardened sandbox.

## Repository development prerequisites

Use the repository path when you want to modify UniversalToolchain, run the full test contract, edit docs or validate the current source candidate.

Prerequisites:

- Git;
- .NET SDK `10.0.103` or a compatible SDK accepted by `UniversalToolchain/global.json`;
- Node.js and npm for VitePress documentation;
- a checked-out working branch for the change you are validating.

The current validation target is `net10.0`.

## Repository development steps

### 1. Check the branch

From the repository root:

```bash ci-run=false
git status
git branch --show-current
```

### 2. Restore, build and test the .NET source

```bash ci-run=false
./build.sh --skip-docs --skip-pack
```

This is the canonical contributor path when release packaging inputs are not present. It builds both solutions and runnable samples, executes the exact test-count contract and runs architecture/documentation-status guards.

Do not use `./build.sh --skip-docs` by itself: without `--skip-pack`, the command intentionally enters the release package gate and requires reviewed previous-source and previous-package artifacts.

### 3. Run focused tests when changing behavior

After the canonical build, focused diagnostics can reuse the build outputs:

```bash ci-run=false
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

### 4. Install documentation dependencies

```bash ci-run=false
npm ci --no-audit --no-fund
```

### 5. Run the documentation site locally

```bash ci-run=false
npm run docs:dev
```

This command keeps running until stopped with `Ctrl+C`, so it is not a CI smoke command.

### 6. Validate the documentation

```bash ci-run=false
npm run docs:check
```

The documentation gate validates structure, navigation, links, release-state synchronization and VitePress compilation.

## Maintainer candidate packaging

Producing `artifacts/packages` is a release operation, not a normal installation prerequisite. The canonical package gate requires both:

- `--baseline-source-archive` pointing to the reviewed previous source archive;
- `--previous-package-bundle` pointing to the reviewed previous package bundle.

It then verifies package provenance, API compatibility, package identities, clean consumers and detached integrity metadata. Follow the [Maintainer and Release Guide](/evidence/maintainer-guide).

## Expected result

After the published-package path:

- the host project references `UniversalToolchain.Wist` `0.1.0-alpha.1` from NuGet.org;
- `using UniversalToolchain.Wist;` resolves;
- `WistEngine.CreateRestrictedArithmetic()` can compile and invoke a typed formula.

After the repository path:

- both solutions and samples build;
- the exact test contract passes;
- source demos and CLI commands can run;
- documentation checks can be executed without pretending that a release package was produced.

## Common mistakes

- replacing the published version with the newer source version even though that candidate is not on NuGet.org;
- using `--source ./artifacts/packages` from a clean external checkout where no reviewed package bundle exists;
- running `./build.sh` or `./build.sh --skip-docs` without the required release-package baseline inputs;
- installing into a project that does not target `net10.0`;
- exposing CLR assemblies to untrusted user input;
- treating restricted dialects as security sandboxes;
- running repository commands from inside `docs/` instead of the repository root;
- treating docs build success as runtime validation.

## Next

Continue with [First Program](/start/first-program).
