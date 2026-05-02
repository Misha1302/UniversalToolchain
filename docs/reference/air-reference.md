---
title: AIR Reference
description: List AIR nodes, operations, and contracts.
---

# AIR Reference

This page lists the current AIR instruction contract.

For conceptual background, read [AIR internals](/internals/air) first.

## Instruction shape

An AIR instruction has:

| Field | Meaning |
|---|---|
| `UOpCode` | operation code |
| `Operands` | operation-specific operands |
| `Metadata` | additional metadata, currently implementation-facing |
| `Comment` | optional debugging/comment text |

Current implementation type:

```csharp
Instruction(UOpCode uOpCode, List<object>? operands = null, List<object>? metadata = null, string? comment = null)
```

## Opcode table

| Opcode | Operands | Stack effect | Control effect | Notes |
|---|---|---|---|---|
| `Nop` | none | none | none | no operation |
| `Push` | `[value]` | push value | none | value type affects CIL return/type simulation |
| `Drop` | none | pop top value | none | interpreter ignores empty stack; CIL path expects stack discipline |
| `Jmp` | `[labelId]` | none | jump to label | label id is currently `Guid` in normal control-flow paths |
| `JmpIf` | `[labelId]` | pop bool | jump when true | condition must be boolean-compatible |
| `JmpIfNot` | `[labelId]` | pop bool | jump when false | condition must be boolean-compatible |
| `Label` | `[labelId]` | none | mark target | interpreter precomputes positions; CIL defines/marks IL labels |
| `Annotate` | annotation operands | none | none | metadata-like instruction; not required runtime semantics |
| `Intrinsic` | `[intrinsicId, ...operands]` | intrinsic-specific | intrinsic-specific | backend support required |

## Builder methods

`GenericAbstractIR<TIdentifier>` exposes helper methods:

| Method | Emitted opcode |
|---|---|
| `Nop()` | `Nop` |
| `Push<T>(T value)` | `Push` |
| `Drop()` | `Drop` |
| `Jmp(identifier)` | `Jmp` |
| `JmpIf(identifier)` | `JmpIf` |
| `JmpIfNot(identifier)` | `JmpIfNot` |
| `SetLabel(label)` | `Label` |
| `Annotate(...)` | `Annotate` |
| `Intrinsic(instructionIdentifier, operands...)` | `Intrinsic` |
| `AppendInstructions(...)` | appends existing instructions |

## Stack contract

AIR uses a value stack model.

Important rules:

- expression producers must leave their result on the stack;
- consumers must pop the values they consume;
- `JmpIf` and `JmpIfNot` consume a condition;
- final execution returns the top value when a value remains;
- empty-stack final behavior is backend-specific but should be parity-tested.

## Label contract

Labels and jumps use an identifier operand.

Current normal control-flow paths use `Guid` labels. Backends must map those labels to their own execution representation:

- interpreter: instruction indexes;
- CIL: IL labels.

## Intrinsic contract

`Intrinsic` operands begin with an intrinsic identifier, followed by operation-specific operands.

The exact stack effect and runtime behavior depend on the intrinsic. A backend must support the intrinsic form before it can execute or compile it.

Do not assume `Intrinsic` is portable across all backends.

## Backend notes

| Backend | AIR consumption model |
|---|---|
| Interpreter | executes instructions sequentially with an instruction pointer, value stack and label map |
| CIL | simulates stack types, emits IL for each instruction and compiles a `DynamicMethod` |

## Stability note

The opcode enum is a developer-facing contract for backend and optimizer work. The exact object types inside operands and metadata are more implementation-sensitive and should be documented per feature/intrinsic when they matter.

## Related pages

- [AIR internals](/internals/air)
- [Backends](/internals/backends)
- [Backend Contracts](/reference/backend-contracts)
- [Intrinsics](/internals/intrinsics)
