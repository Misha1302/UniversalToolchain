---
title: Build DSLs
description: Show how to compose existing modules into a new dialect.
audience: wist-dialect-author
status: current
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# Build DSLs

> **Scope:** this section documents Wist `.wistdialect` composition and embedding. For an independent non-Wist language, use [External Language Authoring](/language-authoring/).


UniversalToolchain is designed for building small, explicit DSL runtimes instead of hardcoding every rule, formula or workflow directly into application code.

This section explains how to assemble a DSL from existing Wist modules before writing new syntax or backend code.

## When to read this section

Read this section when you want to:

- create a smaller language surface than full Wist;
- expose formulas or rules to application users;
- restrict which syntax and runtime features are available;
- choose between interpreter and CIL execution;
- test that a dialect accepts only the intended programs.

If you need to add a brand-new syntax feature, read this section first and then continue with [Writing Modules](/write-modules/).

## The core idea

A UniversalToolchain-based DSL is assembled from capabilities:

- frontend modules that contribute lexemes and parser behavior;
- AST translation and bytecode/AIR lowering;
- optimizers;
- execution backends;
- dialect files that select the allowed surface.

The important distinction is that a dialect does not merely choose a display name. It selects what the runtime can actually recognize and execute.

```text
dialect file
  -> selected modules
  -> selected optimizers
  -> selected backends
  -> composed runtime host
  -> executable DSL program
```

## Minimal example

The smallest useful path is to start from an arithmetic dialect:

```text
dialect MinimalArithmetic
use Arithmetic,Numbers,Scopes,Whitespaces
backend interpreter
```

This dialect is intentionally narrow. It can run arithmetic expressions such as:

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

It should not accept syntax whose owning modules are not selected. For example, variables require `Identifier` and `Variables`; loops require loop-related modules; C# interop requires an explicit trusted runtime surface.

## What you usually build

Most useful DSLs fall into one of three shapes.

### Formula DSL

Use this for pricing, scoring, limits, thresholds or other numeric expressions.

Typical properties:

- narrow module set;
- no interop modules;
- deterministic input bindings;
- compiler mode only when performance matters and CIL support is available;
- negative tests for syntax that must remain unavailable.

### Workflow DSL

Use this for small procedural scripts with variables, branches or loops.

Typical properties:

- variables and identifiers;
- conditions and comparisons;
- loops only when they are truly needed;
- stronger tests for control flow and backend parity.

### Reference language profile

Use this when demonstrating Wist itself rather than restricting it for a production application.

Typical properties:

- broad module set;
- both interpreter and compiler backends;
- examples that show how UniversalToolchain composes a language;
- not suitable as the default surface for untrusted end-user formulas.

## Development order

Build a DSL in this order:

1. Start from a shipped dialect.
2. Remove everything you do not need.
3. Add one module group at a time.
4. Run a positive example after every change.
5. Add a negative example for syntax that must stay unavailable.
6. Enable the second backend only after the first one works.
7. Add backend parity tests when both modes are exposed.
8. Enable optimizers last.

This order keeps composition errors visible. It also prevents optimizers or backend-specific behavior from hiding missing semantics.

## Important boundaries

- A restricted dialect is a composition boundary, not a hardened sandbox.
- A capability marker should describe selected behavior, not secretly activate behavior.
- A backend must not change the observable meaning of a program.
- A module should own the syntax and semantics it introduces.
- Do not document removed or internal rule-declaration behavior as public runtime functionality.
- Public DSL examples must follow the runtime `.wistdialect` format used by shipped profiles.

## Pages in this section

- [Dialect Files](/build-dsls/dialect-files) explains the `.wistdialect` format.
- [Minimal DSL](/build-dsls/minimal-dsl) walks through a small arithmetic DSL.
- [Module Composition](/build-dsls/module-composition) explains how selected modules form a language surface.
- [Backend Selection](/build-dsls/backend-selection) explains interpreter, CIL and mode choice.
- [Testing a DSL](/build-dsls/testing-dsl) explains positive, negative and backend parity tests.
