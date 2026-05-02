---
title: Bytecode Reference
description: List bytecode instructions and semantic tags.
---

# Bytecode Reference

This page summarizes the current bytecode representation used between AST translation and AIR conversion.

For conceptual background, read [Bytecode internals](/internals/bytecode).

## Stability note

Bytecode is an internal developer-facing layer. It is important for module authors and backend/optimizer work, but it should not be treated as a stable public user API.

## Core shape

Current bytecode is represented as:

```csharp
Bytecode(List<BytecodeInstruction> Instructions)
```

Each instruction is represented as:

```csharp
BytecodeInstruction(HashSet<string> Tags, LevelCollection<float, IAbstractMethodConvertable> Ops)
```

There is also a convenience constructor:

```csharp
BytecodeInstruction(IAbstractMethodConvertable op)
```

which creates an instruction with no tags and one operation at level `0`.

## Instruction fields

| Field | Meaning |
|---|---|
| `Tags` | string tags attached to the bytecode instruction |
| `Ops` | level-ordered collection of convertable operation fragments |

Tags are semantic markers. Operations are convertable fragments that can produce AIR.

## Convertable operation contract

Bytecode operations implement:

```csharp
public interface IAbstractMethodConvertable
{
    string Name { get; }
    IAbstractIR GetAbstractIR(Context context);
}
```

The conversion context currently exposes:

```csharp
Context(IReadOnlyList<Type> Stack)
```

This means a bytecode operation may use current stack type information while producing AIR.

## Bytecode-to-AIR conversion

Current conversion walks:

```text
Bytecode.Instructions
  -> BytecodeInstruction.Ops
  -> IAbstractMethodConvertable.GetAbstractIR(context)
  -> append generated AIR instructions
  -> apply AIR instruction type-stack effects
```

The converter keeps a type stack during conversion. Generated AIR instructions are appended to the result and then type effects are applied.

## Tags

Tags are currently string-based.

Use tags only when they have an owner and a consumer. A tag that is produced but never consumed is dead metadata; a consumer that depends on undocumented tags creates hidden coupling.

For new tags, document:

- owning module;
- producer;
- consumer;
- expected shape;
- tests that prove the contract.

## Operation levels

`Ops` uses `LevelCollection<float, IAbstractMethodConvertable>`. This allows more than one convertable operation to be associated with one bytecode instruction and ordered by level.

Do not depend on incidental operation order. If level order affects semantics, cover it with tests.

## Relationship to AIR

Bytecode is not AIR.

| Layer | Shape |
|---|---|
| Bytecode | tagged instructions with convertable operation fragments |
| AIR | concrete `Instruction` list with `UOpCode`, operands, metadata and comments |

Bytecode is module-owned semantic lowering. AIR is backend-facing instruction stream.

## Module author checklist

When emitting bytecode from an AST visitor:

- self-filter before emitting;
- translate children in the intended order;
- emit only feature-owned operations;
- keep tags owned and documented;
- do not emit backend-specific AIR directly from parser logic;
- test the resulting AIR/backend behavior through execution or converter tests.

## Common mistakes

- Treating bytecode tags as public language syntax.
- Using tags as magic strings without producer/consumer tests.
- Depending on optimizer behavior to make bytecode meaningful.
- Repairing parser mistakes during bytecode generation.
- Emitting operations whose stack effects are not understood by AIR conversion.

## Related pages

- [Bytecode internals](/internals/bytecode)
- [AIR Reference](/reference/air-reference)
- [Writing Modules: Bytecode Generation](/write-modules/bytecode-generation)
- [Writing Modules: Semantic Tags](/write-modules/semantic-tags)
