---
title: Dependency Injection
description: Explain service registration and deterministic runtime composition.
---

# Dependency Injection

Dependency injection is part of UniversalToolchain's runtime composition model.

It is not only a convenience mechanism for constructing objects. It is how dialect services, runtime catalogs, selected runtime plans, backend infrastructure and Wist execution hosts are assembled.

## Place in the architecture

The canonical Wist dialect orchestration path starts by registering the Wist dialect services:

```csharp
services.AddWistDialectServices();
```

This registers the core dialect workflow, runtime catalog services and runtime resolution infrastructure.

For shipped Wist dialect profiles, concrete backend runtimes should not be treated as globally enabled just because they exist in the outer service collection. The selected dialect is compiled into a build plan, resolved against runtime manifests and then used to create a runtime provider for that selected composition.

Backend activation happens from the selected runtime plan during host creation, not by assuming that every registered backend is available to every dialect.

## Compatibility backend registration helpers

The repository still exposes manual backend registration helpers:

```csharp
services.AddWistCilBackend();
services.AddWistInterpreterBackend();
```

These helpers register backend registrars directly and are useful for compatibility paths, legacy tests, benchmarks or specialized manual wiring.

They are not the normal shipped-profile execution contract. Normal CLI, facade and shipped dialect execution should resolve backend manifest entries and activate the selected backend registrars from those manifests.

## Wist dialect services

`AddWistDialectServices()` composes three groups:

```text
AddWistDialectCoreServices
AddFileSystemRuntimeCatalogServices
AddReflectionRuntimeResolutionServices
```

This means the default Wist orchestration setup includes:

- dialect DSL compilation and build-plan services;
- runtime catalog services;
- reflection-based runtime resolution services used by manifest-backed activation.

## Core Wist services

`AddWistDialectCoreServices()` registers the orchestration layer used by Wist execution workflows.

Important registered services include:

| Service | Role |
|---|---|
| `IDialectGroupProvider` | contributes named dialect groups |
| `IDialectGroupCatalog` | combines group providers |
| `DialectGroupExpander` | expands group references in dialect definitions |
| `SelectedRuntimePlanResolver` | resolves selected runtime components from a build plan |
| `IDialectBackendIntrinsicPolicyResolver` | resolves backend intrinsic policies |
| `IWistRequiredInfrastructureModulesProvider` | provides required Wist infrastructure modules |
| `SelectedRuntimeModuleClassifier` | classifies selected runtime modules |
| `SelectedRuntimeExecutionShapeBuilder` | builds execution shape from selected runtime modules |
| `DialectBackendRuntimeConfigurationBuilder` | builds backend runtime configuration |
| `IntrinsicSemanticBootstrapPlanBuilder` | prepares intrinsic semantic bootstrap |
| `IntrinsicSemanticBootstrapPreProviderValidator` | validates intrinsic bootstrap before provider construction |
| `IntrinsicSemanticBootstrapRuntimeValidator` | validates intrinsic bootstrap after provider construction |
| `IDialectCompiledDialectBuildPlanBuilder` | builds compiled dialect build plans |
| `WistDialectExecutionConfigurationBuilder` | builds execution configuration |
| `WistDialectServiceProviderFactory` | creates the selected runtime service provider |
| `WistDialectExecutionWorkflow` | composes dialect text/files and creates execution hosts |

This is a Wist-first composition path. It should not be described as a fully generic language-workbench API unless the generic layer provides the same capability directly.

## Workflow object

`WistDialectExecutionWorkflow` is the main orchestration object for dialect execution.

It supports:

```text
ComposeFile(path)
ComposeText(sourceText, sourceName)
CreateHost(compositionResult)
```

The workflow compiles dialect source text, builds a dialect build plan, resolves a selected runtime plan, builds execution configuration and creates a `WistDialectExecutionHost`.

## Runtime service provider

The final Wist host is created from a service provider built for the selected execution configuration.

This matters because a dialect should not run against every service registered in the outer application. It should run against the selected runtime plan and backend/runtime surface chosen by dialect composition.

In the canonical path, backend activation happens inside `WistDialectServiceProviderFactory`. Selected backend manifest entries resolve exact backend registrar types, and only the selected backend runtimes are registered for the host.

## Determinism

DI-based composition must remain deterministic.

Dangerous patterns:

- relying on reflection enumeration order;
- accepting duplicate runtime exports without deterministic conflict handling;
- changing module order accidentally through service registration order;
- letting full Wist modules appear inside restricted dialect hosts;
- making tests pass only because all modules are registered globally.

Good patterns:

- deterministic group expansion;
- explicit runtime exports;
- selected runtime plans;
- exact activation from selected manifest entries;
- tests that inspect selected modules/backends;
- failure diagnostics when resolution is ambiguous or incomplete.

## DI and module discovery

Modules may be registered through attributes and service registration helpers.

This is useful, but it creates hidden contracts:

- aliases must be stable;
- runtime exports must match dialect declarations;
- backend/runtime component kinds must be classified correctly;
- required infrastructure modules must be included intentionally;
- restricted dialects must not receive unrelated modules by accident.

## DI is not a security boundary

A restricted runtime service provider can reduce the runtime surface exposed to a dialect.

That is not the same as process isolation. Do not document DI selection as a hardened sandbox. It is a composition and activation boundary.

## What to test

DI/runtime composition changes should test:

- service registration smoke path;
- dialect composition success and failure;
- deterministic selected runtime plan;
- missing backend behavior;
- duplicate or conflicting runtime exports;
- restricted dialect surface;
- selected backend activation from manifests;
- compatibility backend registration helpers separately from canonical shipped runtime paths;
- execution host creation from successful composition only.

## Common mistakes

- Treating global service registration as selected runtime composition.
- Adding a module service without a stable dialect alias or runtime export.
- Making Wist convenience APIs the only true framework path.
- Assuming all registered backends are available to every dialect.
- Presenting compatibility backend helper methods as the normal shipped profile bootstrap path.
- Letting reflection discovery decide behavior without deterministic ordering tests.

## Next

Continue with [Reference](/reference/) for tables and contracts.
