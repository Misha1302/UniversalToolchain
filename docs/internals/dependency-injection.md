---
title: Dependency Injection
description: Explain service construction inside the canonical LanguagePlan runtime.
---

# Dependency Injection

Dependency injection is an implementation mechanism inside some UniversalToolchain runtime components. It is **not** a semantic planner and it does not decide which Wist modules, optimizers or backends are active.

## Place in the architecture

The canonical ownership chain is:

```text
LanguageDefinition
  → LanguageCompiler
  → LanguagePlan
  → LanguageRuntime
  → exact planned component factories
```

For `.wistdialect` input, the Wist configuration frontend translates aliases and policy into `LanguageDefinition`. `LanguageCompiler` is the sole owner of dependency closure, provider selection, exclusions, ordering and backend routes.

Only after that immutable plan exists does runtime materialization create implementation objects. DI can be used inside those factories to satisfy local implementation dependencies, but it must not mutate or reinterpret the plan.

## Wist frontend materialization

The direct Wist frontend registration creates a small service provider for the **already selected** frontend modules. It supplies implementation services such as:

- `ITypeCatalog`, built from the explicit host-interop assembly boundary;
- `IMethodResolver`;
- the selected capability catalog.

`WistFrontendModuleActivation` then builds module factories in `LanguagePlan` contribution order from exact package sources. A service provider cannot add an unplanned module or reorder the plan.

This distinction is important:

```text
planning:       feature/contribution/backend semantics
materializing:  construct the exact implementation objects selected by the plan
```

DI belongs only to the second line.

## Exact package/source boundary

Runtime materialization is provenance-bearing.

For every component that belongs to the executable graph, the runtime verifies that the supplied component source matches the package id, version, manifest digest and exact implementation identity captured by `LanguagePlan`.

Unrelated tooling-only contributions may exist in a plan without becoming runtime component dependencies. Conversely, a transformer, backend executor or runtime-provider owner that is actually materialized must have an exact source.

## Lifetime ownership

`LanguageRuntime` owns runtime-component lifetime through the canonical runtime lifecycle gate.

Important rules include:

- per-session factories must not silently reuse mutable instances across runtimes;
- stateless singletons require the explicit stateless marker contract;
- runtime disposal waits for active operation leases;
- reentrant disposal from an operation holding a lease fails instead of self-deadlocking;
- construction failures preserve the primary failure even when cleanup also fails;
- component/service-provider owners created for materialization are disposed with the runtime session.

A public facade such as `WistEngine` does not create a second lifetime model. It owns one `LanguageRuntime` and delegates execution/build operations to it.

## Determinism

Dangerous patterns:

- letting service-registration order decide semantic module order;
- scanning all registered services and treating discovered implementations as selected;
- using reflection enumeration as a planner;
- allowing a compatibility service container to select a backend independently of `LanguagePlan`;
- making tests pass only because every Wist module happens to be globally available.

Good patterns:

- make all semantic decisions in `LanguageCompiler`;
- resolve exact contributions and backend routes into `LanguagePlan`;
- materialize only those planned components;
- bind component sources to exact package provenance;
- keep local DI construction deterministic and disposable;
- fail when an exact planned implementation is missing or ambiguous.

## Restricted host interop

DI is not a security boundary.

The canonical Wist runtime derives host-interop exposure from typed runtime policy. Restricted profiles receive no configured host assemblies; trusted profiles receive only the explicit allowlist plus required standard-library ownership.

The local service provider then receives the resulting `ITypeCatalog`. Registering an implementation service does not itself grant language-level interop capability.

## Compatibility boundary

`UniversalToolchain.Dialects.Wist` is retained only as a non-packable compatibility project for a small set of compatibility data/contracts. It is not the canonical DI bootstrap, runtime planner or execution host. Canonical Wist production projects are guarded from depending on it.

The retired production architecture must not be restored through DI helpers. In particular, `SelectedRuntimePlanResolver`, `WistDialectExecutionWorkflow`, `WistDialectExecutionHost`, manifest-selected backend registrars and the old Wist runtime service-provider orchestration are not current semantic owners.

## What to test

Changes around DI/materialization should verify:

- the exact planned component set is materialized;
- unselected module assemblies are not required merely because they exist in the catalog;
- selected package identity/manifest mismatches fail closed;
- tooling-only contributions do not create false runtime-source requirements;
- restricted/trusted interop policy reaches the implementation service boundary correctly;
- independent runtimes do not share mutable per-session state;
- disposal and concurrent operation lifetime semantics remain deterministic;
- backend parity uses one plan rather than two independently composed service graphs.

## Common mistakes

- Treating DI registration as feature selection.
- Reintroducing a second Wist planner behind a compatibility helper.
- Assuming every registered backend is active.
- Allowing module discovery to override `LanguagePlan` order.
- Using global service availability as evidence that a restricted dialect may access a feature.
- Confusing a composition boundary with process isolation.

## Next

Continue with [Reference](/reference/) for public contracts and [Current Canonical Runtime Pipeline](/current-canonical-runtime-pipeline) for the complete execution path.
