# Wist2 D4 Change Rationale - 2026-06-30

## Request Lock

Goal: run five D4 review/fix cycles over the Wist2 hidden-invariants architecture and produce a clean project archive plus a document explaining the changes.

Primary constraints:

- remove silent legacy defaults from production paths;
- keep UniversalToolchain generic and Wist/backend details behind explicit contracts;
- preserve weak coupling between concrete backend implementations;
- make hidden invariants explicit through typed boundaries, selected build/runtime plans, diagnostics and tests;
- verify build/tests where the environment allows it.

## Baseline

Input handoff archive:

- `wist2-d4-handoff-bundle-2026-06-30.tar.gz`
- outer SHA-256: `b6947d90d33fbac8e25e5700d34f2c024e0f13fd498106adde3b21653a5a6602`

Project archive inside handoff:

- `wist2-hidden-invariants-d4-review-fixes-2026-06-30.tar.gz`
- SHA-256: `6978d9ab086c3671a84981f21e4bfc2f6044cfa62bca602123447aac516afa1e`

Observed baseline:

- extracted tree is not a git repository;
- 68 `.csproj` files;
- local SDK: `/workspace/.dotnet/dotnet`, .NET SDK `10.0.301`;
- default parallel restore failed silently after `Determining projects to restore...`;
- single-node restore/build progressed, then NuGet restore was blocked by `https://api.nuget.org/v3/index.json` returning `403 Forbidden`;
- required packages missing locally include `GrEmit` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

## D4 Iteration Summary

### Iteration 1 - Typed Backend Boundary

Finding:

- `BasicCore` exposed `IReadOnlyList<object> BackendComponents`.
- Wist registrar passed `[this]`, so backend descriptor availability depended on registrar object identity.
- `ModuleContractSelectionBuilder.Build(selectedModules, providers)` silently defaulted to `LegacyCompatible`.
- `PipelineEffectVerifier` accepted missing pipeline order and fell back to module-id ordering.
- backend capability selection unioned all backend capability facets.

Changes:

- added `BasicCore.Contracts.IBackendPipelineComponent`;
- changed pipeline observer contexts and `BasicCoreImpl`/`PreparedExecutionBuilder` to carry `IReadOnlyList<IBackendPipelineComponent>`;
- added `IModuleContractBackendPipelineComponent` and `ModuleContractBackendPipelineComponent` in `UniversalToolchain.ModuleContracts`;
- changed Wist backend registrars to expose explicit backend contract components instead of implementing `IModuleContractDescriptorProvider` directly;
- renamed implicit legacy build overload to `BuildLegacyCompatible`;
- made strict pipeline effect verification report `UT-PIPELINE-EFFECT-004` when pipeline order is missing;
- made backend capability selection reject accidental multi-backend capability tables.

Why this is the best shape:

- `BasicCore` owns only a backend metadata boundary, not ModuleContracts types;
- ModuleContracts adapts over the typed boundary and remains the only layer that understands contract descriptor providers;
- Wist binds the selected backend to a concrete contract component explicitly;
- no generic framework layer receives a concrete CIL/interpreter special case.

Why not simpler:

- passing `IReadOnlyList<IModuleContractDescriptorProvider>` through `BasicCore` would make BasicCore depend on ModuleContracts;
- keeping `[this]` would preserve hidden object-identity coupling;
- keeping `object` would make arbitrary runtime objects part of the semantic contract surface.

### Iteration 2 - Test Strength and Vertical Wiring

Finding:

- generated parity tests looped many cases inside one NUnit test;
- there was no regression proving Wist registrar wiring places backend descriptor providers into the created `BasicCoreImpl`;
- backend descriptor tests selected both CIL and interpreter at once, which modeled a multi-backend table rather than a selected backend table.

Changes:

- converted generated arithmetic parity tests to `TestCaseSource` with depth/case/expression in the case name;
- added vertical reflection regression in Wist runtime composition tests to prove real CIL/interpreter registrars create typed backend contract components inside `BasicCoreImpl`;
- changed backend descriptor test to select one backend component and added a negative multi-backend capability test.

Why this is the best shape:

- failures are localizable by generated expression and depth;
- the historical `[this]` defect is tested at the DI/runtime construction boundary;
- multi-backend capability leakage is now rejected rather than accidentally accepted by a broad table.

### Iteration 3 - Legacy Regression Guard

Finding:

- fallback backend component ids could be normalized into poor ids if a future backend component used an already-prefixed id;
- a future contributor could accidentally reintroduce a two-argument `Build` overload with implicit legacy behavior.

Changes:

- `CreateBackendModuleId` now preserves ids already starting with `backend.`;
- added reflection regression proving `ModuleContractSelectionBuilder` has no two-argument `Build` overload.

Why this is the best shape:

- explicit backend ids stay stable;
- legacy compatibility remains available only via a method whose name says what it does.

### Iteration 4 - Strict Default Profile

Finding:

- Wist core service registration still used `ModuleContractPipelineProfiles.MigrationWarn` by default.

Changes:

- changed Wist default module-contract pipeline profile to `StrictEnforced`;
- updated bootstrap contract test accordingly.

Why this is the best shape:

- production/default composition is strict;
- migration compatibility still exists, but only through explicit opt-in in tests or caller-provided factory configuration.

Why not simpler:

- deleting migration profile entirely would remove a useful observe/warn rollout tool;
- keeping `MigrationWarn` as default would violate the request to disable legacy rather than silently tolerate it.

### Iteration 5 - Final Review and Packaging Audit

Checks performed:

- compared modified tree against the input project archive;
- searched for `IReadOnlyList<object>`, `[this]` registrar component passing, and implicit two-argument production selection builder usage;
- checked that `BasicCore` does not reference `UniversalToolchain.ModuleContracts`;
- checked generated `bin/` and `obj/` paths for archive exclusion;
- checked for obvious secret-like paths/content patterns.

Residual findings:

- `SelectedModuleContractTableProvider` still has a `legacy.clr.*` fallback for undeclared frontend/optimizer components. In strict mode this becomes an error for new modules; the fallback is retained as an explicit diagnostic identity for migration/reporting, not as a production compatibility default.
- Some tests still explicitly use `MigrationWarn` where they are testing migration behavior.

## Requirement Traceability

| Requirement | Change | Validation status |
|---|---|---|
| No untyped backend object bag | `IBackendPipelineComponent` replaces `IReadOnlyList<object>` in BasicCore contexts/builders | Static grep passed |
| No `[this]` backend wiring | Wist registrars return explicit `ModuleContractBackendPipelineComponent` | Static grep passed for registrar path; vertical test added |
| No silent legacy default | `BuildLegacyCompatible` replaces implicit 2-arg `Build`; regression test added | Static grep/reflection test added |
| Actual pipeline order required | missing order reports `UT-PIPELINE-EFFECT-004`; tests pass explicit order | Static review; tests added |
| Single selected backend capability surface | factory rejects multiple non-core backend facets | Static review; negative test added |
| Better generated parity diagnostics | arithmetic generated tests use `TestCaseSource` | Static review |
| Default production strictness | Wist core registration uses `StrictEnforced` | Static review; test expectation updated |

## Build and Test Status

Observed commands:

```bash
DOTNET_CLI_HOME=/workspace/.dotnet-home DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 /workspace/.dotnet/dotnet --info
```

Result: SDK `10.0.301` found.

```bash
DOTNET_CLI_HOME=/workspace/.dotnet-home DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 /workspace/.dotnet/dotnet build UniversalIntermediateRepresentation/UniversalIntermediateRepresentation.csproj -m:1 -p:RestoreUseStaticGraphEvaluation=true -v:minimal
```

Result: succeeded.

```bash
DOTNET_CLI_HOME=/workspace/.dotnet-home DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 /workspace/.dotnet/dotnet build UniversalToolchain.ModuleContracts/UniversalToolchain.ModuleContracts.csproj -m:1 -v:minimal
```

Result: restore reached NuGet and failed because `api.nuget.org` returned `403 Forbidden`; missing local packages include `GrEmit` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

Decision-readiness verdict: CONDITIONAL GO for architecture changes, PARTIAL for compile/test release gate because external package restore is blocked in this environment.

## Remaining Completion Gap

The final release gate still needs to be run in an environment with NuGet access or a populated local package cache:

```bash
DOTNET_CLI_HOME=/workspace/.dotnet-home DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 /workspace/.dotnet/dotnet test Tests/Tests.csproj -m:1 -v:minimal
DOTNET_CLI_HOME=/workspace/.dotnet-home DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 /workspace/.dotnet/dotnet test UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -m:1 -v:minimal
DOTNET_CLI_HOME=/workspace/.dotnet-home DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 /workspace/.dotnet/dotnet test UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -m:1 -v:minimal
```

Use `-m:1` in this container-like environment because parallel restore graph produced silent MSBuild failures.

## Compilation Follow-up With Local Packages

Additional package archives provided after the first D4 pass:

- `gremit.tar.xz`, SHA-256 `472c80a55bd0130850031889dd5f1d7df5e06125a6454b0299b45de0d18865c3`;
- `microsoft.extensions.dependencyinjection.abstractions.tar.xz`, SHA-256 `a71e9670be8cb52cb10b8bd12d8ec34760cd9d40920c557e0c00539e64d59a6e`.

Local feed used:

- `/workspace/local_nuget_feed_20260630_b`

Compile fix found during this gate:

- `UniversalToolchain.ModuleContracts/ContractIdentifierValidation.cs`
- changed `Contains('.', StringComparison.Ordinal)`, `StartsWith('.', ...)`, `EndsWith('.', ...)` to string overloads with `"."`;
- reason: current target/framework resolved the used overloads as string overloads and reported `CS1503 char -> string`.

Observed successful builds:

```bash
DOTNET_CLI_HOME=/workspace/.dotnet-home DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 NUGET_PACKAGES=/workspace/nuget_packages_20260630_b /workspace/.dotnet/dotnet build UniversalToolchain.ModuleContracts/UniversalToolchain.ModuleContracts.csproj --no-restore -m:1 -v:minimal
```

Result: succeeded with `0 Warning(s), 0 Error(s)`.

```bash
DOTNET_CLI_HOME=/workspace/.dotnet-home DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 NUGET_PACKAGES=/workspace/nuget_packages_20260630_b /workspace/.dotnet/dotnet build BasicCilCompiler/BasicCilCompiler.csproj --no-restore -m:1 -v:minimal
```

Result: succeeded with `0 Warning(s), 0 Error(s)`.

```bash
DOTNET_CLI_HOME=/workspace/.dotnet-home DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 NUGET_PACKAGES=/workspace/nuget_packages_20260630_b /workspace/.dotnet/dotnet build BasicInterpreter/BasicInterpreter.csproj --no-restore -m:1 -v:minimal
```

Result: succeeded with `0 Warning(s), 0 Error(s)`.

Full solution restore is still blocked because the provided local feed does not include these package ids:

- `BenchmarkDotNet`
- `CommandLineParser`
- `DynamicExpresso.Core`
- `JetBrains.Annotations`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.NET.Test.Sdk`
- `NCalc.LambdaCompilation`
- `NCalcSync`
- `NUnit`
- `NUnit.Analyzers`
- `NUnit3TestAdapter`
- `SharpFuzz.Common`
- `System.Reflection.MetadataLoadContext`
- `coverlet.collector`

Updated decision-readiness verdict:

- `GO` for the changed core contract/backend projects that were compiled successfully;
- `PARTIAL` for full solution build and tests until the remaining NuGet packages are available locally or `nuget.org` access works.
