---
title: Intrinsics Reference
description: List supported intrinsics and backend capabilities.
---

# Intrinsics Reference

This page lists the current built-in intrinsic symbol families and documents how to treat intrinsic support.

For conceptual background, read [Intrinsics](/internals/intrinsics).

## Symbol shape

Built-in intrinsic symbols are represented as namespaced symbols such as:

```text
Core.LoadConst
Arithmetic.Add
Comparison.Equal
Storage.LoadLocal
```

Do not treat these as arbitrary strings scattered across the codebase. They are producer/consumer contracts between AIR emitters, optimizers and backends.

## Built-in symbol families

### Core

| Symbol | Purpose |
|---|---|
| `Core.LoadConst` | typed constant loading |
| `Core.CallCSharp` | C# method call descriptor path |
| `Core.CallCSharpCtor` | C# constructor call descriptor path |
| `Core.LoadExternal` | external binding load |
| `Core.StoreExternal` | external binding store |

### Arithmetic

| Symbol | Purpose |
|---|---|
| `Arithmetic.Add` | addition |
| `Arithmetic.Subtract` | subtraction |
| `Arithmetic.Multiply` | multiplication |
| `Arithmetic.Divide` | division |

### Comparison

| Symbol | Purpose |
|---|---|
| `Comparison.Equal` | equality comparison |
| `Comparison.NotEqual` | inequality comparison |
| `Comparison.Greater` | greater-than comparison |
| `Comparison.GreaterOrEqual` | greater-or-equal comparison |
| `Comparison.Less` | less-than comparison |
| `Comparison.LessOrEqual` | less-or-equal comparison |

### Boolean

| Symbol | Purpose |
|---|---|
| `Boolean.And` | boolean conjunction |
| `Boolean.Or` | boolean disjunction |
| `Boolean.Not` | boolean negation |

### Storage

| Symbol | Purpose |
|---|---|
| `Storage.LoadLocal` | local value load |
| `Storage.StoreLocal` | local value store |
| `Storage.LoadLocalRef` | local reference load |

## Capability model

Intrinsic support is capability-based.

A backend or optimizer path should check whether a symbol and type argument set is supported before emitting or consuming that intrinsic form.

Current optimizer-side checks use:

```text
IOptimizerIntrinsicCapabilityContext.Supports(symbol, typeArguments)
OptimizerCapabilityGuards.SupportsAll(...)
```

## Type arguments

Many intrinsics are type-specialized.

Examples:

- arithmetic intrinsics may be specialized for numeric runtime types;
- constant loading may be specialized for concrete primitive types;
- comparison and boolean intrinsics may have their own supported type shapes.

Do not assume a symbol is supported for every type.

## Backend support

Backend support is not universal.

| Backend | Intrinsic behavior |
|---|---|
| Interpreter | executes supported intrinsic instructions through interpreter intrinsic execution |
| CIL | compiles supported intrinsic instructions through the CIL intrinsic compiler/registry |

A dialect exposing both backends should not enable optimizer output that only one backend can consume unless the selected runtime plan keeps that output backend-specific and guarded.

## Optimizer interaction

Optimizers may introduce built-in intrinsic instructions from more general AIR shapes.

Examples:

- arithmetic optimization may rewrite C# call descriptors into `Arithmetic.*` intrinsic instructions;
- native CIL optimization may rewrite `Push` constants into `Core.LoadConst` instructions;
- comparison and boolean optimization may rewrite general calls into typed comparison/boolean intrinsics.

These rewrites must be capability-gated.

## What to document for new intrinsics

When adding a new intrinsic, document:

- symbol family;
- symbol name;
- expected operands;
- supported type arguments;
- stack effect;
- interpreter support;
- CIL support;
- optimizer producers;
- tests covering supported and unsupported cases.

## Common mistakes

- Assuming a symbol is supported by every backend.
- Assuming a symbol is supported for every type argument.
- Adding an optimizer rewrite without a capability check.
- Adding backend support without type-stack simulation support.
- Treating C# interop descriptors and built-in intrinsics as the same abstraction.

## Related pages

- [Intrinsics](/internals/intrinsics)
- [Optimizers](/internals/optimizers)
- [AIR Reference](/reference/air-reference)
- [Backend Contracts](/reference/backend-contracts)
