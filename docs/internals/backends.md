---
title: Backends
description: Explain interpreter and CIL execution backends.
---

# Backends

Backends consume AIR and produce executable behavior.

Current Wist runtime paths are centered around two backend styles:

- interpreter execution over AIR;
- CIL compilation into a `DynamicMethod`.

## Place in the pipeline

Backends run after AIR translation and IR optimization:

```text
methodsTranslator.Translate(bytecode)
  -> optimizers.ProcessIr
  -> middleEndModules.InitMethodsCompiler
  -> compiler.Compile
  -> middleEndModules.ProcessCompilation
  -> middleEndModules.InitExecutor
```

A backend should not parse source text or reinterpret frontend syntax. It consumes AIR.

## Shared backend contract

A backend must preserve AIR semantics.

This includes:

- stack behavior;
- labels and jumps;
- intrinsic behavior that the backend declares as supported;
- external binding shape;
- final result behavior.

When two backends are exposed for the same dialect, they should produce the same observable result for the same source and runtime inputs.

## Interpreter backend

The interpreter executes AIR instruction by instruction.

Current behavior:

1. create an `InterpreterState`;
2. attach the execution environment;
3. build label positions from AIR instructions;
4. execute instructions while the instruction pointer is inside the instruction list;
5. return `null` when the value stack is empty;
6. otherwise return the top value on the stack.

Supported instruction behavior includes:

| Opcode | Interpreter behavior |
|---|---|
| `Nop` | no operation |
| `Push` | push operand value |
| `Drop` | pop a value when present |
| `Jmp` | set instruction pointer to label position |
| `JmpIf` | pop condition and jump if true |
| `JmpIfNot` | pop condition and jump if false |
| `Label` | no direct runtime action |
| `Annotate` | no direct runtime action |
| `Intrinsic` | delegate to interpreter intrinsic executor |

The interpreter is useful as a semantic reference because its behavior maps directly to AIR instructions.

## CIL backend

The CIL backend compiles AIR into a `DynamicMethod`.

Current behavior:

1. simulate AIR stack types to determine return type;
2. create a `DynamicMethod` named `main`;
3. pass `IExecutionEnvironment` as the first argument;
4. add external binding types as additional arguments;
5. initialize labels and label stack states;
6. compile each AIR instruction into IL;
7. emit a method return compatible with the simulated return type.

The CIL backend is not just a faster interpreter. It has its own compilation contract and type-stack simulation path.

## CIL constants

For `Push`, the current CIL backend stores pushed constant values in a generic static holder and emits IL to load them by index.

This is an implementation detail. Do not document it as a public API contract, but keep it in mind when analyzing global state and compiled artifact behavior.

## Intrinsic support

Backends do not automatically support every intrinsic.

The CIL compiler exposes a supported intrinsic list through its registry. Optimizers and intrinsic policies must respect backend capability sets before producing backend-specific forms.

If an intrinsic is unsupported by a selected backend, the correct behavior is explicit failure or rejection, not silent fallback to another backend.

## Backend availability vs. backend parity

These are separate concerns:

- **availability**: whether a dialect exposes a backend at all;
- **parity**: whether two exposed backends agree on observable behavior.

A dialect may intentionally expose only interpreter or only CIL. In that case, unsupported modes should fail explicitly.

When a dialect exposes both, tests should compare interpreter and CIL results.

## What backends must not own

A backend should not own:

- source syntax;
- parser node selection;
- dialect module composition;
- feature availability decisions outside the canonical `LanguagePlan`;
- optimizer-only semantics required for correctness.

Backends may own:

- execution of AIR opcodes;
- compilation of AIR opcodes;
- backend-specific intrinsic implementation;
- backend-specific artifact creation;
- backend-specific type simulation.

## Common mistakes

- Treating interpreter as a silent fallback for failed CIL compilation.
- Adding CIL support without interpreter parity tests when both backends are exposed.
- Emitting backend-specific intrinsics before checking backend capability.
- Assuming `DynamicMethod` behavior defines language semantics.
- Making a frontend module depend on concrete backend artifact types.

## What to test

Backend-affecting changes should test:

- basic AIR opcode execution;
- labels and conditional jumps;
- final result behavior when stack is empty or non-empty;
- external binding argument shape;
- supported and unsupported intrinsic paths;
- interpreter/compiler parity for shared dialects;
- explicit failure for unavailable backend modes.

## Next

Continue with [Intrinsics](/internals/intrinsics).
