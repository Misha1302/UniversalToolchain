---
title: AIR
description: Explain Abstract IR and backend-independent lowering.
---

# AIR

AIR means Abstract Intermediate Representation.

It is the backend-facing instruction stream produced from bytecode after method/operation conversion and before backend compilation or interpretation.

## Place in the pipeline

AIR is produced after bytecode post-processing and before IR optimizers/backend compilation:

```text
modules.ProcessBytecode
  -> optimizers.InitMethodsTranslator
  -> optimizers.InitIntrinsicCapabilityContext
  -> methodsTranslator.Translate(bytecode)
  -> optimizers.ProcessIr
  -> compiler.Compile
```

The backend consumes AIR, not source text, tokens or AST nodes.

## Current instruction model

The core AIR instruction set is intentionally small:

| Opcode | Purpose |
|---|---|
| `Nop` | no operation |
| `Push` | push a value onto the runtime stack |
| `Drop` | drop the top value from the runtime stack |
| `Jmp` | unconditional jump to a label |
| `JmpIf` | jump when the popped condition is true |
| `JmpIfNot` | jump when the popped condition is false |
| `Label` | mark a jump target |
| `Annotate` | carry metadata-like annotations |
| `Intrinsic` | represent an operation handled by an intrinsic executor/compiler |

An AIR instruction stores:

- opcode;
- operands;
- metadata;
- optional comment.

## Generic AIR builder

The generic AIR implementation accumulates instructions and exposes helper methods such as:

```text
Nop
Push
Drop
Jmp
JmpIf
JmpIfNot
SetLabel
Annotate
Intrinsic
AppendInstructions
```

This makes AIR simple to construct while keeping the instruction stream explicit.

## Bytecode-to-AIR conversion

The current bytecode-to-AIR converter does not simply rename bytecode instructions.

It walks through:

```text
bytecode instructions
  -> instruction operations
  -> convertable operation fragments
  -> convertable.GetAbstractIR(context)
  -> append generated AIR instructions
  -> apply type-stack effects
```

The converter maintains a type stack while converting. After generated AIR instructions are appended, `InstructionTypeStackApplier` applies the type effects of those instructions.

This is important for later optimizer and backend correctness.

## Stack behavior

AIR is stack-oriented for values.

Examples:

- `Push` adds a value to the stack;
- `Drop` removes a value;
- `JmpIf` and `JmpIfNot` consume a boolean condition;
- `Intrinsic` behavior depends on the intrinsic identifier and operands.

Backends must preserve the same observable stack behavior.

## Control flow

Control flow uses labels and jumps:

```text
Label(id)
Jmp(id)
JmpIf(id)
JmpIfNot(id)
```

The interpreter resolves label positions before execution. The CIL backend defines and marks IL labels while compiling AIR.

## Intrinsics

`Intrinsic` represents operations that are not covered by the small fixed opcode set.

This does not mean every backend supports every intrinsic. Intrinsic support is a backend capability and must be checked before optimizers or emitters produce backend-specific intrinsic forms.

## Optimizer boundary

Optimizers run on AIR after bytecode-to-AIR translation.

A valid optimizer:

- preserves observable semantics;
- respects backend capability sets;
- does not make base semantics depend on optimization;
- does not emit intrinsic forms unsupported by the selected backend.

## AIR vs. backend-specific IR

AIR is backend-facing but should not become one backend's private representation.

For example, the CIL backend may simulate stack types and emit `DynamicMethod` IL, but AIR itself should remain readable by interpreter and compiler paths when both are supported.

## Common mistakes

- Treating `Intrinsic` as automatically supported by all backends.
- Adding optimizer output without checking backend capabilities.
- Assuming AIR is source-level syntax.
- Relying on comments or annotations for required semantics.
- Changing stack behavior without interpreter/compiler parity tests.

## What to test

AIR-affecting changes should test:

- opcode shape and operands;
- stack effects;
- label/jump behavior;
- intrinsic support boundaries;
- optimized and unoptimized behavior where possible;
- interpreter/compiler parity when both backends support the dialect.

## Next

Continue with [Backends](/internals/backends).
