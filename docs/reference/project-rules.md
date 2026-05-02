---
title: Project Rules
description: Document coding, testing, and architecture rules.
---

# Project Rules

This page is a compact reference index for the canonical project rules.

The source of truth is [`docs/PROJECT_RULES.md`](/PROJECT_RULES). Do not treat this page as a full replacement for that file.

## Rule categories

| Category | Summary |
|---|---|
| Core principles | preserve universality, layering, determinism and composition boundaries |
| Language/comments | English-only comments, XML docs, diagnostics and technical text |
| Naming | PascalCase types/members, `_camelCase` private fields, `I...` interfaces |
| File layout | one main public type per file, file-scoped namespaces |
| Formatting | Allman braces, readable wrapping, guard clauses |
| Async | async methods use `Async`; `async void` only for true event handlers |
| Null handling | use `Thrower`-based guards; validate at public boundaries |
| Exceptions | all project exceptions go through `Thrower` or approved helpers |
| Public API docs | non-trivial public APIs should have XML documentation |
| Data flow | prefer read-only abstractions and boundary input normalization |
| Parser/AST | use safe child access, local parser mutations and processed-node markers |
| Type stack/IR | preserve stack order, type inference and backend intrinsic contracts |
| Architecture | avoid hidden authorities and concrete Wist/profile coupling in framework layers |
| DI/reflection | centralize reflection and keep behavior deterministic |
| Testing | add tests for non-trivial parsing, binding, intrinsic, optimizer, parity and DI changes |

## High-impact rules for documentation work

When updating documentation, preserve these rules:

- write English technical text in code and docs that behave as project references;
- do not document implementation details as stable public API unless tests support them;
- distinguish framework-level UniversalToolchain behavior from Wist convenience behavior;
- avoid implying that restricted dialect composition is a hardened sandbox;
- avoid performance claims without benchmark evidence;
- document current implementation details honestly when they are not yet stable contracts.

## High-impact rules for module work

When adding or changing modules:

- use stable aliases and runtime exports;
- keep syntax owned by lexer/parser/AST stages;
- use `SafeGet` where child access may be out of range;
- do not mutate ancestors outside allowed parser scope;
- mark processed nodes when required by parser flow;
- self-filter AST visitors;
- preserve bytecode/AIR stack discipline;
- add interpreter/compiler parity tests when both backends are supported.

## High-impact rules for backend and optimizer work

When changing backend or optimizer behavior:

- do not add intrinsics without backend support analysis;
- register type-stack behavior where required;
- check backend capabilities before emitting backend-specific intrinsic forms;
- keep optimizers semantics-preserving;
- avoid making optimizer output required for base correctness;
- test unsupported backend/intrinsic paths explicitly.

## Forbidden practices summary

The canonical rules forbid, among other things:

- direct exception throwing outside `Thrower` or approved Thrower helpers;
- non-English comments or XML docs;
- mixed null-check styles;
- hidden parser mutations outside allowed local scope;
- unsafe indexing where `SafeGet` is required;
- scattered reflection logic;
- magic strings for protocol-like behavior without a clear shared contract;
- stateful mutable static fields in new code;
- convenience layers that become hidden framework authorities;
- framework-level branching on concrete shipped profile ids when a general path exists.

## Contributor checklist

Before merging non-trivial code changes, verify:

- naming and formatting match project style;
- exceptions/null checks use project-approved mechanisms;
- public APIs are documented when non-trivial;
- parser/visitor/IR stack invariants are preserved;
- intrinsics have backend and type-processing support;
- tests cover behavior and parity where applicable;
- the change preserves universality rather than narrowing architecture for a local case.

## Related pages

- [Writing Modules](/write-modules/)
- [Internals](/internals/)
- [Semantic Parity](/internals/semantic-parity)
- [Backend Contracts](/reference/backend-contracts)
