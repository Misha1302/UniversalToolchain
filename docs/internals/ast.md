---
title: AST
description: Explain AST structure and translation responsibilities.
---

# AST

The AST is the source-oriented structure produced by the parser.

It represents parsed language constructs before they are lowered into bytecode. This makes the AST the boundary between grammar ownership and executable semantics.

## Place in the pipeline

AST processing happens between parser output and binding/translation:

```text
parser.Parse
  -> modules.ProcessAst
  -> Binder
  -> modules.InitAstTranslator
  -> astTranslator.Translate
```

The AST should describe what the program says. It should not encode a concrete backend strategy.

## Current AST construction

The current parser starts by converting every lexeme into an AST node. These nodes are initially flat children under a `Program` root.

Parser node creators then reorganize those nodes into structured AST shapes:

```text
flat lexeme-derived nodes
  -> parser node creators
  -> structured AST
  -> tree validation
```

The exact shape depends on selected modules and parser creators.

## Module-owned nodes

A module that introduces syntax should also own the AST shape for that syntax.

Examples:

- arithmetic modules own arithmetic operation nodes;
- variable modules own declaration and assignment shapes;
- condition modules own conditional shapes;
- loop modules own loop shapes.

A later pipeline stage should not have to rediscover the syntax by scanning source text or guessing from unrelated child layouts.

## AST post-processing

Frontend modules can transform the AST through `ProcessAst`.

This hook should be used for AST-level normalization only. It should not be used to implement missing parser behavior or backend-specific lowering.

Good uses:

- normalize module-owned tree shape;
- combine related AST nodes when that is an AST concern;
- validate feature-owned shape before translation.

Bad uses:

- parse raw source text;
- emit bytecode semantics;
- insert backend-specific execution details;
- repair syntax that should have been handled by node creators.

## Binding

After AST post-processing, the pipeline applies external bindings through `Binder`.

This means AST visitors receive a bound tree. They should not independently reinterpret runtime parameters or merge external input values with local variable storage.

The boundary is important:

- external bindings come from the host/program input boundary;
- local variables come from program semantics;
- bytecode visitors should preserve that distinction.

## AST-to-bytecode visitors

Modules register AST visitors through `InitAstTranslator`.

The current translator gives registered visitors a chance to inspect nodes. Therefore visitors must self-filter:

```text
if this node is not mine
  -> return without emitting
else
  -> translate children and emit owned semantics
```

A visitor that emits for unrelated nodes can double-emit bytecode or consume another module's syntax.

## Child translation

Visitors often translate children before emitting the parent operation.

For expression-like constructs, this usually means:

```text
translate child expressions
  -> emit operation for parent node
```

The order is semantically important because bytecode and AIR are stack-oriented in many paths.

## Current implementation detail

The basic AST-to-bytecode translator creates one `Bytecode` instance for the translation request and uses a request-local translator wrapper to visit nodes.

This keeps the bytecode output shared across recursive visitor calls without making bytecode global state.

## What AST docs should not overclaim

Do not document every current node layout as a stable public language contract unless tests explicitly protect it.

It is safe to document:

- which module owns a syntax family;
- which broad shape the feature uses;
- which visitor consumes it;
- which invariants must hold.

It is risky to document:

- incidental child indexes without tests;
- parser implementation quirks as language guarantees;
- backend-specific expectations as AST requirements.

## What to test

AST-related changes should test:

- valid source produces executable behavior;
- malformed AST shapes fail before incorrect bytecode is emitted;
- visitors self-filter;
- nested constructs preserve intended structure;
- binding does not confuse external inputs with local variables;
- interpreter/compiler parity remains intact when AST shape changes.

## Next

Continue with [Bytecode](/internals/bytecode).
