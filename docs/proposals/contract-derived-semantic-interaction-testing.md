---
status: proposal
last_verified: 2026-07-20
current_truth: ../CURRENT_ARCHITECTURE_STATUS.md
related_design: typed-module-contracts-and-verifiers.md
---

# Contract-Derived Semantic Interaction Testing

Status: research proposal, not an implementation or novelty claim.

Owner area: UniversalToolchain module contracts, optimizer validation, and cross-backend semantic parity.

## Summary

UniversalToolchain already records typed information about selected language modules, AST ownership, Bytecode emissions, AIR emissions, required backend capabilities, verifier contributions, and actual SSA route execution. Today these contracts primarily validate one selected composition.

This proposal explores a second use for the same metadata:

> Derive semantic test obligations from compiler contracts, select small witness programs that exercise those obligations, execute them across backend and optimizer routes, and localize semantic divergences to the smallest interaction boundary.

The intended pipeline is:

```text
module / IR / optimizer / backend contracts
  -> semantic interaction graph
  -> derived test obligations
  -> minimal dialect and execution configurations
  -> typed witness selection
  -> differential execution
  -> observation comparison
  -> failure minimization and localization
```

The candidate research contribution is not module composition, IR verification, capability-gated lowering, fuzzing, or differential testing by themselves. It is the mechanism that turns architectural contracts into targeted semantic experiments.

## Problem

The difficult defects in a modular compiler often live at intersections rather than inside one component:

```text
language module
x IR representation
x optimization pass
x backend capability
x runtime route
```

A module, optimizer, and backend can each pass focused tests while their composition still changes observable semantics. Examples include:

- local and external storage identities becoming conflated after lowering;
- an effectful call being removed because an optimizer consumed incorrect purity metadata;
- branch folding changing which effects remain reachable;
- a backend-specific intrinsic reaching an unsupported route;
- an SSA request silently falling back, causing an experiment to test a different route than intended;
- interpreter and CIL diagnostics diverging for the same accepted language surface.

Exhaustively testing every module subset, backend, SSA policy, and optimizer profile is impractical. Random or grammar-based fuzzing can generate many valid programs without reaching the rare semantic interaction that matters. A verifier can reject one invalid artifact, but it does not tell us which valid artifacts and configurations must be constructed to test agreement between components.

## Existing repository foundation

The proposal builds on current repository mechanisms rather than inventing a parallel description language.

Relevant current contracts include:

- `IModuleContractFacet`: typed module identity, facet kind, and schema version;
- `IBytecodeContractFacet`: possible Bytecode emissions;
- `IAirContractFacet`: AIR patterns, intrinsics, and required capabilities;
- `IBackendCapabilityFacet`: backend capabilities and supported intrinsics;
- `IVerifierContractFacet`: verifier rule contributions;
- `WistOptimizationReport`: evidence of actual SSA route use, fallback, and executed passes.

`VariablesModuleContractDescriptorProvider` is the first useful vertical slice. One variable-related AST surface can produce both `LocalRead` and `ExternalRead`. Those patterns require different storage capabilities while participating in the same language feature and execution pipeline.

A minimal witness is:

```wist
let local = 10
local + external
```

with `external = 100`.

The same witness can be executed through:

```text
Interpreter / SSA disabled
CIL         / SSA disabled
Interpreter / SSA required
CIL         / SSA required
```

The expected result is equivalent across all routes. Route evidence must prove that SSA-required variants actually executed the SSA route; fallback is an invalid experiment, not a passing parity result.

## Core concepts

### Semantic interaction graph

The graph normalizes existing contract facets into typed nodes and edges.

Candidate node kinds:

- module;
- AST kind;
- Bytecode pattern;
- AIR pattern;
- intrinsic;
- backend capability;
- optimizer pass;
- backend or execution route;
- semantic property or observable.

Candidate edge kinds:

- `owns`;
- `emits`;
- `lowers-to`;
- `requires`;
- `provides`;
- `consumes`;
- `transforms`;
- `verifies`.

The generic graph layer must use typed contract IDs. It must not infer semantics from CLR class names, raw source parsing, or Wist-specific string matching.

### Semantic test obligation

An obligation is not a source program. It describes what interaction must become observable.

```csharp
public sealed record SemanticTestObligation(
    TestObligationId Id,
    string Reason,
    IReadOnlySet<ModuleId> RequiredModules,
    IReadOnlySet<SemanticRequirementId> Requirements,
    IReadOnlyList<ExecutionVariant> Variants,
    IReadOnlySet<ObservablePropertyId> Observables,
    ObligationProvenance Provenance);
```

Example:

```text
id:
  variables.local-external.backend-parity

reason:
  LocalRead and ExternalRead originate from one selected feature surface
  but require different storage capabilities.

requirements:
  local read
  external read
  both values meet in one observable expression

variants:
  Interpreter / SSA off
  CIL / SSA off
  Interpreter / SSA require
  CIL / SSA require

observables:
  status
  result
  stable diagnostic category
  side-effect trace
  actual optimization route
```

### Witness

The first implementation should not attempt unrestricted program synthesis. It should use a typed registry of small parameterized witnesses.

```csharp
public interface ISemanticWitnessProvider
{
    bool CanSatisfy(SemanticTestObligation obligation);

    WitnessProgram Create(
        SemanticTestObligation obligation,
        WitnessGenerationContext context);
}
```

Initial witness families can include:

- local plus external read;
- lexical shadowing;
- branch merge;
- pure and effectful calls;
- constant and non-constant operands;
- constructor or managed-call preservation;
- unsupported intrinsic rejection.

The core selects witnesses by semantic requirements. It must not contain a branch for a concrete Wist module name or obligation ID.

### Execution observation

The oracle must compare more than the final value.

```csharp
public sealed record ExecutionObservation(
    ExecutionVariant Variant,
    ExecutionStatus Status,
    object? Result,
    IReadOnlyList<DiagnosticSnapshot> Diagnostics,
    IReadOnlyList<SideEffectEvent> SideEffects,
    OptimizationRouteSnapshot Optimization,
    ExceptionSnapshot? Exception);
```

The minimum comparison surface is:

- success or failure status;
- result value;
- stable diagnostic code or category;
- ordered side-effect trace;
- actual route and executed passes.

Correctness validation and performance validation remain separate.

## Obligation derivation rules

The first prototype should use a small explicit set of deterministic rules.

### Representation split

Trigger:

```text
one semantic or AST owner
  -> emits multiple lower-level patterns
  -> patterns require materially different capabilities or identities
```

Result: derive an obligation requiring both patterns to occur in one observable program and compare every supported route.

The first instance is `LocalRead` versus `ExternalRead`.

### Multiple supported backends

Trigger:

```text
one semantic pattern
  -> supported by more than one backend route
```

Result: derive a cross-backend parity obligation.

### Optimizer transformation

Trigger:

```text
pass consumes pattern P
  -> may produce Q or remove P
```

Result: compare optimizer-disabled and optimizer-enabled routes while preserving declared observables.

### Effects under transformation

Trigger:

```text
pattern reads or writes state, may throw, or records an external effect
  + pass may remove, duplicate, or reorder instructions
```

Result: require an effect trace and an observation that remains externally visible.

### Alternative lowering routes

Trigger:

```text
one semantic operation
  -> portable call lowering
  -> specialized intrinsic lowering
```

Result: compare portable and specialized routes, including unsupported-target diagnostics.

Each derivation rule must preserve provenance: the exact facets, patterns, capabilities, and pass contracts that caused the obligation.

## Optimizer test contracts

Current module contracts do not completely describe optimizer interaction. A test-oriented optimizer contract is therefore required.

```csharp
public sealed record OptimizationPassContract(
    OptimizationPassId PassId,
    IReadOnlySet<AirPatternId> ConsumesPatterns,
    IReadOnlySet<AirPatternId> MayProducePatterns,
    IReadOnlySet<SemanticPropertyId> Preconditions,
    IReadOnlySet<ObservablePropertyId> MustPreserve);
```

Examples:

| Pass | Consumes | Preconditions | Must preserve |
|---|---|---|---|
| Constant folding | pure call and constants | pure, deterministic, trusted, evaluable | result, diagnostics, effects |
| Dead pure instruction elimination | unused instruction | pure | result, effects, termination |
| Branch folding | constant condition and branch | known condition | selected-branch semantics, reachable effects |
| SCCP-lite | values and control flow | supported type/lattice | result, reachable effects, diagnostics |

The contract describes testing obligations. It does not replace legality checks inside the optimizer.

## Proposed project boundaries

A first implementation can use separate projects:

```text
UniversalToolchain.SemanticTesting.Abstractions
UniversalToolchain.SemanticTesting.Core
UniversalToolchain.SemanticTesting.Wist
UniversalToolchain.SemanticTesting.Tests
```

Responsibilities:

- `Abstractions`: graph, obligations, witnesses, variants, observations, reports;
- `Core`: graph construction, derivation rules, planning, comparison, minimization;
- `Wist`: Wist witness providers, dialect construction, runtime execution adapter;
- `Tests`: focused tests and mutation evaluation harness.

Architectural constraints:

- UniversalToolchain is the generic framework; Wist remains the reference language;
- generic semantic-testing code must not depend on Wist syntax or concrete backend classes;
- the interpreter remains a semantic reference route, not a high-performance intrinsic backend;
- backend-specific forms remain capability-gated;
- test infrastructure must not alter production execution semantics;
- contracts are hypothesis sources and planning inputs, not proofs of correctness.

## MVP vertical slice

The first cycle should stop after one real obligation works end to end.

### Input evidence

Read the current `VariablesModuleContractDescriptorProvider` and detect:

```text
VariableNode
  -> LocalRead
  -> ExternalRead
  -> different capability sets
```

### Derived obligation

Produce `variables.local-external.backend-parity` with requirements for one local read, one external read, and a shared observable expression.

### Selected witness

```wist
let local = 10
local + external
```

with `external = 100`.

### Execution matrix

```text
Interpreter / SSA disabled
CIL         / SSA disabled
Interpreter / SSA required
CIL         / SSA required
```

### Required experiment checks

- all four configurations build and execute or return comparable structured diagnostics;
- SSA-required variants prove actual SSA use;
- result, status, diagnostics, and effect trace are compared;
- unsupported or fallback routes are reported as invalid experiments;
- the report contains obligation provenance and minimal failing dimensions.

### First controlled defect

Introduce one isolated mutation in a test-only branch or mutation harness that conflates local and external storage identity in one route. The system should:

1. detect the divergence;
2. report the failing backend and SSA dimension;
3. retain the original obligation provenance;
4. reduce optional witness parameters and execution dimensions;
5. identify the suspected boundary as variable storage identity during lowering or emission.

## Configuration planning

The MVP may use one minimal dialect per obligation. After multiple obligations exist, configuration selection becomes a constrained set-cover problem:

- generate only valid module sets;
- satisfy dependencies and forbidden combinations;
- prefer configurations covering several obligations;
- preserve a minimal configuration when extra modules may change semantics;
- record which obligation each configuration covers.

The planner must not claim exhaustive coverage. Its report should distinguish modeled, covered, unsupported, and unknown interactions.

## Failure minimization

The initial reducer should operate on typed witness parameters and execution dimensions rather than arbitrary source text.

Reduction dimensions can include:

- remove unused declaration;
- remove branch;
- remove shadowing;
- reduce nesting depth;
- remove constant subexpression;
- remove optional module;
- disable one optimizer pass;
- remove one execution variant while retaining the divergence.

A later implementation may add AST-aware reduction.

## Evaluation plan

The research value must be demonstrated against baselines, not asserted from architecture.

### Research questions

- **RQ1:** Do contract-derived obligations detect interaction defects missed by the existing focused test suite?
- **RQ2:** Do they detect such defects with fewer executions than random or grammar-based fuzzing?
- **RQ3:** Does semantic interaction planning outperform module-only pairwise coverage?
- **RQ4:** Can provenance and reduction localize the responsible module/pass/backend boundary?
- **RQ5:** What metadata and witness-authoring cost is required for a new extension?

### Baselines

- current manually authored tests;
- grammar-based or random valid-program generation;
- feature/module pairwise testing;
- contract-derived testing without semantic derivation rules;
- full method with provenance, route evidence, and minimization.

### Mutation operators

Candidate seeded defects include:

- conflate local and external storage identity;
- remove an effectful call as though it were pure;
- reorder two effectful calls;
- break branch folding;
- allow an unsupported intrinsic;
- change overflow behavior;
- lose a constructor or managed call;
- violate stack effect;
- select the wrong lowering target;
- break lexical shadowing;
- accept fallback under `Require`;
- accept conflicting descriptor snapshots.

### Metrics

- mutation detection rate;
- executions and wall-clock time to first detection;
- invalid-experiment rate;
- localization accuracy;
- minimal witness size;
- contract and witness authoring cost;
- false-positive rate.

A reproducibility package should pin the commit, SDK, environment, mutations, raw reports, baselines, and rerun command.

## Novelty boundary

Safe claim before implementation and literature review:

> UniversalToolchain is exploring contract-derived semantic interaction testing, where module, IR, optimizer, and backend contracts are used to derive targeted differential test obligations and execution plans.

A stronger claim is allowed only if experiments support it:

> The proposed derivation method detects seeded interaction defects more effectively than selected baselines with fewer executions and useful boundary localization.

Do not claim without additional evidence:

- the first such method in the world;
- semantic equivalence of all backends;
- exhaustive automatic test generation;
- correctness proved by contracts;
- universality across arbitrary languages and compilers;
- that Wist currently implements this proposal.

A publication-grade novelty claim requires a focused review of software product-line interaction testing, constrained covering arrays, modular language composition, semantic and grammar-based fuzzing, compiler differential testing, translation validation, contract-based test generation, extensible IR verification, and metamorphic optimizer testing.

## Risks

| Risk | Consequence | Mitigation |
|---|---|---|
| Incomplete contracts | important interactions remain invisible | report unknown and unmodeled boundaries; never treat contracts as exhaustive |
| Incorrect contracts | false obligations or shared undetected bugs | controlled mutations, independent verifiers, expected witnesses, metamorphic checks |
| Witness bias | only familiar shapes are covered | several witness variants per requirement and later AST combinators |
| Weak oracle | interpreter and CIL may share the same defect | expected values, properties, verifier invariants, and mutation ground truth |
| Silent fallback | the intended optimizer route is not exercised | `Require` plus route evidence; classify fallback as invalid experiment |
| Combinatorial growth | excessive obligation and route count | constraints, bounded interaction strength, weighting, and set cover |
| Framework contamination | research concerns leak into production core | separate projects and adapter boundaries |
| Publication overclaim | known techniques are presented as a new result | precise delta, baselines, ablations, literature review, and explicit limitations |

## MVP acceptance criteria

The MVP is complete only when:

1. an interaction graph is built from current typed contracts without raw string matching in generic core;
2. one obligation is automatically derived from a real `VariablesModule` descriptor;
3. a witness is selected by semantic requirements rather than a concrete obligation name;
4. the runner executes Interpreter/CIL with SSA disabled and required;
5. SSA-required variants prove actual route use;
6. status, result, diagnostics, and side effects are compared;
7. invalid experiments are distinct from semantic divergences;
8. the report contains derivation provenance and minimal failing dimensions;
9. focused tests cover derivation, witness selection, route validation, and comparison;
10. a controlled mutation is detected without changing supported user semantics.

The MVP is not complete if the generic core hardcodes `VariablesModule`, an SSA fallback is counted as parity coverage, only final values are compared, the witness does not prove the required patterns occurred, or the obligation is manually inserted instead of derived.

## Recommended first-cycle stop rule

Stop expansion when one obligation has been derived from a real contract, one witness has been selected through the registry, four execution routes have been checked with route evidence, and one controlled defect has been detected and localized.

Do not build a general program synthesizer, global optimizer-contract system, or exhaustive configuration planner before that vertical slice works.

## Next actions

1. Inventory the current contract facets and stable IDs needed by the graph.
2. Add semantic-testing abstractions and a minimal graph builder.
3. Implement the representation-split derivation rule.
4. Add the local/external arithmetic witness.
5. Add the Wist execution adapter and four-route runner.
6. Add route validation and structured observation comparison.
7. Emit a machine-readable report with obligation provenance.
8. Add one controlled mutation and verify end-to-end detection.
9. Perform the focused literature review before strengthening the novelty claim.
10. Extend optimizer contracts and witness coverage only after the first slice is reproducible.

## Evidence status

`PARTIAL`.

The repository provides enough current architecture to justify feasibility and a concrete first slice. The proposed semantic-testing subsystem, mutation evaluation, baseline comparison, and publication-grade novelty review have not yet been implemented.