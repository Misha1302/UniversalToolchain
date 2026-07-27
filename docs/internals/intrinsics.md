---
title: Intrinsics
description: Document intrinsic capabilities and backend support.
---

# Intrinsics

Intrinsics are named operations that travel through AIR and are executed or compiled by a backend-specific intrinsic implementation.

They are not just strings in an instruction. In the current architecture, an intrinsic is a contract between emitters, optimizers, capability sets and backends.

## Place in the pipeline

Intrinsics appear after bytecode has been lowered to AIR:

```text
bytecode
  -> bytecode-to-AIR translator
  -> AIR instructions
  -> optimizers
  -> backend cil/interpreter
```

The fixed AIR opcode set is small. `UOpCode.Intrinsic` is used for operations that need a typed, named operation beyond `Push`, `Drop`, labels and jumps.

## Core idea

An intrinsic has several parts:

| Part | Purpose |
|---|---|
| intrinsic symbol | stable operation identity |
| type arguments | runtime type specialization, when needed |
| operands | operation-specific values or descriptors |
| capability metadata | whether a backend can consume the operation |
| executor/compiler implementation | backend behavior for the operation |

The important rule is simple: emitting an intrinsic is valid only when the selected backend path can consume it.

## Capability checks

Optimizer capability checks are performed through `IOptimizerIntrinsicCapabilityContext`.

The context answers questions of this shape:

```csharp
Supports(symbol, typeArguments)
```

The implementation delegates to an `IIntrinsicCapabilitySet` and converts runtime `Type` values into intrinsic type arguments.

For multiple requirements, `OptimizerCapabilityGuards.SupportsAll(...)` checks that every required symbol/type pair is supported.

## Why capability checks matter

Optimizers may replace general AIR shapes with more specific intrinsic instructions.

That is safe only when the backend supports the replacement. Otherwise, the optimizer would produce AIR that the selected backend cannot execute or compile.

Correct model:

```text
selected backend
  -> supported intrinsic capability set
  -> optimizer checks requirements
  -> optimizer emits backend-compatible intrinsic form
```

Incorrect model:

```text
optimizer emits intrinsic form
  -> hope every backend accepts it
```

The second model breaks backend parity and restricted backend support.

## Intrinsics and backends

Backends may support different intrinsic sets.

The interpreter executes intrinsic instructions through an interpreter intrinsic executor. The CIL backend compiles supported intrinsic instructions through a CIL intrinsic compiler/registry.

A backend-specific intrinsic must not be treated as a general language feature. It is a backend capability.

## Intrinsics and optimizers

Several optimizers depend on intrinsic support.

For example:

- arithmetic optimization can replace C# call descriptors with built-in arithmetic intrinsic instructions, but only when arithmetic intrinsic capabilities are supported for the relevant types;
- native CIL optimization can replace `Push` constants with typed load-constant intrinsics, but only when typed load constants are supported;
- comparison or boolean optimizers must respect the same capability boundary.

This is why `InitIntrinsicCapabilityContext` runs before `ProcessIr`.

## Intrinsics and type stack

AIR conversion and backend compilation both care about stack type behavior.

When AIR instructions are generated from bytecode, type-stack effects are applied after generated AIR instructions are appended. Backend compilers may also simulate stack types to determine return type, label stack state and IL shape.

An intrinsic therefore needs more than a name. It needs a predictable stack effect for every supported type shape.

## Intrinsics are not syntax

Do not expose intrinsics as if they were source-level syntax.

Source syntax belongs to lexer, parser and AST modules. Intrinsics belong to AIR/backend execution. A feature may lower to intrinsics, but users should not have to understand intrinsic internals to write ordinary Wist programs.

## What an intrinsic provider should define

A backend or module that introduces intrinsic behavior should define:

- intrinsic symbol identity;
- supported type arguments;
- stack effects;
- backend executor/compiler behavior;
- optimizer interaction;
- tests for supported and unsupported paths.

## Failure mode

Unsupported intrinsic paths should fail explicitly or be avoided by capability checks.

Silent fallback is dangerous because it can make a dialect appear to support a backend while actually executing through an unintended path.

## Common mistakes

- Treating an intrinsic identifier as a magic string without a producer/consumer contract.
- Emitting backend-specific intrinsics before checking capabilities.
- Assuming CIL-supported intrinsics are also interpreter-supported.
- Assuming interpreter-supported intrinsics are also CIL-supported.
- Using intrinsics to hide missing frontend semantics.
- Changing intrinsic stack behavior without parity tests.

## What to test

Intrinsic-affecting changes should test:

- supported symbol/type combinations;
- unsupported symbol/type combinations;
- stack type effects;
- interpreter execution when supported;
- CIL compilation when supported;
- optimizer behavior with and without required capabilities;
- semantic parity for dialects that expose both backends.

## Next

Continue with [Optimizers](/internals/optimizers).
