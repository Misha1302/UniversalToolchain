---
title: Writing Backends
description: Add a backend through typed package contributions and the canonical language plan.
---

# Writing Backends

A backend is a typed route contribution selected by `LanguageCompiler` and materialized by `LanguageRuntime`. It is not discovered from assemblies, DI registrations, attributes, or sidecar manifests.

## Canonical registration

For a reusable language package, declare the backend through `LanguagePackageBuilder` (or the equivalent explicit `ILanguageExtensionPackage` descriptor):

```csharp
var package = LanguagePackageBuilder.Create("Acme.Language", "1.0.0")
    .AddBackend(
        "optimized-cil",
        "acme.backend.optimized-cil",
        loweredArtifact,
        static (artifact, context) => CompileOptimized(artifact, context),
        LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
    .UseRouteRuntime("acme.runtime", "1.0.0")
    .Build();
```

The contribution id, package identity, input artifact contract and runtime traits are explicit data in the package descriptor. `LanguageCompiler` owns selection and ordering; `LanguageRuntime` binds the exact selected implementation from the supplied route-component sources.

## Wist backends

Built-in Wist backends are owned by `WistLanguageFeaturePackage` and its exact route-component catalog. A third-party backend must use its own package/contribution ids; it must not impersonate canonical `wist.*` contributions.

Do not build a backend by recreating an end-to-end BasicCore pipeline. Backends own backend-specific transformation/execution only; Source → Syntax/AST → Bytecode → AIR and optimizer routing remain plan-owned stages.

## Dialect selection

A dialect frontend may request a backend alias, but the frontend must translate that request into a typed `BackendId` before planning. Alias parsing is configuration work, not runtime discovery.

```text
dialect my-dialect
backend optimized-cil enable
```

The resulting `LanguageDefinition` selects the typed backend; `LanguageCompiler` either produces one deterministic plan or fails closed.

## What must not be required

Adding a backend must not require:

- adding a value to a central backend enum;
- editing parser switches for a specific backend id;
- scanning loaded assemblies or metadata attributes;
- emitting or shipping `.dialect.runtime.json` files;
- registering every possible backend eagerly in a service provider;
- constructing a second runtime/build plan after `LanguageCompiler`.

## Acceptance

Tests for an external backend should prove:

- a new external package/contribution id is accepted without generic SDK edits;
- its route is present in the resulting `LanguagePlan`;
- `LanguageRuntime` resolves exactly that package implementation;
- missing/duplicate/tampered component sources fail closed;
- attempts to reuse a canonical built-in contribution id are rejected;
- reference and optimized backends preserve the public behavior contract where parity is required.
