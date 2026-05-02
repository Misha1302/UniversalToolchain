---
title: Dependency Injection
description: Explain service registration and deterministic module discovery.
---

# Dependency Injection

Dependency injection is part of UniversalToolchain's runtime composition model.

It is not only a convenience mechanism for constructing objects. It is how dialect services, runtime catalogs, selected runtime plans, backend infrastructure and Wist execution hosts are assembled.

## Place in the architecture

The Wist dialect runtime is normally bootstrapped with:

```csharp
services.AddWistDialectServices();
services.AddWistCilBackend();
services.AddWistInterpreterBackend();
```

The first call registers the core dialect workflow and runtime resolution infrastructure. Backend registration calls add concrete backend runtime surfaces.

## Wist dialect services

`AddWistDialectServices()` composes three groups:

```text
AddWistDialectCoreServices
AddFileSystemRuntimeCatalogServices
AddReflectionRuntimeResolutionServices
```

This means the default Wist setup includes:

- dialect DSL compilation and build-plan services;
- runtime catalog services;
- reflection-based runtime resolution services.

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
| `IDialectCompiledDialectBuildPlanBuilder` | builds compiled dialect build plans |
| `WistDialectExecutionConfigurationBuilder` | builds execution configuration |
| `WistDialectServiceProviderFactory` | creates the runtime service provider |
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
- interpreter/CIL backend registration matrix;
- execution host creation from successful composition only.

## Common mistakes

- Treating global service registration as selected runtime composition.
- Adding a module service without a stable dialect alias or runtime export.
- Making Wist convenience APIs the only true framework path.
- Assuming all registered backends are available to every dialect.
- Letting reflection discovery decide behavior without deterministic ordering tests.

## Next

Continue with [Reference](/reference/) for tables and contracts.
