---
title: Package and Contribution Model
description: Features, contributions, slots, package identity and cross-package composition.
---

# Package and contribution model

The generic authoring model separates what a user selects from the implementation participants required to realize it.

## Core terms

| Term | Meaning |
|---|---|
| package | Versioned owner of feature and contribution descriptors plus runtime registrations |
| feature | User-visible capability selected by a language definition |
| contribution | One parser, binder, transformation, pass, backend, tooling or runtime-provider participant |
| slot | Architectural ownership location such as `frontend.parser`, `lowering`, `optimizers` or `backends` |
| capability | Named property supplied and required by contributions |
| artifact contract | Typed protocol crossing a transformation or executor boundary |
| runtime provider | Component that creates the runtime session for the selected plan |

## Feature-owned and package-level contributions

A contribution created inside `AddFeature` is eligible only when an owning feature is selected. This prevents capability resolution from silently pulling implementation from an unselected feature.

Package-level `AddTransformer`, `AddPass` and `AddBackend` registrations are appropriate for infrastructure, optimizer-only or backend-only packages that should not require a synthetic feature.

## Slots and multiplicity

The standard slot set includes:

```text
frontend.syntax
frontend.parser
semantics.binding
semantics.types
lowering
operations
optimizers
backends
runtime.provider
tooling
```

A contribution declares `LanguageSlotMultiplicity`. Single-owner conflicts fail closed. Replace an owner explicitly:

```csharp
builder.ReplaceSlot(
    LanguageSlots.FrontendParser,
    alternativeFrontend,
    expectedCurrentOwner: defaultFrontend);
```

The optional expected owner turns package drift into a diagnostic instead of silently replacing an unexpected implementation.

## Dependencies, conflicts and capabilities

Contribution configuration can declare:

- required contributions;
- required capabilities;
- conflicting contributions;
- conflicting capabilities;
- provided capabilities;
- backend scope;
- `Before` and `After` pass constraints.

If one eligible provider exists, capability closure can select it. Multiple providers require `PreferCapabilityProvider`; no provider produces a planning diagnostic.

## Exact package identity

The plan records package ID, version, Toolchain API compatibility and SHA-256 of the complete selected package manifest. Runtime assembly rejects a package with the same ID/version but different descriptor content.

This protects against a split-brain state where planning used one package graph but execution silently used another implementation graph.

## Cross-package runtime composition

The route runtime assembles transformers, passes and executors from every package selected by the plan. The runtime provider cannot execute only its own package and ignore frontend or optimizer contributions owned by other packages.

See [artifact routing](/language-authoring/artifact-routing) for route construction and [planning diagnostics](/language-authoring/contribution-planning) for fail-closed cases.
