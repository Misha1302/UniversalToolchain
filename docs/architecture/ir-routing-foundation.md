# IR routing foundation

Status: current foundation with a minimal SSA model, structural verifier,
AIR/SSA conversion boundary, first verifier-gated optimization boundary and
opt-in dialect optimizer wiring for the alpha SSA route.

UniversalToolchain now exposes minimal generic IR routing contracts in
`UniversalToolchain.Ir.Abstractions`.

The contracts intentionally describe:

- IR artifact kind identity;
- pipeline stages;
- converters;
- optimization passes;
- verifiers;
- stage fact and capability effects;
- optional intermediate layer policy.

The contracts intentionally do not reference:

- Wist;
- SSA;
- concrete backends;
- concrete modules;
- product profiles.

SSA-specific code lives outside the generic IR layer:

- `UniversalToolchain.Air.Analysis` defines deterministic AIR CFG construction,
  AIR value-stack analysis and structural AIR verification;
- `UniversalToolchain.Ssa.Abstractions` defines the immutable SSA model,
  identifiers, operation descriptors, attribute bags, terminators and
  `SsaArtifact`;
- `UniversalToolchain.Ssa.Core` defines `StructuralSsaVerifier`, which is an
  `IIrVerifier` implementation for `SsaIrKinds.Ssa`.
- `UniversalToolchain.Ssa.Lowering` defines the first minimal `AIR -> SSA`
  converter over the verified AIR subset.
- `UniversalToolchain.Ssa.Emission` defines the first minimal `SSA -> AIR`
  converter over the SSA subset that can be represented by the current generic
  AIR stack model.
- `UniversalToolchain.Ssa.Optimization` defines a verifier-gated SSA optimizer
  pipeline plus the first local constant folding pass over pure core int32
  operations.
- `UniversalToolchain.Ssa.Optimization` also exposes `SsaOptimizerModule`
  as a manifest-driven optimizer alias `Ssa`. The adapter runs the current
  verifier-gated `AIR -> SSA -> SSA optimization -> AIR` route when a dialect
  explicitly enables it.

## Current runtime behavior

The default supported runtime path remains AIR-only:

```text
Source/Text -> Lexer/Parser -> AST -> Bytecode -> AIR -> AIR optimizers -> Backend -> Execution
```

`IntermediateRepresentationAbstractions` wraps the existing AIR model as
`AirArtifact` so future pipeline planners can treat AIR as a normal IR artifact
without replacing the current semantic boundary.

`BasicCore` uses an internal AIR-only executor that invokes the canonical
`IAirOptimizer` contract through the generic stage pipeline. This is the normal
AIR runtime route, not a second compatibility runtime.

SSA execution is available only as an explicit alpha optimizer selected
through the normal dialect optimizer directive path:

```text
enable Ssa
```

This route remains fail-fast. Unsupported AIR intrinsics, managed calls,
unsupported stack reshaping and unsupported value types are diagnosed before
backend execution rather than silently falling back to AIR-only execution.

## Implemented SSA surface

The first SSA slice implements:

- `SsaModule`, `SsaFunction`, `SsaBlock`, `SsaBlockParameter`, `SsaValue`,
  `SsaOperation`, `SsaTerminator` and `SsaArtifact`;
- block arguments instead of phi instructions;
- descriptor-driven operations through `SsaOpDescriptor` and
  `SsaDescriptorSet`;
- deterministic ids, attributes and descriptor ordering;
- structural verification for duplicate functions, blocks, operations and
  values;
- terminator shape and target validation;
- block argument count/type validation;
- return arity/type validation against the function return type;
- descriptor operand/result type and attribute validation;
- basic dominance/use-before-definition validation for reachable blocks.

The default core descriptor snapshot currently covers neutral int32 arithmetic
and comparison descriptors only as alpha compatibility for the first
AIR/SSA/AIR route. The long-term direction is callable-first SSA: arithmetic,
runtime calls, constructors and intrinsics should be modeled as semantic
callable descriptors outside the structural SSA core. Extensions must provide
their own descriptor sets instead of relying on operation-name string matching
inside verifiers or optimizers.

## Implemented AIR analysis surface

`UniversalToolchain.Air.Analysis` implements the first shared AIR analysis
boundary required before full stack-to-SSA lowering:

- deterministic block leader discovery;
- label, jump, conditional jump and fallthrough CFG edges;
- predecessor/successor materialization;
- duplicate-label and unknown-target diagnostics;
- typed stack analysis for the currently supported generic subset:
  `Push bool`, `Push int`, `Drop`, `Jmp`, `JmpIf`, `JmpIfNot`, `Label`,
  `Nop` and `Annotate`;
- stack underflow, non-bool condition and incompatible merge diagnostics.

Intrinsics are intentionally not accepted by the generic stack analysis yet.
They require descriptor/effect support before they can be lowered safely.

## Implemented AIR to SSA subset

`UniversalToolchain.Ssa.Lowering` adds `AirToSsaConverter` as a normal
`IIrConverter`:

```text
InputKind:  air
OutputKind: ssa
```

The current subset supports:

- constants from `Push bool`, `Push int` and `Push double`;
- `Drop`;
- labels;
- unconditional jumps;
- conditional branches with explicit true/false successors;
- block arguments for stack values at merge blocks;
- zero-value and single-value function returns.

The converter rejects unsupported intrinsics, unsupported constants, invalid
stack states and multi-value returns with structured diagnostics. It verifies
AIR before lowering and verifies the produced SSA before returning.

## Implemented SSA to AIR subset

`UniversalToolchain.Ssa.Emission` adds `SsaToAirConverter` as a normal
`IIrConverter`:

```text
InputKind:  ssa
OutputKind: air
```

The current subset supports SSA that can be expressed by the existing generic
AIR stack operations:

- one function with no function parameters;
- `core.const.i32`, `core.const.bool` and `core.const.f64`;
- jumps whose transfer arguments already match the current stack;
- conditional branches whose true and false transfers keep the same stack
  arguments, with one target represented by AIR fallthrough;
- CFG-based block layout for simple branch/jump shapes instead of relying on
  the source `SsaFunction.Blocks` order;
- block parameters as incoming AIR stack values at labels;
- a single final return block represented by the end of the AIR instruction
  stream.

The converter rejects arithmetic operations, branch argument reshaping,
unsupported terminators, non-final returns, invalid input SSA and invalid output
AIR with structured diagnostics. It verifies SSA before emission and verifies
the produced AIR before returning.

## Implemented SSA optimization subset

`UniversalToolchain.Ssa.Optimization` adds the first optimizer boundary without
wiring it into the runtime path:

- `SsaOptimizerPipeline` runs ordinary `IIrOptimizationPass` implementations
  over `SsaArtifact`;
- pipeline input and every pass output are checked with `StructuralSsaVerifier`;
- pass legality is guarded by declared facts and capabilities;
- declared fact effects are applied at the pipeline boundary: produced facts are
  added, preserved input facts survive and invalidated facts are removed;
- `SsaConstantFoldingPass` folds local trusted pure callables when all operands
  are already known constants in the same block;
- folded instructions preserve the original result SSA value ids, so downstream
  terminators and block transfers do not need a separate rename map.

The initial pass covers alpha int32 arithmetic callables lowered from AIR
intrinsics. It does not perform cross-block propagation, dead-code elimination,
algebraic simplification, effectful folding or runtime/backend selection.

## Non-goals in this step

This original foundation step did not implement:

- full AIR to SSA conversion for all intrinsics, locals, loops with effectful
  operations, or all runtime-supported value types;
- complete SSA to AIR lowering for arbitrary arithmetic operations, intrinsics,
  stack rearrangement and multi-return shapes;
- a full SSA optimizer suite beyond local constant folding;
- SSA-native backend support;
- dialect syntax for intermediate-layer policies.

Later callable-first work added a managed `MethodInfo` / `ConstructorInfo`
bridge for the supported bool/int32/float64/object subset. Execution-scoped
provider descriptors, unresolved generic methods and wider CLR value-type
mapping remain outside the backend-neutral SSA subset.

Those features must be added as separate architecture tasks with verifier and
semantic parity coverage.

## Required invariants

- AIR remains the current stable semantic boundary.
- Existing interpreter and CIL behavior must not change because of this
  foundation layer.
- New IR layers must register through generic contracts instead of adding
  hardcoded branches to framework/runtime layers.
- Optimizers that change IR semantics must declare fact and capability effects
  and be verifier-compatible.

## Build note

All projects currently target `net10.0`. The canonical entrypoints build
`Wist.sln` and `PlanFuzz.sln` sequentially because the solutions share projects
and output directories, but MSBuild traverses each solution graph in parallel.
`--jobs N` / `-Jobs N` caps the node count; `--serial --no-build-servers` /
`-Serial -NoBuildServers` preserves an isolated diagnostic path.

This build policy does not change runtime references or IR routing behavior. New
SSA/AIR projects should still declare every project whose public types they
consume directly; projects with existing transitive-reference behavior may keep
it temporarily until their direct public-type dependencies are declared in a
separate cleanup.
