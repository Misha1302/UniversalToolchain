---
title: What is UniversalToolchain?
description: Explain the framework, Wist facade and external language-authoring surface.
---

# What is UniversalToolchain?

UniversalToolchain is a .NET compiler and runtime framework for composing language implementations from package-owned features, contributions, typed artifacts, backends and runtime providers. It is not one language and not only a parser library.

## Configuration time is a separate problem

```text
package descriptors + registrations
  -> LanguageDefinition
  -> LanguageCompiler
  -> immutable LanguagePlan
  -> LanguageRuntime (exact selected implementations)
  -> typed routes for compiling/executing input artifacts
```

`LanguageCompiler` closes declared dependencies, checks capabilities and slot ownership, plans type-compatible backend routes and resolves a runtime provider. `LanguagePlan` captures the selected configuration. Runtime materialization verifies matching package/component sources rather than silently selecting a new graph. This is different from parsing a source file.

[Feature, Contribution, Package, Capability, Slot, artifact contract and runtime provider](/language-authoring/package-model) are generic composition terms. Wist modules are reference-language implementations; the generic SDK does not require Wist's AST, Bytecode or AIR.

## Two current entry surfaces

### Wist

Wist is the reference language and first-contact product surface. `UniversalToolchain.Wist` validates restricted formulas, returns structured diagnostics and compiles approved expressions into typed delegates.

### External Language Authoring SDK

The generic SDK lets an external project define a non-Wist language package through typed artifact kinds, features, contributions, transformers, passes, backends and a runtime provider. The compiler resolves these inputs into an immutable `LanguagePlan` before runtime creation.

## What the framework owns

- stable IDs and typed artifact contracts;
- package, feature and contribution metadata;
- dependency, conflict, slot and capability resolution;
- deterministic route planning and pass ordering;
- package-manifest and plan identity;
- exact backend executor selection;
- runtime policy validation and component lifecycle;
- Wist-specific lexer/parser/Bytecode/AIR pipeline through the reference language stack.

## What a language author still owns

- grammar and parsing implementation;
- syntax/semantic artifact types;
- binding and type rules;
- transformations and compiler passes;
- backend executors and observable semantics;
- trust model for third-party components.

## What a successful plan does not prove

The planner can diagnose multiple eligible capability providers, competing single-owner slots, ambiguous equal-cost routes, incompatible typed contracts and infeasible pass order. These are structural guarantees, not automatic proof of optimizer correctness, backend parity, isolation from untrusted extensions or runtime speed. See [planning diagnostics](/language-authoring/contribution-planning), [security](/SECURITY) and [limitations](/limitations).

[Acme.PricingLanguage](/language-authoring/quickstart) provides an existing non-Wist parser with interpreter and compiled routes.

## When it fits

Use the framework when the main problem is not merely “evaluate a string”, but own a controlled language/runtime surface with explicit composition, multiple execution paths, reproducible plans or independently shipped extension packages.

Use a simpler expression evaluator, parser generator or handwritten implementation when those additional contracts are not needed.
