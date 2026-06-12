# Semantic-parity regression case study

## The failure mode

The interpreter and CIL backend accepted the same Wist program, but external bindings and local variables could be mapped through incompatible storage assumptions. A small local-variable operation could therefore change how an external value was read or stored, causing the two execution paths to disagree.

The current code is fixed. The talk uses the preserved regression scenarios rather than intentionally breaking the current runtime on stage.

## Minimal preserved scenario

```text
let i = 0
i = i + 1
i = i + 1
i = i + 1
price + fee * i
```

With `price = 100.0` and `fee = 2.5`, both backends must produce `107.5`.

The focused regression fixture is:

```text
UniversalToolchain/Tests/Backends/InterpreterBindingsParityTests.cs
```

Relevant tests include:

- `ExternalBindings_ReadsMustWorkWithoutLocalContainerStorage`;
- `Reproducer_WithPriceFeeAndLocalLoopVariable_ShouldMatchCompilerInterpreterAndExpected`;
- `ShadowingAndNestedScope_WithLocalNamesOverlappingExternals_ShouldBeDeterministicAndParityStable`;
- `LocalVariable_WithExternalArithmetic_MustNotSwitchStorageContainer`;
- `LocalShadowing_MustUseIndependentStorageKeys`.

## Architectural invariant

External bindings and lexical local variables are distinct semantic entities. Their physical representation may differ by backend, but their identity and observable behavior must not depend on backend-specific slot allocation, declaration order, unused arguments, or local shadowing.

In practical terms:

- external names are resolved through an explicit binding contract;
- local variables use independent storage identities;
- shadowing creates a new lexical binding instead of overwriting an external binding;
- optimizers may change representation, not meaning;
- interpreter and compiler results are compared through shared parity infrastructure.

## Why this matters beyond Wist

Any system with an interpreter plus a compiled or optimized path can accidentally acquire two semantic definitions. The same class of failure appears in expression engines, rule runtimes, query compilers, template engines, and multi-tier language implementations.

The reusable lesson is to make binding/storage semantics explicit before optimization and to test backend parity across reordered inputs, unused inputs, repeated reads/writes, nested scopes, and shadowing.
