# Wist2 architecture hardcode fixes v4 report

## Baseline

Source archive: `Wist2-ea428d9-architecture-hardcode-fixes-v3.tar.gz`.
Reason for v4: user-provided `dotnet test Wist.sln` log showed that v3 built successfully, but tests failed after execution began.

## Root cause from test log

The dominant failure was `UT-CONTRACT-007`: selected module `backend.runtime.provider.policy` had no module contract descriptor and was not in the legacy compatibility baseline. This was introduced by v3: `RuntimeProviderPolicyComponent` was added as an `IBackendPipelineComponent`, and the module contract pipeline treated every backend component as a selected backend module.

A secondary regression appeared in `LocalVariableRuntimeCallPipelineTests`: v3 changed `AbstractIrExtensions.CallCSharp(...)` to emit typed intrinsic instructions, while existing compatibility tests and some pipeline surfaces still expected the legacy-normalized `['call C#', operand]` shape.

## Changes in v4

### 1. Contract pipeline ignores auxiliary backend components

Changed:

- `UniversalToolchain.ModuleContracts/SelectedModuleContractTableProvider.cs`
- `UniversalToolchain.ModuleContracts/ModuleContractPipelineObserver.cs`

Only `IModuleContractBackendPipelineComponent` instances now contribute selected backend module IDs to module-contract selection and pipeline effect ordering. Auxiliary backend components, such as runtime provider policy, remain available to execution/runtime composition but are not automatically promoted into the module contract graph.

This keeps the v3 architecture intent: provider allowlist remains composition-owned and no longer comes from scanning optimized AIR.

### 2. Added regression guardrail for this exact bug

Changed:

- `Tests/Internal/ModuleContracts/BackendCapabilityDescriptorTests.cs`

Added `AuxiliaryBackendPipelineComponents_ShouldNotBecomeSelectedContractModules`, which verifies that a plain backend component with id `runtime-provider-policy` does not create selected module id `backend.runtime.provider.policy` and does not produce missing-descriptor diagnostics.

### 3. Restored legacy-compatible C# call emission shape

Changed:

- `AbstractIrExtensions/GenericAbstractIrExtensions.cs`

`CallCSharp(MethodInfo)`, `CallCSharp(CSharpCallDescriptor)`, and `CallCSharp(ConstructorInfo)` again emit legacy-normalized intrinsic instructions through `air.Intrinsic("call C#", ...)` / `air.Intrinsic("call C# ctor", ...)`.

The typed/legacy `CSharpCallIntrinsicReader` remains in optimizer/runtime-analysis code, so production optimizer/runtime planning still avoids raw display-name comparison.

## Static checks performed

Commands run from `UniversalToolchain`:

```bash ci-run=false
grep -R '"call C#"' -n \
  BasicCore/Core/PreparedExecutionBuilder.cs \
  NativeMathModule \
  ConditionsModule/Optimizers \
  BytecodeDynamicMethodsCompiler/Compilers/CilExecutionRequirementAnalyzer.cs \
  --exclude-dir=bin --exclude-dir=obj
```

Observed: no matches.

```bash ci-run=false
grep -R 'UniversalToolchain.Wist.Contracts\|WistIdentifierFacts\|WistScopesFacts' -n \
  IdentifierModule ScopesModule VariablesModule \
  --exclude-dir=bin --exclude-dir=obj --exclude='*.csproj'

grep -R 'UniversalToolchain.Wist.Contracts' -n \
  IdentifierModule/*.csproj ScopesModule/*.csproj VariablesModule/*.csproj
```

Observed: no matches.

```bash ci-run=false
grep -R 'ReadSelectedBackendModuleIds' -n UniversalToolchain.ModuleContracts
```

Observed: call sites now filter backend components through `OfType<IModuleContractBackendPipelineComponent>()`.

## Test/build status

I could not complete full `dotnet test Wist.sln` inside the sandbox because the sidecar restore/build environment repeatedly stalled during restore for the clean archive. I therefore do **not** claim full suite pass from my environment.

Post-fix user-environment status: after applying v4, the user reported that `dotnet test Wist.sln` completed successfully. Treat that as external developer-machine evidence for v4, not as sandbox evidence from this report.

Evidence from the user's environment showed v3 compiled successfully and then failed at test execution. v4 specifically fixes the observed execution-time root cause and adds a regression test for that root cause.

Recommended verification command on the user's machine:

```bash ci-run=false
cd UniversalToolchain
dotnet test Wist.sln
```

If `PLATFORM=linux/amd64` is present in the shell, use:

```bash ci-run=false
dotnet test Wist.sln -p:Platform="Any CPU"
```

## Artifact checks

Before packaging, generated `bin/`, `obj/`, `.git`, `.vs`, and common build/cache directories were removed/excluded. The final archive was unpacked into a clean directory for content checks.
