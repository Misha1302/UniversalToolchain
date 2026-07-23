---
title: What is UniversalToolchain?
description: Explain the framework, Wist facade and external language-authoring surface.
---

# What is UniversalToolchain?

UniversalToolchain is a .NET framework for composing language features, typed intermediate artifacts, deterministic execution routes and runtime providers. It is not one language and not only a parser library.

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

## When it fits

Use the framework when the main problem is not merely “evaluate a string”, but own a controlled language/runtime surface with explicit composition, multiple execution paths, reproducible plans or independently shipped extension packages.

Use a simpler expression evaluator, parser generator or handwritten implementation when those additional contracts are not needed.
