---
title: Bytecode
description: Explain bytecode as a semantic intermediate layer.
---

# Bytecode

Bytecode is the intermediate layer produced from the bound AST before AIR translation.

It is where module-owned AST semantics become a more execution-oriented representation while still remaining above backend-specific implementation details.

## Place in the pipeline

Bytecode appears after AST visitors run and before bytecode-to-AIR translation:

```text
modules.InitAstTranslator
  -> astTranslator.Translate
  -> modules.ProcessBytecode
  -> optimizers.InitMethodsTranslator
  -> methodsTranslator.Translate(bytecode)
```

AST visitors produce bytecode. Modules may then post-process bytecode before AIR is built.

## What bytecode is for

Bytecode provides a shared semantic layer between source-level AST nodes and backend-facing AIR.

It is useful because it can:

- collect feature-owned semantics from multiple modules;
- preserve structure needed by later conversion;
- allow bytecode-level validation or normalization;
- keep backend-specific decisions out of AST nodes;
- feed a bytecode-to-AIR translator that produces backend-consumable instructions.

## Current translation model

The basic AST-to-bytecode translator:

1. creates a new `Bytecode` instance;
2. creates a request-local translator wrapper;
3. asks configured visitors to inspect the current AST node;
4. lets visitors recursively translate child nodes;
5. returns the accumulated bytecode.

The bytecode instance is shared for the translation request, not global.

## Visitor responsibility

A visitor that emits bytecode must:

- check whether the current AST node is owned by that visitor;
- translate children in the correct order;
- emit only feature-owned operations;
- leave stack expectations clear for later stages;
- avoid depending on unrelated visitors unless that cooperation is explicit and tested.

Visitors that do not self-filter are dangerous because multiple visitors can inspect the same node.

## Bytecode post-processing

`IFrontendCoreModule.ProcessBytecode` runs after AST translation.

Use it for bytecode-level work only:

- feature-owned bytecode normalization;
- validation of feature-owned instruction shapes;
- removal or rewriting of module-owned intermediate artifacts.

Do not use it to repair parser mistakes, rediscover syntax, or add backend-only behavior that should belong to AIR/backends.

## Bytecode vs. AIR

Bytecode and AIR have different jobs.

| Layer | Main concern |
|---|---|
| AST | source syntax structure |
| Bytecode | module-owned semantic lowering |
| AIR | backend-facing abstract instruction stream |

Bytecode may still contain higher-level operations or method-like convertables. AIR is the representation that interpreter and compiler paths consume.

## Stack discipline

Many bytecode paths ultimately lower into stack-oriented AIR. Therefore bytecode generation must preserve evaluation order.

For a binary expression, the usual pattern is:

```text
left expression
right expression
operation
```

If children are translated in the wrong order, interpreter and compiler results can diverge even when both backends compile successfully.

## Relationship with intrinsics

Some bytecode operations lower into intrinsic AIR instructions.

That does not mean bytecode should blindly emit backend-specific intrinsics. Backend capability checks and optimizer policies should happen in the IR/optimizer/backend path, not as hidden frontend assumptions.

## What bytecode docs should not overclaim

The current bytecode model is an internal representation. Do not document incidental implementation details as stable public API unless reference pages and tests make them explicit.

For stable documentation, focus on:

- stage ownership;
- visitor responsibilities;
- stack discipline;
- bytecode-to-AIR boundary;
- backend parity expectations.

## What to test

Bytecode-affecting changes should test:

- simple feature execution;
- nested feature execution;
- interpreter/compiler parity;
- disabled dialect behavior;
- bytecode post-processing does not affect unrelated modules;
- optimizer behavior is not required for base correctness;
- malformed syntax fails before invalid bytecode becomes valid AIR.

## Next

Continue with [AIR](/internals/air).
