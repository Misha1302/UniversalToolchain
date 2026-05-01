---
title: Pipeline
description: Explain source to execution pipeline.
---

# Pipeline

UniversalToolchain separates language processing into explicit stages. Each stage has a narrow job, produces a more structured representation, and gives modules a controlled place to extend behavior.

The practical shape is:

```text
source text
  -> lexer
  -> parser
  -> AST
  -> bytecode
  -> AIR
  -> backend
  -> result
```

This page explains what each stage is responsible for and where a developer should normally make changes.

## Why the pipeline is split

The project is not designed as a single hardcoded Wist compiler. Wist is a reference language built on top of the UniversalToolchain infrastructure. The split pipeline keeps three concerns separate:

- language syntax and parsing;
- intermediate semantics and optimization;
- execution by a concrete backend.

That separation is what allows a dialect to be assembled from modules instead of being baked into one compiler class.

## Stage overview

| Stage | Input | Output | Main responsibility |
| --- | --- | --- | --- |
| Lexer | Source text | Tokens | Recognize lexical units while preserving enough information for the parser. |
| Parser | Tokens | AST | Build a structured syntax tree using module-provided parsing behavior. |
| AST | Parsed structure | AST nodes | Represent source-level language constructs before lowering. |
| Bytecode | AST nodes | Bytecode operations | Normalize module semantics into a compact executable/intermediate form. |
| AIR | Bytecode | Abstract IR | Represent backend-oriented operations and enable backend-safe processing. |
| Backend | AIR | Execution result | Interpret or compile the program while preserving the same semantics. |

## Lexer

The lexer is the first boundary between raw text and the rest of the compiler. It should remain simple and deterministic: it recognizes tokens, but it should not decide high-level language meaning.

A lexer change is usually appropriate when a dialect needs a new token category, a new literal form, or different whitespace/comment handling.

## Parser

The parser consumes tokens and produces AST nodes. This is where syntax becomes structure.

Parser extensions should avoid global assumptions about the whole language. A module should claim only the syntax it owns and should cooperate with other modules through deterministic ordering and priority rules.

When a parsing bug appears, the first question should be whether the syntax decision belongs in the parser or whether it is actually a later semantic/lowering concern.

## AST

The AST is source-oriented. It should describe what the program says, not how a backend will execute it.

Good AST nodes are useful for:

- making source constructs explicit;
- keeping module-specific syntax isolated;
- delaying backend decisions until lowering;
- writing tests around language behavior before execution details matter.

A common mistake is to put backend knowledge into AST nodes. That makes a feature harder to run through both interpreter and compiled backends.

## Bytecode

Bytecode is the first major normalization layer. It lowers AST-level constructs into operations that are easier to translate, inspect, and execute.

Bytecode is useful because it gives the system a stable semantic layer between syntax and backend-specific execution. It can carry tags or metadata that describe meaning without forcing every backend to rediscover it from syntax.

This is the right place to encode language semantics that should be shared by different backends.

## AIR

AIR, the Abstract Intermediate Representation, is closer to execution than bytecode. It is the representation backends consume or compile.

AIR should be backend-neutral, but backend-aware enough to support efficient execution. That means it can expose operations that are meaningful to optimizers and backends without becoming CIL-specific, interpreter-specific, or tied to a single runtime strategy.

The important rule is: AIR may prepare work for backends, but it should not leak one backend's private implementation into the whole pipeline.

## Optimizers and intrinsics

Optimizers should operate on representations where their assumptions are explicit. Intrinsics should be treated as contracts: a backend may support a fast path only when it can preserve the same observable semantics.

A safe optimization has three properties:

- it is guarded by backend capabilities;
- it does not change interpreter/compiler parity;
- it has tests proving both optimized and unoptimized behavior.

This matters especially when adding native arithmetic, local-variable optimizations, or backend-specific fast paths.

## Backends

Backends execute the program represented by AIR.

The interpreter backend is useful as a semantic reference: it should make behavior clear and testable. A compiled backend can then optimize execution, but it must not silently change language meaning.

The main backend contract is semantic parity. If the interpreter and compiler disagree on the same dialect, input, and runtime bindings, the pipeline has a correctness bug.

## Where to make changes

| Change | Preferred place |
| --- | --- |
| New token syntax | Lexer module |
| New expression or statement grammar | Parser extension / frontend module |
| New source-level construct | AST node and parser extension |
| Shared language semantics | Bytecode lowering |
| Backend-facing execution operation | AIR translation |
| Runtime acceleration | Optimizer or backend intrinsic |
| Backend-specific implementation | Backend only |
| Cross-backend correctness rule | Semantic parity tests |

## Professional invariants

A pipeline change should preserve these invariants:

1. Module order is deterministic.
2. Parsing does not depend on accidental service registration order.
3. AST nodes do not contain backend-specific execution logic.
4. Bytecode and AIR keep semantics explicit enough for tests and optimizers.
5. Interpreter and compiled execution are tested against the same cases.
6. Mutable state is scoped to one compilation/execution request unless explicitly designed otherwise.
7. Backend-specific fast paths are capability-checked before use.

## Minimal validation checklist

After changing the pipeline, run at least:

```bash ci-run=false
dotnet build UniversalToolchain/Wist.sln -c Release
dotnet test UniversalToolchain/Wist.sln -c Release --no-build
npm run docs:build
```

Use more targeted tests when the change affects a specific module, dialect, optimizer, or backend.

## Next

Continue with [Lexer](/internals/lexer) or [Bytecode](/internals/bytecode).
