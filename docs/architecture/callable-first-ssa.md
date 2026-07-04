# Callable-first SSA architecture

Status: pre-release foundation with callable instruction, descriptor-driven
constant folding, constants-only SSA core descriptors, managed callable
descriptors, a no-optimization SSA roundtrip route and the first
capability-checked callable lowering planner in code.

The current SSA preview route remains:

```text
AIR -> SSA -> SSA optimization -> AIR
```

The no-optimization release route is also available:

```text
AIR -> SSA -> AIR
```

Both routes are intentionally explicit. The architecture direction changes what
SSA is allowed to treat as its semantic foundation.

## Core decision

SSA core is structural. Semantics are descriptor-driven.

SSA core owns:

- value identity;
- block identity;
- block arguments;
- data dependencies;
- control-flow edges;
- call/result shape;
- terminator shape;
- verifier hooks;
- fact and capability propagation.

SSA core must not own:

- an arithmetic operation universe;
- a primitive type universe;
- Wist runtime classes;
- .NET method semantics;
- backend-specific intrinsic names;
- optimizer legality by operation-name matching.

Arithmetic, managed calls, constructors, runtime calls, standard-library
functions, user functions and backend intrinsics should be represented as
ordinary callables with semantic descriptors.

## Implemented foundation in this increment

This increment adds the first foundation layer without replacing the existing
preview pipeline:

- `UniversalToolchain.Semantics.Abstractions`
  - `SemanticTypeId`;
  - `CallableId`;
  - `SemanticTypeDescriptor`;
  - `CallableDescriptor`;
  - `CallableSignature`;
  - `SemanticEffectSummary`;
  - `AlgebraicTraits`;
  - `SemanticTypeTraits`;
  - `SemanticTrustLevel`;
  - `ConstantValue`;
  - `IConstantEvaluator`.
- `UniversalToolchain.StandardSemantics`
  - optional standard int32/bool/float64 descriptors;
  - optional int32 callable descriptors such as `std.i32.add.unchecked`;
  - `StandardInt32ConstantEvaluator` for trusted deterministic int32
    arithmetic callables.
- `UniversalToolchain.Ssa.Abstractions`
  - `ISsaInstruction`, the common ordered instruction contract;
  - `SsaCall`, the long-term descriptor-driven operation shape;
  - `SsaManagedCallableAttribute`, which lets trusted managed methods and
    constructors declare effects, determinism, algebraic traits and trust;
  - `SsaManagedCallables`, which maps supported `MethodInfo` and
    `ConstructorInfo` values to stable managed `CallableId` values and
    descriptors;
  - `SsaCoreDescriptors.ConstantMaterialization`, which describes only
    materialized constants;
  - `SsaBlock.Instructions`, with `Operations` and `Calls` kept as
    compatibility projections.
- `UniversalToolchain.Ssa.Core`
  - `SsaSemanticCallVerifier`, which verifies `SsaCall` against a semantic
    descriptor snapshot;
  - `StructuralSsaVerifier` support for mixed `SsaOperation` / `SsaCall`
    blocks, duplicate instruction ids, dominance and same-block
    use-before-definition.
- `UniversalToolchain.Air.Analysis`
  - `AirIntrinsicDescriptor`;
  - `AirIntrinsicDescriptorSet`;
  - stack analysis for explicitly described AIR intrinsics.
- `UniversalToolchain.Ssa.Lowering` / `UniversalToolchain.Ssa.Emission`
  - preview int32 arithmetic AIR intrinsics are lowered to `SsaCall`;
  - preview int32 arithmetic `SsaCall` instructions are emitted back to
    verifiable AIR intrinsics;
  - supported managed static methods, instance methods with a stack receiver,
    and constructors are represented as managed `SsaCall` instructions and can
    be emitted back to the ordinary AIR `call C#` / `call C# ctor` lowering
    surface;
  - `SsaCallableLoweringPlanner` selects the best supported target by priority
    and reports ambiguity only inside the best priority bucket;
  - AIR intrinsic targets require matching callable, lowering candidate and
    AIR intrinsic descriptor shapes;
  - managed-call targets require the resolved `MethodInfo` / `ConstructorInfo`
    descriptor to match the callable signature exactly, including parameter and
    result types;
  - CIL opcode and interpreter-primitive targets are modeled explicitly, but
    rejected by the current AIR route.
- `UniversalToolchain.Ssa.Optimization`
  - `SsaRoundtripRoute` exposes `Off`, `Prefer`, `Require` and `Debug` policies
    for `AIR -> SSA -> AIR` without running optimization passes.

The minimal SSA optimizer preserves callable instructions in order. The
SSA -> AIR emitter rejects unsupported callable instructions with explicit
diagnostics such as `ssa.to-air.call-lowering.missing`,
`ssa.to-air.call-lowering.unsupported-target` or
`ssa.to-air.managed-call-descriptor.shape`. This avoids silently dropping calls
that do not yet have a valid route target.

`SsaConstantFoldingPass` now folds through callable descriptors and an
`IConstantEvaluator`:

- `SsaCall` is evaluated by its `CallableId`;
- the pass checks that the descriptor is pure, deterministic and trusted before
  evaluating constants;
- untrusted descriptors are not folded, even if an evaluator exists.
- trusted pure managed callables can be folded when a bounded evaluator is
  explicitly provided for their descriptor.

Legacy preview arithmetic operation ids such as `core.add`, `core.sub`,
`core.mul` and `core.eq` are no longer public SSA known ids and are not part of
the active core descriptor route. Constants are still materialized as
`SsaOperation` until the constant model is generalized.

## Callable descriptor contract

A callable descriptor is the semantic source of truth for non-structural
operations:

```text
CallableId
Signature
Effects
Determinism
AlgebraicTraits
TrustLevel
Allowed/required attributes
Lowering options in future increments
```

Optimizers should ask what the descriptor permits. They should not primarily
match operation names like `core.add`.

Bad:

```text
if op == core.add then fold
```

Good:

```text
if descriptor is trusted + pure + deterministic + constant-evaluable then fold
```

## Trust and effects

Descriptor metadata is powerful enough to cause miscompilation if it is wrong.
Therefore algebraic traits are accepted only for trusted descriptor levels.

Conservative defaults:

- unknown callables fail verification;
- untrusted callables cannot expose algebraic traits;
- managed methods and constructors default to conservative external effects;
- `SsaManagedCallableAttribute` changes optimizer-visible semantics only when
  the descriptor is admitted through the normal trust checks;
- effectful calls must not be removed or reordered by pure passes;
- constant evaluation must be trusted, deterministic and bounded.

Example trusted managed callable:

```csharp
[SsaManagedCallable(
    IsPure = true,
    Determinism = Determinism.Deterministic,
    AlgebraicTraits = AlgebraicTraits.Commutative | AlgebraicTraits.Associative,
    TrustLevel = SemanticTrustLevel.VerifiedPlugin)]
private static int Add(int left, int right) => left + right;
```

The attribute does not make the method a special SSA opcode. It only feeds the
ordinary `CallableDescriptor` contract.

## Lowering rule

Intrinsics are lowering targets, not the semantic source of truth.

Example:

```text
SsaCall(std.i32.add.unchecked)
  -> AIR intrinsic
  -> CIL opcode
  -> managed call
  -> interpreter primitive
  -> reject with diagnostic
```

The current planner implements AIR intrinsic and managed-call emission for the
AIR route. CIL and interpreter targets are already represented as route targets,
but they remain explicit diagnostics until those routes own executable lowering
for them.

## Required next steps

1. Extend `AIR -> SSA` lowering for runtime patterns that still use
   execution-scoped provider descriptors and value types outside the current
   bool/int32/float64/object mapping.
2. Extend `SSA -> AIR` emission beyond the current AIR-compatible stack subset
   and add executable CIL/interpreter routes for their explicit target kinds.
3. Extend the constant model beyond preview int32/bool materialization so
   folded values are not tied to `SsaOperations.ConstantInt32` and
   `SsaOperations.ConstantBool`.
4. Add dialect syntax for intermediate-layer policies such as `off`, `prefer`,
   `require` and `debug`; keep `enable Ssa` only as a compatibility alias if
   useful.
5. Build interpreter/CIL parity tests before publishing performance claims.

## Non-goals for this increment

This increment does not claim:

- all Wist programs run through callable SSA;
- SSA-native backend support exists;
- SSA improves performance.

It establishes the architectural direction and the first typed contracts needed
to migrate safely.
