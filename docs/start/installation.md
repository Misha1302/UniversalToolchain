---
title: Installation
description: Install the published Wist package or prepare the current repository source safely.
audience: wist-application-developer
status: current
lastVerifiedAgainst: wist-release-readiness-2026-08-18
---

# Installation

There are three different operations that must not be confused:

1. install the pinned package verified by the NuGet.org smoke;
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

The repository smoke scripts create a temporary `net10.0` project, isolate NuGet caches, restore only from NuGet.org, compile and evaluate formulas and verify a rejected statement-style rule. They test the published package boundary, not a sandbox/security boundary.

Linux/macOS:

```bash ci-run=false
./Tools/smoke-published-wist-package.sh 0.1.0-alpha.1
```

Windows with PowerShell 7:

```powershell
pwsh -File .\Tools\smoke-published-wist-package.ps1 -PackageVersion 0.1.0-alpha.1
```

Expected final line:

```text
Published UniversalToolchain.Wist 0.1.0-alpha.1 smoke passed.
```

Both scripts require the exact version explicitly and use an isolated temporary NuGet cache plus the public NuGet.org feed. The PowerShell script reports the failing stage and removes its temporary project in `finally` on both success and failure.

## Current source candidate

<!-- wist-source-candidate:begin -->
The current source project defines `UniversalToolchain.Wist` `0.1.0-alpha.7`. The same version number is **available on NuGet.org**, but current-source binary provenance has not been established. See [Availability and binary provenance](#availability-and-binary-provenance). The repository does not present `./artifacts/packages` as a public feed: that directory exists only after the baseline-bearing maintainer packaging gate succeeds.
<!-- wist-source-candidate:end -->

To evaluate this exact checkout, build/run it as source. Do not silently change the pinned installation version merely because another version is available.

## Availability and binary provenance

The official NuGet v3 index was checked on **2026-11-03**. It returned `0.1.0-alpha.1`, `0.1.0-alpha.7`, `0.1.0-preview.1` and `0.1.0-preview.2`. This is a dated observation, not a recommendation to select the greatest version string. The machine-readable snapshot is `nugetAvailability` in `eng/documentation-release-state.json`.

| Status | Meaning |
|---|---|
| Available on NuGet | The official index lists the version; this does not verify its behavior. |
| Pinned installation / CI smoke | `0.1.0-alpha.1`, retained as the documented installation choice for controlled evaluation. The legacy JSON key `publishedVersion` selects this one version; it does not mean “only published version” or “latest”. |
| Clean-room verified | A successful smoke applies only to the downloaded version, tested scenario, environment and run. It is not a full compatibility certificate. |
| Source identity | The project declares `0.1.0-alpha.7`; this does not identify every byte of a same-number NuGet package. |
| Stable compatibility | Not promised by these prereleases. |

The downloaded `alpha.7` package records repository commit `165e3b051bcc879a38f512c9cc8df63d1d172318` under the historical repository name `Misha1302/Wist2`. That metadata is not proof that its binary payload matches current `master`. Its embedded README and project URL still point to the old `/Wist2/` site. Source link corrections do not repair an already published package: a separately reviewed release with a new version is needed; do not repack the existing published identity.

Run the bounded external check separately from offline documentation validation:

```bash ci-run=false
npm run docs:external
```

It checks the official index and current first-contact website links. Exit `0` means those checks passed, `1` means a confirmed availability/link defect, and `2` means **INCONCLUSIVE** (network/service unavailable). It does not certify existing embedded READMEs or choose a recommended version. Inspect the downloaded `.nupkg` and run the exact-version consumer smoke before release promotion.

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

- treating a version listed on NuGet as proof of current-source equivalence or an automatic upgrade recommendation;
- using `--source ./artifacts/packages` from a clean external checkout where no reviewed package bundle exists;
- running `./build.sh` or `./build.sh --skip-docs` without the required release-package baseline inputs;
- installing into a project that does not target `net10.0`;
- exposing CLR assemblies to untrusted user input;
- treating restricted dialects as security sandboxes;
- running repository commands from inside `docs/` instead of the repository root;
- treating docs build success as runtime validation.

## Next

Continue with [First Program](/start/first-program).
