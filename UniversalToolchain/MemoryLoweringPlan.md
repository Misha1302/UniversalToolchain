# Plan: lazy loading only required modules to reduce memory usage

## Status

Architecture tracking document. The original plan was written before the minimal dialect runtime path stabilized; this
file now distinguishes implemented behavior from remaining work.

Implemented in the normal Wist dialect path:

- dialect source is compiled into a build plan before runtime assembly,
- runtime selection is resolved from manifest-backed catalog entries,
- `ComposeText` and `ComposeFile` return the selected runtime plan without creating the host,
- `CreateHost` builds the runtime provider from that resolved selection,
- shipped Wist example dialects are covered by composition and execution smoke tests,
- backend aliases such as `compiler` resolve to the canonical `cil` backend where declared by the backend manifest.

Partially implemented:

- manifest files and reflection-backed component resolution are available, but runtime artifacts are still normally
  already present in application output directories,
- provider creation registers selected runtime components, while backend registrars and some bootstrap services remain
  concrete Wist integration points,
- eager discovery and compatibility helpers still exist and are not removed.

Still future work:

- selective assembly loading for unloaded feature packs,
- measured memory baselines and regression thresholds,
- broader cleanup or renaming of eager discovery APIs,
- fully descriptor-driven deployment packaging outside the current Wist validation path.

## Goal

Reduce memory usage during dialect composition and runtime startup by keeping the normal dialect path on the
selection-driven flow:

~~~text
discover everything -> register everything -> filter
~~~

to:

~~~text
parse dialect -> build plan -> resolve only selected components -> register only selected components
~~~

This plan is focused on the **dialect-based execution path** first. The existing eager path remains for compatibility.
The current dialect workflow already compiles a dialect, builds a plan, resolves a selected runtime plan from manifests,
and creates a host as a separate step. The full eager path still performs automatic discovery first and filtering later,
and `TypesFinder` keeps global static caches and recursively scans assemblies.

For concrete reference points in the current codebase, see
[`TypesFinder`](AssemblyFinder/TypesFinder.cs),
[`WistDialectExecutionWorkflow`](UniversalToolchain.Dialects.Wist/WistDialectExecutionWorkflow.cs), and
[`AddWistDialectServices`](UniversalToolchain.Dialects.Wist/WistDialectServiceCollectionExtensions.cs).

---

## Problem statement

### Current memory problem

The compatibility/eager DI path is broad:

1. `AddWistServices(...)` registers core factories.
2. It calls `RegisterAutoDiscoveredServices(...)`.
3. That path uses `TypesFinder` or loaded assemblies.
4. `AddAutoRegisteredServices(...)` iterates all types from assemblies and registers supported ones.
5. Only after that `ApplyOptionsFilters(...)` removes excluded namespaces/modules.

This means that an eager startup path can still do broad work even when a dialect actually needs only:

~~~text
use Arithmetic, Variables
backend interpreter
enable LocalVariablesOptimization
~~~

the eager path may still:

- scan a large set of assemblies,
- reflect over many unrelated types,
- populate global caches,
- register many descriptors and services that are later removed.

`TypesFinder` is especially important here because it uses global static/lazy caches, keeps loaded assemblies, caches
assembly names, caches “bad assemblies”, scans `*.dll` recursively and loads types from assemblies. This is a strong
candidate for startup memory growth that is independent from the actual selected module count.

### Why “just use fewer modules” is not enough

Using fewer modules is not sufficient for an eager startup path because the expensive part can happen before selection
becomes effective. The normal Wist dialect path avoids making eager discovery the source of truth by resolving a
manifest-backed selected runtime plan before host creation.

---

## Scope

## In scope

- Keep the **dialect execution path** selected by requested modules/optimizers/backends.
- Keep lightweight manifest-backed runtime descriptors and two-stage composition visible in code and tests.
- Reduce dependency on full auto-discovery in dialect workflows.
- Maintain determinism and memory-oriented tests.
- Plan remaining selective loading and eager-path cleanup without overstating what is complete.

## Out of scope

- Full rewrite of the non-dialect eager path.
- Full unload support for assemblies.
- Replacing all reflection in the project.
- Aggressive process-level sandboxing.

---

## High-level design

## Target architecture

### Old path

~~~text
dialect text/file
    -> full auto-discovery of modules/services
    -> all matching types reflected and registered
    -> filters applied
    -> runtime created
~~~

### Current normal dialect path

~~~text
dialect text/file
    -> parse dialect DSL
    -> build DialectBuildPlan
    -> resolve aliases to manifest-backed runtime descriptors
    -> register only selected modules
    -> register only selected optimizers
    -> register only selected backends
    -> build provider
    -> create host
    -> execute
~~~

### Main principle

The dialect path must remain **selection-driven**, not **discovery-driven**.

---

## Design constraints

1. **Do not break existing eager path immediately.**
   `AddWistServices()` should remain available for compatibility and tests that still rely on full auto-discovery.

2. **The dialect path must not depend on broad assembly scanning at execution time.**
   It may still use a prebuilt registry/catalog, but must not scan all assemblies every time.

3. **Keep deterministic ordering.**
   The project already has ordering-sensitive dialect behavior and tests for deterministic ordering. The new runtime
   composition preserves deterministic ordering of selected modules/optimizers/backends.

4. **Do not mix runtime selection with DI auto-registration.**
   Selection must be based on runtime descriptors, then DI registration should instantiate only chosen types.

5. **No hidden global mutable state in the selected dialect path.**
   Avoid new static caches unless they are read-only and deterministic.

---

## Main implementation strategy

## Summary

The dialect runtime composition subsystem is organized around 3 layers:

### Layer 1: lightweight catalog/registry

Stores metadata only:

- alias
- canonical id
- implementation type
- optional assembly identity
- component kind

This layer must be cheap and stable.

### Layer 2: build plan to runtime selection

Convert `DialectBuildPlan` into a resolved set of runtime descriptors:

- ordered frontend modules
- enabled optimizers
- enabled backends

No service provider yet.

### Layer 3: minimal service provider assembly

Use `AddWistCoreServices()` and explicitly register only selected components.
This is the point where actual runtime objects are instantiated. `AddWistCoreServices()` is already the right minimal
base for this.

---

## Detailed step-by-step implementation plan

The phase sections below preserve the original design checklist and implementation sketches. Use the status section
above as the current truth for what is implemented, partial, or future work.

## Phase 1. Freeze current behavior with tests

Before refactoring, add tests that lock in the current expected dialect semantics.

### Add tests

#### 1. Deterministic dialect composition

Create tests:

- `ComposeText_SameDialect_RepeatedRuns_ProduceSameSelectedModules`
- `ComposeFile_SameDialect_RepeatedRuns_ProduceSameBackends`
- `ComposeText_ParallelRuns_DoNotCrossPolluteSelections`

#### 2. Compatibility between eager and selected runtime paths

Prepare golden cases for several dialects:

- minimal dialect
- full-default
- arithmetic-only
- interpreter-only
- compiler-only
- optimizer enabled/disabled cases

Each test should compare:

- selected module aliases
- selected optimizer aliases
- selected backend ids
- diagnostics
- final runtime behavior on a few sample programs

#### 3. Memory-oriented smoke tests

Not precise profiler assertions, only coarse safety tests:

- repeated compose 100 times should not grow registered component count,
- repeated provider build should not accumulate selected module descriptors,
- no static selection cache growth.

### Acceptance criteria for Phase 1

- Existing dialect behavior is captured in tests.
- There is a stable comparison baseline before refactoring.

---

## Phase 2. Introduce lightweight runtime descriptor model

Add a dedicated runtime descriptor model that contains **metadata only**.

### New types

~~~csharp
public enum DialectRuntimeComponentKind
{
    FrontendModule,
    Optimizer,
    Backend
}
~~~

~~~csharp
public sealed record DialectRuntimeModuleDescriptor(
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    Type ImplementationType,
    string? AssemblyName = null
);
~~~

~~~csharp
public sealed record DialectRuntimeOptimizerDescriptor(
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    Type ImplementationType,
    string? AssemblyName = null
);
~~~

~~~csharp
public sealed record DialectRuntimeBackendDescriptor(
    DialectBackendId CanonicalId,
    IReadOnlyList<string> Aliases,
    Type MetadataOwnerType,
    string? AssemblyName = null
);
~~~

### Notes

- `Type` is enough for first iteration.
- `AssemblyName` is optional but should be included now because it helps future selective assembly loading.
- These descriptors must be immutable.

### Why

This layer allows the dialect flow to reason about runtime capabilities without immediately building DI objects or doing
broad registration. This is implemented through manifest catalog entries and selected runtime plans.

### Acceptance criteria for Phase 2

- Runtime descriptors exist as standalone immutable records.
- They can fully represent selected runtime components without DI.

---

## Phase 3. Introduce runtime catalog/registry abstraction

Use a catalog specifically for alias resolution.

### New interface

~~~csharp
public interface IDialectRuntimeCatalog
{
    bool TryResolveModule(string alias, out DialectRuntimeModuleDescriptor? descriptor);
    bool TryResolveOptimizer(string alias, out DialectRuntimeOptimizerDescriptor? descriptor);
    bool TryResolveBackend(DialectBackendId id, out DialectRuntimeBackendDescriptor? descriptor);

    IReadOnlyCollection<DialectRuntimeModuleDescriptor> Modules { get; }
    IReadOnlyCollection<DialectRuntimeOptimizerDescriptor> Optimizers { get; }
    IReadOnlyCollection<DialectRuntimeBackendDescriptor> Backends { get; }
}
~~~

### Implementation shape

~~~csharp
public sealed class DialectRuntimeCatalog : IDialectRuntimeCatalog
{
    // internal immutable dictionaries, case policy explicit and tested
}
~~~

### Builder

~~~csharp
public sealed class DialectRuntimeCatalogBuilder
{
    public DialectRuntimeCatalogBuilder RegisterModule(DialectRuntimeModuleDescriptor descriptor);
    public DialectRuntimeCatalogBuilder RegisterOptimizer(DialectRuntimeOptimizerDescriptor descriptor);
    public DialectRuntimeCatalogBuilder RegisterBackend(DialectRuntimeBackendDescriptor descriptor);

    public DialectRuntimeCatalogBuilder RegisterAttributedModules(params Type[] types);
    public DialectRuntimeCatalogBuilder RegisterAttributedOptimizers(params Type[] types);
    public DialectRuntimeCatalogBuilder RegisterAttributedBackends(params Type[] types);

    public DialectRuntimeCatalog Build();
}
~~~

### Important implementation details

#### Alias collisions

Builder must fail fast with clear messages:

- which alias collided,
- which old type owns it,
- which new type tried to reuse it.

This is already aligned with existing alias collision tests.
For example, see
[
`RuntimeManifestCatalogContractTests`](UniversalToolchain.Dialects.Tests/RuntimeCatalog/RuntimeManifestCatalogContractTests.cs).

#### Ordering

Internally sort descriptors deterministically:

- canonical alias ascending for modules/optimizers,
- canonical backend id ascending for backends,
- type full name as tie-breaker.

This ensures that repeated catalog builds are stable even if registration order varies.

### Acceptance criteria for Phase 3

- A catalog can resolve aliases without building a provider.
- Catalog output is deterministic for repeated construction.
- Duplicate aliases fail fast.

---

## Phase 4. Separate selection from instantiation

Introduce a resolved runtime composition object that contains only the selected descriptors in final order.

### New type

~~~csharp
public sealed record DialectResolvedRuntimeSelection(
    IReadOnlyList<DialectRuntimeModuleDescriptor> OrderedModules,
    IReadOnlyList<DialectRuntimeOptimizerDescriptor> EnabledOptimizers,
    IReadOnlyList<DialectRuntimeBackendDescriptor> EnabledBackends
);
~~~

### Resolver

~~~csharp
public sealed class DialectRuntimeSelectionResolver
{
    public DialectRuntimeSelectionResolver(IDialectRuntimeCatalog catalog);

    public DialectResolvedRuntimeSelection Resolve(DialectBuildPlan plan);
}
~~~

### Responsibilities

#### Modules

- Resolve all module aliases from `plan.OrderedModules`.
- Preserve the exact order from the normalized `DialectBuildPlan`.
- Report missing aliases as resolution diagnostics.

#### Optimizers

- Resolve only explicitly enabled optimizers.
- Respect backend targeting if policy is backend-specific.
- Preserve deterministic order.

#### Backends

- Resolve only selected backend ids from the build plan.
- Preserve deterministic order from plan normalization.

### Important rule

This resolver must do **no DI registration** and **no instance creation**.

### Acceptance criteria for Phase 4

- Given a `DialectBuildPlan`, resolver returns selected descriptors only.
- Missing components are reported without side effects.
- The selection is deterministic across repeated calls.

---

## Phase 5. Introduce minimal runtime provider factory

This is the core memory-saving behavior in the current path.

### New type

~~~csharp
public sealed class DialectRuntimeProviderFactory
{
    public IServiceProvider CreateProvider(DialectResolvedRuntimeSelection selection);
}
~~~

### Internal algorithm

#### Step 1

Create a fresh `ServiceCollection`.

#### Step 2

Call:

~~~csharp
services.AddWistCoreServices();
~~~

This gives the minimal base runtime without broad discovery.

#### Step 3

Register selected frontend modules explicitly:

~~~csharp
foreach (var module in selection.OrderedModules)
{
    services.AddSingleton(typeof(IFrontendCoreModule), module.ImplementationType);
}
~~~

#### Step 4

Register selected optimizers explicitly:

~~~csharp
foreach (var optimizer in selection.EnabledOptimizers)
{
    services.AddTransient(typeof(IIRProcessingModule), optimizer.ImplementationType);
}
~~~

#### Step 5

Register only required backend-related components.

This part depends on current backend design. For first iteration:

- keep backend descriptor as metadata,
- map selected backends to the required core runnable registrations,
- do **not** register unselected backend paths.

### Very important

Do **not** call `AddWistServices()` here.
Do **not** call `AddAutoRegisteredServices()` here.
Do **not** call `TypesFinder.LoadAllAssemblies()` here.
Do **not** scan all types here.

### Acceptance criteria for Phase 5

- Minimal provider can be built from selection only.
- Provider contains only selected modules/optimizers/backends plus core services.
- No broad auto-discovery occurs in this path.

---

## Phase 6. Add a dedicated dialect composition pipeline

The normal dialect path does not go through the full eager DI registration path.

### Current flow

~~~text
WistDialectExecutionWorkflow
    -> parse dialect
    -> build DialectBuildPlan
    -> resolve runtime selection from catalog
    -> create minimal provider from selection
    -> create host
~~~

### Required refactor

`WistDialectExecutionWorkflow` receives dependencies with this responsibility split:

~~~csharp
public sealed class WistDialectExecutionWorkflow
{
    private readonly IDialectDslCompiler _dslCompiler;
    private readonly IDialectBuildPlanBuilder _buildPlanBuilder;
    private readonly DialectRuntimeSelectionResolver _selectionResolver;
    private readonly DialectRuntimeProviderFactory _providerFactory;
}
~~~

### Output object

Existing composition result object should expose:

- build plan
- resolved runtime selection
- diagnostics
- provider/host if composition succeeded

### Compatibility rule

Keep the old eager path as a compatibility/bootstrap option:

~~~csharp
ComposeWithFullAutoDiscovery(...)
~~~

The default dialect path should continue to use selected runtime assembly.

### Acceptance criteria for Phase 6

- `ComposeText` and `ComposeFile` use selected-only registration.
- Full auto-discovery is not part of normal dialect composition.
- Existing successful dialects still compose and execute.

---

## Phase 7. Restrict `TypesFinder` usage to bootstrap scenarios only

`TypesFinder` should no longer be a required dependency of every dialect composition.
Currently it is expensive because it maintains global caches and performs recursive assembly scanning and type
enumeration.

### New rule

`TypesFinder` may be used only in one of these cases:

1. compatibility eager path,
2. optional bootstrap catalog generation,
3. explicit offline catalog-building tool.

### Strong recommendation

Do **not** use `TypesFinder.AllTypes` or `LoadAllAssemblies()` from the hot dialect runtime path.

### Optional improvement

Introduce a bootstrapping service:

~~~csharp
public interface IDialectRuntimeCatalogBootstrapper
{
    IDialectRuntimeCatalog BuildCatalog();
}
~~~

Then provide two implementations:

#### 1. Reflection bootstrapper

For development/compatibility.
May scan assemblies once at application startup.

#### 2. Static/manual bootstrapper

For production/minimal memory.
Registers known types explicitly.

### Acceptance criteria for Phase 7

- Normal dialect execution does not require `TypesFinder`.
- `TypesFinder` remains only in bootstrap/compatibility paths.

---

## Phase 8. Optional but recommended: manual runtime catalog for Wist

For maximum memory reduction, do not build the runtime catalog by scanning all assemblies.

Instead, create a manual registration class for the canonical Wist runtime.

### New type

~~~csharp
public static class WistRuntimeCatalogFactory
{
    public static IDialectRuntimeCatalog Create()
    {
        return new DialectRuntimeCatalogBuilder()
            .RegisterModule(new(... Arithmetic ...))
            .RegisterModule(new(... Variables ...))
            .RegisterModule(new(... Loops ...))
            .RegisterOptimizer(new(... LocalVariablesOptimization ...))
            .RegisterBackend(new(... cil ...))
            .RegisterBackend(new(... interpreter ...))
            .Build();
    }
}
~~~

### Why this is valuable

This avoids:

- scanning assemblies,
- reflecting over unrelated types,
- building huge type lists.

### Migration compromise

For the first iteration, manual catalog is acceptable and even desirable.
It is much simpler and much more memory-friendly than trying to keep full magical discovery.

### Acceptance criteria for Phase 8

- Wist dialect path works using a manual catalog only.
- No broad runtime scanning is needed to compose known Wist dialects.

---

## Phase 9. Future-proof selective assembly loading

This remains future work.

### Current limitation

If many modules live in already loaded assemblies, selected-only DI registration helps but cannot fully eliminate
assembly metadata memory cost.

### Future direction

Extend runtime descriptors with assembly information:

~~~csharp
public sealed record DialectRuntimeModuleDescriptor(
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    string AssemblySimpleName,
    string TypeFullName
);
~~~

Then introduce:

~~~csharp
public interface ISelectiveAssemblyLoader
{
    Type LoadType(string assemblySimpleName, string typeFullName);
}
~~~

### First safe version

Do not use custom unloadable `AssemblyLoadContext` yet.
Just load missing assemblies on demand using a controlled loader.

### Second version, optional

Support feature packs in separate assemblies:

- `ArithmeticModule.dll`
- `VariablesModule.dll`
- `LoopsModule.dll`
- `Optimizers.LocalVariables.dll`

Then the dialect path can truly load only needed DLLs.

### Acceptance criteria for Phase 9

- Catalog can work with metadata-only descriptors.
- Selected types can be loaded on demand.
- Modular deployments become possible.

---

## Phase 10. Keep and isolate old eager path

Do not delete the current eager path immediately.

### Keep

- `AddWistServices(...)`
- `RegisterAutoDiscoveredServices(...)`
- `AddAutoRegisteredServices(...)`
- `TypesFinder`-based discovery

### But change their positioning

They become:

- compatibility path,
- test utility,
- developer convenience feature,
- non-minimal startup mode.

### Add naming clarity

Prefer names such as:

- `AddWistServicesEager()`
- `AddWistServicesFullDiscovery()`

If renaming is too disruptive, at least document clearly that it is **not** the minimal memory path.

### Acceptance criteria for Phase 10

- Existing consumers are not broken abruptly.
- The new minimal dialect path is the recommended path for low-memory operation.

---

## Concrete code changes by file/module

## 1. `DependencyInjection/ServiceCollectionExtensions.cs`

### Keep

- `AddWistCoreServices()`
- `AddWistServices(...)`

### Add

- `AddSelectedWistRuntimeServices(DialectResolvedRuntimeSelection selection)`

### Expected implementation sketch

~~~csharp
public static IServiceCollection AddSelectedWistRuntimeServices(
    this IServiceCollection services,
    DialectResolvedRuntimeSelection selection)
{
    services.AddWistCoreServices();

    foreach (var module in selection.OrderedModules)
        services.AddSingleton(typeof(IFrontendCoreModule), module.ImplementationType);

    foreach (var optimizer in selection.EnabledOptimizers)
        services.AddTransient(typeof(IIRProcessingModule), optimizer.ImplementationType);

    // backend registration policy here

    return services;
}
~~~

### Rule

This method must never call full auto-discovery.

---

## 2. `DependencyInjection/AutoRegistration.cs`

### Keep

As compatibility/bootstrap-only feature.

### Add docs/comments

State explicitly that this is eager discovery and not the preferred low-memory dialect path.

---

## 3. `AssemblyFinder/TypesFinder.cs`

### Keep

As bootstrap/compatibility utility.

### Do not call from

- `ComposeText`
- `ComposeFile`
- minimal provider assembly path

### Optional cleanup later

Add explicit warnings/comments about static cache semantics.

---

## 4. Dialect runtime package

Add new files:

- `DialectRuntimeComponentKind.cs`
- `DialectRuntimeModuleDescriptor.cs`
- `DialectRuntimeOptimizerDescriptor.cs`
- `DialectRuntimeBackendDescriptor.cs`
- `IDialectRuntimeCatalog.cs`
- `DialectRuntimeCatalog.cs`
- `DialectRuntimeCatalogBuilder.cs`
- `DialectResolvedRuntimeSelection.cs`
- `DialectRuntimeSelectionResolver.cs`
- `DialectRuntimeProviderFactory.cs`
- `WistRuntimeCatalogFactory.cs`

---

## 5. `WistDialectExecutionWorkflow`

### Refactor

Move from eager registration/dependency on broad discovery toward:

~~~text
compile dialect -> build plan -> resolve selection -> create minimal provider
~~~

### Important

The workflow result should expose the selected runtime composition clearly for diagnostics and tests.

---

## Testing plan

## A. Determinism tests

1. `RuntimeCatalog_RepeatedBuilds_AreEquivalent`
2. `RuntimeCatalog_ReversedRegistrationOrder_ProducesEquivalentResolution`
3. `SelectionResolver_RepeatedResolve_SamePlanSameSelection`
4. `ProviderFactory_RepeatedCreateProvider_ContainsOnlySelectedComponents`

## B. Isolation tests

5. `ComposeText_ParallelDifferentDialects_DoNotMixSelections`
6. `ProviderFactory_TwoSelections_ProduceIndependentProviders`
7. `SequentialCompose_DifferentDialects_DoNotLeakSelectedModules`

## C. Compatibility tests

8. `MinimalPath_FullDefault_BehavesLikeOldPath`
9. `MinimalPath_ArithmeticOnly_BehavesLikeOldPath`
10. `MinimalPath_InterpreterOnly_BehavesLikeOldPath`

## D. Low-memory behavioral tests

11. `MinimalPath_DoesNotInvokeAutoRegistration`
12. `MinimalPath_DoesNotTouchTypesFinder`
13. `MinimalPath_RegistersOnlySelectedFrontendModules`
14. `MinimalPath_RegistersOnlySelectedOptimizers`

## E. Failure tests

15. `SelectionResolver_MissingModuleAlias_ProducesResolutionDiagnostic`
16. `SelectionResolver_MissingOptimizerAlias_ProducesResolutionDiagnostic`
17. `SelectionResolver_MissingBackendAlias_ProducesResolutionDiagnostic`
18. `RuntimeCatalog_DuplicateAlias_FailsFast`

---

## Logging and diagnostics

Add lightweight debug diagnostics for the selected runtime path.

### Example fields

- selected module aliases
- selected optimizer aliases
- selected backend ids
- whether minimal path or eager path was used
- provider registration counts

### Why

This will make it much easier to verify that the selected runtime path really avoids eager discovery.

---

## Migration plan

The first migration steps are implemented for the Wist dialect path. Remaining steps focus on packaging, selective
loading, measurement, and cleanup.

## Step 1

Implemented: tests freeze current dialect behavior.

## Step 2

Implemented through manifest-backed runtime component entries and catalog services.

## Step 3

Implemented through selected runtime plan resolution.

## Step 4

Partially implemented: provider creation is selected-runtime based, with Wist-specific backend registrar integration.

## Step 5

Implemented: `WistDialectExecutionWorkflow` composes first and creates hosts from the resolved selection.

## Step 6

Implemented: old eager/discovery helpers remain compatibility and bootstrap paths.

## Step 7

Future/optional: current catalog resolution is manifest-backed rather than a fully manual static catalog.

## Step 8

Future: add selective assembly loading metadata and loading policy.

---

## Risks

## Risk 1: missing implicit registrations

Some runtime behavior may currently depend on eager registration of unrelated services.

### Mitigation

- compare eager and selected runtime paths with golden tests,
- add explicit registrations only where required,
- do not guess hidden dependencies.

## Risk 2: backend registration coupling

Backend support may currently be mixed into generic DI registration.

### Mitigation

- isolate backend descriptor to runtime selection,
- introduce explicit backend registration policy,
- test interpreter-only and compiler-only compositions.

## Risk 3: hidden global state

Some existing logic may silently rely on global caches.

### Mitigation

- add repeated-run and parallel isolation tests,
- make the new dialect path avoid static mutation where possible.

## Risk 4: catalog drift

Manual Wist catalog may become outdated when modules are added.

### Mitigation

- add tests that validate expected aliases exist,
- document update checklist for new modules,
- optionally keep reflection bootstrapper for dev verification.

---

## Acceptance criteria for the whole feature

The memory-lowering feature is complete when all of the following are true:

1. Dialect composition no longer requires `AddWistServices()` in the normal path. Implemented.
2. Dialect composition no longer performs broad auto-discovery as its source of truth in the normal path. Implemented.
3. Runtime provider for a dialect contains only selected modules/optimizers/backends plus required core services.
   Implemented for current Wist validation profiles.
4. Existing main dialect examples still work. Implemented and covered by smoke tests.
5. Deterministic ordering is preserved. Implemented and covered by tests.
6. Parallel compositions do not leak state into each other. Implemented and covered by tests.
7. Tests prove that the selected runtime path does not touch eager discovery helpers. Partially implemented; keep
   strengthening this.
8. Memory usage at startup is measurably lower in the dialect path. Future measurement work.

---

## Minimal first merge recommendation

The original minimal first merge recommendation was:

1. add runtime descriptor records,
2. add runtime catalog,
3. add selection resolver,
4. add minimal provider factory using `AddWistCoreServices()`,
5. switch `WistDialectExecutionWorkflow` to this path,
6. provide a **manual** `WistRuntimeCatalogFactory`,
7. add determinism/compatibility tests.

The current implementation uses a manifest-backed catalog rather than a fully manual `WistRuntimeCatalogFactory`. The
same balance still applies to future refinements:

- real memory reduction,
- low implementation risk,
- minimal architectural damage,
- fast delivery.

---

## Final practical rule

For low-memory dialect execution, the project should follow this invariant:

~~~text
Dialect execution must be assembled from a build plan and a runtime catalog,
not from global assembly scanning and post-filtered auto-registration.
~~~
