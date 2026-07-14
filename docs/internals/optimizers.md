---
title: Optimizers
description: Explain optimization passes and their contracts.
---

# Optimizers

Optimizers transform AIR while preserving observable program behavior.

In UniversalToolchain, optimizers implement the `IAirOptimizer` contract. They are selected by dialect/runtime composition and run after bytecode has been translated into AIR.

## Place in the pipeline

Optimizer-related hooks appear in three places:

```text
modules.ProcessBytecode
  -> optimizers.InitMethodsTranslator
  -> optimizers.InitIntrinsicCapabilityContext
  -> methodsTranslator.Translate(bytecode)
  -> current = optimizer.Optimize(current)
  -> compiler.Compile
```

This order matters. Optimizers may prepare the bytecode-to-AIR translator, receive backend capability context and then transform AIR before backend compilation.

## IAirOptimizer hooks

`IAirOptimizer` exposes three responsibilities:

| Hook | Purpose |
|---|---|
| `InitMethodsTranslator` | configure bytecode-to-AIR translation before AIR exists |
| `InitIntrinsicCapabilityContext` | receive backend intrinsic capability information |
| `Optimize` | transform AIR after translation and before backend compilation |

A module may use only one hook or all three.

## Optimizer is not semantics

An optimizer must not be required for base program correctness.

Correct layering:

```text
AST visitor emits correct bytecode
  -> bytecode translates to correct AIR
  -> optimizer improves AIR without changing meaning
  -> backend executes/compiles optimized AIR
```

Incorrect layering:

```text
AST/bytecode emits incomplete semantics
  -> optimizer repairs it
  -> program only works when optimization is enabled
```

The second model makes dialect behavior unpredictable and breaks non-optimized paths.

## Capability-gated optimization

Some optimizations produce intrinsic forms that only specific backends support.

The optimizer should ask the capability context before producing those forms:

```text
capabilityContext.Supports(symbol, typeArguments)
```

or use `OptimizerCapabilityGuards.SupportsAll(...)` when several requirements must be present.

If required capabilities are missing, the optimizer should return the original AIR unchanged.

## Example: arithmetic optimization

The arithmetic optimizer first requires an intrinsic capability context. Then it checks that the backend supports arithmetic intrinsic symbols for the supported numeric types.

Only after the capability check passes does it transform AIR.

Its transformations include:

- replacing certain C# call descriptors with built-in arithmetic intrinsic instructions;
- simplifying identity operations such as adding zero or multiplying by one;
- canonicalizing commutative operands;
- folding selected integer reassociation patterns.

This is intentionally capability-gated because the optimized instruction form must be supported by the selected backend.

## Example: native CIL loads

The native CIL optimizer can replace generic `Push` constants with typed load-constant intrinsics.

It does this only when the backend capability set supports `LoadConst` for the relevant types. Otherwise it leaves the original AIR unchanged.

This avoids emitting CIL-oriented intrinsic forms into a backend that cannot consume them.

## Optimizer order

Optimizer order can affect the final AIR shape.

Examples:

- one optimizer may introduce intrinsic forms another optimizer can simplify;
- native type lowering may make arithmetic optimization possible;
- a backend-specific optimizer may require prior normalization.

Therefore optimizer order must be deterministic and tested when behavior depends on it.

## Optimizer state

Optimizers may hold state such as a received capability context.

That state must be initialized before `Optimize` and must not leak across incompatible compilation paths. If an optimizer requires initialization, it should fail explicitly when used incorrectly.

Prefer request-scoped or build-scoped state. Be cautious with static mutable collections.

## Backend capability boundary

`Optimize` does not receive a concrete compiler. Backend awareness enters through the initialized intrinsic capability context, keeping optimizer logic independent from compiler implementations while still preventing unsupported rewrites.

## What optimizers must not do

Optimizers must not:

- parse source syntax;
- depend on AST node shapes;
- provide required base semantics;
- emit unsupported intrinsics;
- silently switch backend execution path;
- make restricted dialects broader;
- change observable behavior for the same source/input.

## What to test

Optimizer changes should test:

- unoptimized behavior still works;
- optimized behavior matches unoptimized behavior;
- required capabilities are checked;
- missing capabilities leave AIR unchanged or fail explicitly as designed;
- interpreter/compiler parity when both backends are exposed;
- optimized AIR does not contain backend-specific intrinsics for unsupported backends;
- deterministic output when optimizer order matters.

## Common mistakes

- Treating optimizer output as universally backend-compatible.
- Running optimizer logic before capability context is initialized.
- Adding a transformation without a negative capability test.
- Changing stack effects while preserving only the final happy-path result.
- Benchmarking an optimizer before proving semantic equivalence.

## Next

Continue with [Semantic Parity](/internals/semantic-parity).
