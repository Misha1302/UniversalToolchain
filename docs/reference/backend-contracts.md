---
title: Backend Contracts
description: Document what each backend must support.
---

# Backend Contracts

This page summarizes backend obligations for current UniversalToolchain/Wist execution paths.

For conceptual background, read [Backends](/internals/backends) and [Semantic Parity](/internals/semantic-parity).

## Backend modes

Current Wist-facing modes:

| Mode | Meaning |
|---|---|
| `interpreter` | execute AIR through the interpreter backend |
| `cil` | compile through the CIL backend when the selected runtime exposes CIL |

Dialect files decide which backend modes are available for a runtime host.

## Shared contract

Every backend must preserve:

- AIR opcode semantics;
- value stack behavior;
- label and jump behavior;
- external binding shape;
- final result semantics;
- supported intrinsic behavior;
- explicit failure for unsupported operations.

## Interpreter contract

The interpreter backend:

| Area | Contract |
|---|---|
| input | AIR plus execution environment |
| state | instruction pointer, value stack, label positions, execution environment |
| labels | built before execution from `Label` instructions |
| execution | sequential dispatch over AIR instructions |
| final result | `null` when stack is empty, otherwise top stack value |
| intrinsics | delegated to interpreter intrinsic executor |

The interpreter is useful as a semantic reference because behavior maps directly to AIR instructions.

## CIL contract

The CIL backend:

| Area | Contract |
|---|---|
| input | AIR plus declared compilation input |
| output | `DynamicMethod` |
| arguments | `IExecutionEnvironment` first, then external binding argument types |
| return type | derived by AIR type simulation |
| labels | converted to IL labels with simulated stack state |
| intrinsics | compiled through CIL intrinsic compiler/registry |
| constants | current implementation stores pushed constants in generic static holders |

The CIL backend is a compiler, not a second interpreter. Its type simulation and IL emission must still preserve AIR semantics.

## Backend availability

Availability is selected by dialect/runtime composition.

A dialect exposing only `interpreter` should reject `cil` mode.

A dialect exposing only CIL/CIL mode should reject `interpreter` mode.

Do not silently fall back to a different backend when the requested mode is unavailable.

## Backend parity

When a dialect exposes both backend modes, they must agree on observable behavior.

Parity should cover:

- successful values;
- comparable failures;
- external bindings;
- variables;
- conditions;
- loops;
- intrinsic-heavy programs;
- optimizer-enabled programs.

## Intrinsic support

A backend is not required to support every intrinsic.

Backend-specific intrinsic support must be represented through capability sets or supported intrinsic lists. Optimizers and emitters must check those capabilities before producing backend-specific intrinsic forms.

## Unsupported operations

Unsupported backend operations should produce explicit failure.

Bad behavior:

```text
requested compiler
  -> compiler cannot consume AIR
  -> silently run interpreter
```

Good behavior:

```text
requested compiler
  -> compiler cannot consume AIR
  -> fail with a clear unsupported operation/backend diagnostic
```

## Backend author checklist

A backend implementation should provide or document:

- supported AIR opcodes;
- supported intrinsic symbols/type arguments;
- stack behavior;
- label/jump behavior;
- external binding argument handling;
- final result handling;
- failure behavior for unsupported instructions;
- parity tests against existing backends when sharing dialects.

## Related pages

- [AIR Reference](/reference/air-reference)
- [Backends](/internals/backends)
- [Intrinsics](/internals/intrinsics)
- [Semantic Parity](/internals/semantic-parity)
