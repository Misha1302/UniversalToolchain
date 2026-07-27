---
title: Internals Overview
description: Give a map of the compiler pipeline and runtime architecture.
---

# Internals Overview

> **Scope:** these pages primarily describe the Wist compiler/runtime implementation. Generic package planning and typed routes are documented under [External Language Authoring](/language-authoring/).


This section documents how UniversalToolchain currently works internally.

It is written for maintainers, backend authors, module authors and reviewers who need more than the public “source → execution” mental model.

## How to read this section

Read internals in this order:

1. [Pipeline](/internals/pipeline) — the real lifecycle from `CompilationInput` to compiled artifact/session.
2. [Lexer](/internals/lexer) — token recognition, registration order and ignored lexemes.
3. [Parser](/internals/parser) — AST construction through registered node creators.
4. [AST](/internals/ast) — source-level structure and visitor responsibilities.
5. [Bytecode](/internals/bytecode) — feature-owned semantic lowering before AIR.
6. [AIR](/internals/air) — backend-facing abstract instruction stream.
7. [Backends](/internals/backends) — interpreter and CIL execution paths.
8. [Intrinsics](/internals/intrinsics) — intrinsic identifiers, capability checks and type-stack effects.
9. [Optimizers](/internals/optimizers) — IR transformations and backend capability boundaries.
10. [Semantic Parity](/internals/semantic-parity) — why interpreter and compiler must agree.
11. [Dependency Injection](/internals/dependency-injection) — runtime composition and service wiring.

## What internals are for

Internals pages answer questions such as:

- where a module may extend the pipeline;
- which stage owns source syntax;
- when bytecode becomes AIR;
- what a backend is expected to consume;
- how optimizer capability checks fit into compilation;
- why semantic parity tests matter;
- which parts are current implementation details rather than stable public API.

## Canonical pipeline shape

The current core build path is orchestrated by `PreparedExecutionBuilder<TCompilationOutput>`.

At a high level:

```text
CompilationInput
  -> frontend module text/lexeme/AST hooks
  -> lexer
  -> parser
  -> binder
  -> AST-to-bytecode translation
  -> bytecode post-processing
  -> bytecode-to-AIR translation
  -> IR optimizers
  -> backend cil
  -> middle-end modules
  -> compiled artifact/session
```

This is more precise than the simplified public model:

```text
source -> lexer -> parser -> AST -> bytecode -> AIR -> backend -> result
```

The simplified model is useful for learning. Internals should use the precise lifecycle.

## Main internal actors

| Actor | Role |
|---|---|
| `BasicCoreImpl<TCompilationOutput>` | Public core façade over compile/prepare/run operations. |
| `PreparedExecutionBuilder<TCompilationOutput>` | Builds compiled artifacts and prepared execution sessions. |
| `IFrontendCoreModule` | Lets modules participate in text, lexeme, parser, AST and bytecode stages. |
| `ILexer` | Turns source text into lexeme values. |
| `IParser` | Turns lexemes into an AST through registered node creators. |
| `Binder` | Binds external inputs before translation. |
| `IAstToBytecodeTranslator` | Runs AST visitors and produces bytecode. |
| `IAbstractMethodsTranslator` | Converts bytecode methods/operations into AIR. |
| `IAirOptimizer` | Initializes intrinsic capability context and transforms AIR. |
| `IAbstractIrCompiler<T>` | Compiles AIR into a backend-specific artifact. |
| `IMiddleEndCoreModule<T>` | Initializes compiler/executor integration and post-processes compilation output. |
| `IExecutor<T>` | Executes a compiled artifact/session. |

## Current implementation vs. contract

Internals pages distinguish two kinds of statements:

- **Current implementation** — how the code works today.
- **Required invariant** — behavior that should be preserved by future changes.

For example:

- current parser implementation builds unknown nodes and rewrites them through node creators;
- required invariant: parser behavior must remain deterministic and module-owned;
- current CIL backend compiles AIR into `DynamicMethod`;
- required invariant: backend output must preserve AIR semantics.

Do not promote an implementation detail into a public guarantee unless tests and architecture rules already support it.

## Strong invariants

When changing internals, preserve these invariants:

- module registration and execution order must be deterministic;
- syntax must be owned by lexer/parser/AST stages, not recovered from raw text later;
- visitors must self-filter and emit only owned semantics;
- bytecode and AIR must remain inspectable enough for tests and optimizers;
- optimizers must not be required to make base semantics correct;
- backend-specific intrinsics must not leak into unsupported backends;
- interpreter and compiled execution must agree when both backends are supported;
- mutable state must be scoped to compilation/execution unless intentionally global and tested.

## What not to use this section for

This section is not:

- a user tutorial;
- a complete language specification;
- a promise that every internal type is stable API;
- a replacement for tests;
- a license to depend on undocumented internal ordering.

For user-facing examples, use [Wist Language](/wist/). For authoring guidance, use [Writing Modules](/write-modules/). For tables and contracts, use [Reference](/reference/).

## Next

Start with [Pipeline](/internals/pipeline).
