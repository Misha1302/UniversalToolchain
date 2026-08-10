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

1. [Pipeline](/internals/pipeline) — the canonical lifecycle from `LanguageDefinition` and `LanguagePlan` to route execution.
2. [Lexer](/internals/lexer) — token recognition, registration order and ignored lexemes.
3. [Parser](/internals/parser) — AST construction through registered node creators.
4. [AST](/internals/ast) — source-level structure and visitor responsibilities.
5. [Bytecode](/internals/bytecode) — feature-owned semantic lowering before AIR.
6. [AIR](/internals/air) — backend-facing abstract instruction stream.
7. [Backends](/internals/backends) — interpreter and CIL execution paths.
8. [Intrinsics](/internals/intrinsics) — intrinsic identifiers, capability checks and type-stack effects.
9. [Optimizers](/internals/optimizers) — IR transformations and backend capability boundaries.
10. [Semantic Parity](/internals/semantic-parity) — why interpreter and compiler must agree.
11. [Dependency Injection](/internals/dependency-injection) — component materialization and service wiring.

## Canonical ownership chain

The current Wist path has one planning/runtime owner chain:

```text
.wistdialect / preset
  -> LanguageDefinition
  -> LanguageCompiler
  -> immutable LanguagePlan
  -> LanguageRuntime
  -> exact plan-owned route components
```

`LanguageCompiler` owns dependency closure, contribution selection/order, backend routes and runtime-provider selection. `LanguageRuntime` validates and materializes that already-selected graph; it does not re-plan it.

For Wist, the selected artifact route is:

```text
Source/Text
  -> Syntax/AST
  -> Bytecode
  -> AIR
  -> optimizer / optional SSA route steps
  -> backend artifact
  -> execution
```

The reusable frontend/lowering mechanics are exposed through narrow BasicCore contracts and `CanonicalArtifactStages`. There is no BasicCore end-to-end orchestration façade: `BasicCoreImpl` and `PreparedExecutionBuilder` are retired surfaces.

## Main internal actors

| Actor | Role |
|---|---|
| `LanguageCompiler` | Compiles a `LanguageDefinition` into the single immutable `LanguagePlan`. |
| `LanguageRuntime` | Verifies the plan and owns runtime/build-session lifecycle. |
| `LanguageArtifactBuildPipeline` | Executes the exact artifact route selected by the plan. |
| `ILanguageArtifactTransformer<,>` | Implements one typed route edge. |
| `ILanguageArtifactBuildTransformer` | Implements build-specific transformation when declared bindings are required. |
| `IFrontendCoreModule` | Participates in Wist text, lexeme, parser, AST and bytecode stages. |
| `ILexer` | Turns source text into lexeme values. |
| `IParser` | Turns lexemes into an AST through registered node creators. |
| `Binder` | Binds external inputs before translation. |
| `IAstToBytecodeTranslator` | Runs AST visitors and produces bytecode. |
| `IAbstractMethodsTranslator` | Converts bytecode methods/operations into AIR. |
| `IAirOptimizer` | Performs a selected AIR transformation under backend capability policy. |
| backend route component | Produces the plan-selected executable artifact. |
| route observer | Observes selected boundaries for verification/diagnostics without changing route ownership. |

## Current implementation vs. contract

Internals pages distinguish two kinds of statements:

- **Current implementation** — how the code works today.
- **Required invariant** — behavior that should be preserved by future changes.

Do not promote an implementation detail into a public guarantee unless tests and architecture rules already support it.

## Strong invariants

When changing internals, preserve these invariants:

- `LanguagePlan` is the sole selected semantic/runtime graph;
- runtime materialization uses exact plan-owned contributions and cannot add a second planner;
- module registration and execution order are deterministic;
- syntax is owned by lexer/parser/AST stages, not recovered from raw text later;
- visitors self-filter and emit only owned semantics;
- bytecode and AIR remain explicit, inspectable boundaries;
- optimizers are not required to make base semantics correct;
- backend-specific intrinsics do not leak into unsupported backends;
- interpreter and compiled execution agree where both routes promise parity;
- verification observers cannot alter contribution/route selection;
- no BasicCore file may recreate an end-to-end lexer→parser→lowering→backend→executor coordinator.

## What not to use this section for

This section is not a user tutorial, a complete language specification, a promise that every internal type is stable API, or a replacement for tests.

For user-facing examples, use [Wist Language](/wist/). For authoring guidance, use [Writing Modules](/write-modules/). For tables and contracts, use [Reference](/reference/).

## Next

Start with [Pipeline](/internals/pipeline).
