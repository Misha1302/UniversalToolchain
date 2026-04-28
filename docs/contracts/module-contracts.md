# Module Contracts

This document makes implicit module conventions explicit.

These contracts exist because the compiler pipeline is intentionally modular. A new module can affect lexing, parsing, AST translation, bytecode, AIR, runtime manifests, optimizers, and backends. Hidden assumptions in any of those layers can silently change language behavior.

## Token-name contract

Token names are part of the internal module API.

If parser node creators or visitors refer to token names by string, those names must be treated as stable contracts.

Rules:

- Do not invent a token name in one file and consume it by raw string in another file without a shared constant or clear documentation.
- Do not reuse an existing token name for a different semantic meaning.
- Do not rename token names without updating parser, visitor, and dialect tests.
- Prefer feature-scoped constants for shared token names.
- Add regression tests for syntax that depends on shared token names.

Risk:

A misspelled or reused token name may compile successfully while changing parser behavior or visitor ownership.

## Parser-priority contract

Parser creator priority is a grammar contract, not just an ordering hint.

Rules:

- A priority value must reflect the intended precedence and ownership of a syntax form.
- A module must not choose priority values by copying nearby code without checking grammar overlap.
- Priority changes require parser regression tests.
- Overlapping syntax requires negative tests that show the wrong node creator does not take ownership.
- New syntax that affects expression precedence must include examples for nested and mixed expressions.

Risk:

A small priority change can silently change how existing programs parse.

## Visitor-ownership contract

AST translation visitors must be self-filtering.

The translator can give every configured visitor a chance to inspect a node. Therefore each visitor must decide whether it owns the node.

Rules:

- A visitor must return without output when the node type or node shape is not its responsibility.
- A visitor must not emit output only because a token name or child count happens to match loosely.
- A visitor must not assume it is the only visitor that can see a node.
- If two visitors intentionally cooperate on one node family, the ownership boundary must be documented and tested.
- If a visitor changes stack shape, the expected stack effect must be covered by tests or verifier rules.

Risk:

Two visitors can emit for the same node, or no visitor can emit for a valid node, without a compile-time error.

## Bytecode-tag contract

Bytecode tags are semantic metadata, not decoration.

Rules:

- A tag must have a documented producer and consumer.
- A backend-specific tag must not be used as generic semantic truth.
- Optimizers must not depend on undocumented tag strings.
- Unknown tags should eventually be rejected by a verifier or explicitly treated as extension tags.
- Tags that affect safety, side effects, purity, stack shape, or backend lowering must be centrally documented.

Risk:

Tags can become a second untyped language hidden inside bytecode.

## Shared-state contract

Mutable state shared between module components must be explicit, narrow, and scoped.

Rules:

- Avoid static mutable state.
- Avoid state that survives across independent compilations unless it is immutable configuration.
- Shared data objects must have one responsibility.
- Shared data must be injected or passed explicitly.
- Add tests that compile multiple independent inputs and prove state does not leak.

Risk:

A module can become order-dependent or compilation-history-dependent.

## Dialect-selection contract

A feature is not truly modular unless a dialect can include or exclude it predictably.

Rules:

- New features should be selectable through dialect/runtime composition when practical.
- Restricted dialects must reject syntax or runtime behavior that is not selected.
- Generic framework layers must not activate a feature through hardcoded module names.
- Capability reports explain selected behavior; they must not activate behavior.

Risk:

A feature can appear enabled even when the selected dialect did not request it.

## Backend-capability contract

Optimized lowering must be backend-capability gated.

Rules:

- Do not emit backend-specific intrinsics unless the selected backend supports them.
- The interpreter must remain the reference universal-call backend under the current architecture rules.
- If an optimization changes AIR or bytecode shape, prove semantic parity with the reference backend.
- Backend-specific fast paths must have fallback behavior or clear diagnostics.

Risk:

A program can work in CIL and fail in interpreter, or the interpreter can accidentally grow feature-specific intrinsic branches.

## Documentation contract

Every module with non-obvious behavior should document:

- its alias;
- its syntax ownership;
- its parser priority assumptions;
- its visitors;
- its bytecode/AIR output responsibilities;
- its backend/intrinsic requirements;
- its dialect inclusion and exclusion behavior.

This documentation can live in the module README, feature docs, or tests when the tests are named and structured as executable documentation.
