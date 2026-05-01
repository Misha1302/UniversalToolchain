---
title: Bytecode Generation
description: Show how a feature lowers into bytecode.
---

# Bytecode Generation

Bytecode generation is where an AST feature becomes executable semantics.

For module authors, the most important rule is stack and ownership discipline: emit only the operations your feature owns, in the order expected by the backend pipeline.

## When to read this page

Read this page after [AST Nodes](/write-modules/ast-nodes) when a feature already has parser/AST ownership and now needs runtime behavior.

## Goal

Understand how AST visitors lower feature-owned nodes into bytecode without leaking syntax assumptions into later stages.

## Where bytecode is produced

Frontend modules usually register AST visitors through `InitAstTranslator`:

```csharp
public void InitAstTranslator(IAstToBytecodeTranslator translator) =>
    translator.AddVisitors(...);
```

A visitor receives a node and decides whether it owns that node. If it does, it translates children and emits bytecode instructions.

A simplified lowering path looks like this:

```text
AST node
  -> owning visitor
  -> translate child nodes
  -> emit feature-owned bytecode instructions
  -> bytecode post-processing
  -> AIR/backend path
```

## Example: arithmetic lowering

Arithmetic lowering follows a simple shape:

1. Check whether the AST node is an arithmetic operation.
2. Translate child nodes first.
3. Emit the arithmetic operation instruction.

Conceptually:

```text
left operand
right operand
operation
```

This stack-oriented order matters. If children are translated in the wrong order, or if the operation is emitted too early, interpreter and compiler behavior can diverge.

## Visitor self-filtering

A bytecode visitor must return without emitting when it does not own the node:

```csharp
if (!IsOwnedNode(data.Node))
    return;
```

This is not optional. Translators may invoke multiple visitors, and a visitor that emits for unrelated nodes can create duplicate or invalid bytecode.

## Stack discipline

Bytecode generation should respect the expected stack behavior of the feature.

For expression-like features:

- child expressions usually push values;
- the parent operation consumes those values;
- the parent operation pushes the result or produces a documented control-flow effect.

For statement-like or control-flow features:

- define what values remain on the stack;
- document whether a body result is preserved or discarded;
- test both interpreter and compiler paths.

Never rely on accidental stack cleanup by a later backend.

## Bytecode post-processing

`IFrontendCoreModule` includes a `ProcessBytecode` hook:

```csharp
Bytecode ProcessBytecode(Bytecode current) => current;
```

Use this only when the module genuinely owns a bytecode-level transformation. Do not use post-processing as a workaround for missing parser or AST ownership.

Good use:

- normalize feature-owned bytecode;
- validate feature-owned instruction shape;
- apply a documented transformation with tests.

Bad use:

- scan instructions to guess missing syntax;
- rewrite unrelated module instructions;
- hide backend incompatibilities;
- depend on previous compilations or global mutable state.

## Backend awareness

Bytecode is consumed by backend paths. A feature is not complete until intended backends understand the emitted semantics.

When both compiler and interpreter are enabled, add parity tests:

```text
same source
  -> interpreter result
  -> compiler result
  -> compare observable behavior
```

If a feature is intentionally supported by only one backend, document that limitation and test that unsupported modes fail explicitly.

## Optimizers are not semantics

Optimizers may improve or normalize bytecode/AIR, but they must not be required to make basic semantics correct.

Build in this order:

1. parser/AST ownership;
2. unoptimized bytecode semantics;
3. backend execution;
4. parity tests;
5. optimizers.

If a program works only after an optimizer is enabled, the base feature is probably incomplete.

## What to test

For bytecode generation, test:

- simple valid source;
- nested source;
- malformed source;
- disabled dialect behavior;
- interpreter execution when supported;
- compiler execution when supported;
- parity when both backends are supported;
- optimizer safety when optimizers touch the feature.

## Common mistakes

- Emitting bytecode for nodes not owned by the visitor.
- Depending on child order without a test.
- Leaving unexpected values on the stack.
- Fixing parser mistakes in bytecode post-processing.
- Adding compiler behavior but forgetting interpreter behavior.
- Using optimizers as required semantic lowering.

## Next

Continue with [Semantic Tags](/write-modules/semantic-tags).
