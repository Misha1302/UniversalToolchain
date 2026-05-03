---
title: Module Contracts
description: Make module ownership and extension contracts visible in the public documentation route.
---

# Module Contracts

This page summarizes the contracts module authors must preserve when adding or changing Wist/UniversalToolchain language features.

For the full source document, see `docs/contracts/module-contracts.md`. This reference page exists because these contracts are part of the public module-authoring route, not hidden project notes.

## Why this page exists

UniversalToolchain is intentionally modular. A new feature can affect lexing, parsing, AST translation, bytecode, AIR, runtime manifests, optimizers and backends. Hidden assumptions in any of those layers can silently change language behavior.

A module is not complete when one example runs. It is complete when its syntax ownership, dialect selection, runtime behavior and backend support are explicit and tested.

## Token-name contract

Token names are part of the internal module API.

Rules:

- Do not invent a token name in one file and consume it by raw string in another unrelated file without a shared constant or clear documentation.
- Do not reuse an existing token name for a different semantic meaning.
- Do not rename token names without updating parser, visitor and dialect tests.
- Prefer feature-scoped constants for shared token names.

Risk: a misspelled or reused token name can compile successfully while changing parser behavior.

## Parser-priority contract

Parser creator priority is a grammar contract, not just an ordering hint.

Rules:

- Priority must reflect intended precedence and syntax ownership.
- Do not copy nearby priority values until a single example passes.
- Priority changes require parser regression tests.
- Overlapping syntax requires tests that show the wrong node creator does not take ownership.

Risk: a small priority change can silently change how existing programs parse.

## Visitor-ownership contract

AST translation visitors must be self-filtering.

Rules:

- A visitor must return without output when it does not own the node.
- A visitor must not emit only because a token name or child count loosely matches.
- A visitor must not assume it is the only visitor that can see a node.
- Intentional cooperation between visitors must be documented and tested.

Risk: two visitors can emit for the same node, or no visitor can emit for a valid node.

## Bytecode-tag contract

Bytecode tags are semantic metadata, not decoration.

Rules:

- A tag must have a documented producer and consumer.
- Backend-specific tags must not be used as generic semantic truth.
- Optimizers must not depend on undocumented tag strings.
- Tags affecting safety, purity, side effects, stack shape or backend lowering need documentation and tests.

Risk: tags can become a second untyped language hidden inside bytecode.

## Shared-state contract

Mutable state shared between module components must be explicit, narrow and scoped.

Rules:

- Avoid static mutable state.
- Avoid state that survives across independent compilations unless it is immutable configuration.
- Pass shared data explicitly.
- Add tests that compile independent inputs and prove state does not leak.

Risk: a module can become order-dependent or compilation-history-dependent.

## Dialect-selection contract

A feature is not modular unless a dialect can include or exclude it predictably.

Rules:

- New features should be selectable through dialect/runtime composition when practical.
- Restricted dialects must reject syntax or runtime behavior that is not selected.
- Generic framework layers must not activate features through hardcoded module names.
- Capability reports explain selected behavior; they must not activate behavior.

Risk: a feature can appear enabled even when the selected dialect did not request it.

## Backend-capability contract

Optimized lowering must be backend-capability gated.

Rules:

- Do not emit backend-specific intrinsics unless the selected backend supports them.
- If an optimization changes AIR or bytecode shape, prove semantic parity with a reference backend.
- Backend-specific fast paths need fallback behavior or clear diagnostics.
- Unsupported backend operations must fail explicitly instead of silently falling back.

Risk: a program can work in CIL and fail in interpreter, or an optimizer can leak backend-specific intrinsics into unsupported backends.

## Documentation contract

Every module with non-obvious behavior should document:

- alias;
- syntax ownership;
- parser priority assumptions;
- visitors;
- bytecode/AIR responsibilities;
- backend or intrinsic requirements;
- dialect inclusion and exclusion behavior.

## Related pages

- [Write Modules](/write-modules/)
- [Create Your First Module](/write-modules/create-your-first-module)
- [Testing a Module](/write-modules/testing-module)
- [Backend Contracts](/reference/backend-contracts)
