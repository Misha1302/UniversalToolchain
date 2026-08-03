# Formal model to implementation audit

Status: first implementation-backed audit for CGO 2027 hardening.

## Central claim

Obligation-guided reverification turns selected compiler-state changes into named, boundary-indexed verification obligations with canonical owners. Under the stated relative assumptions, an obligation-enforcing scheduler either discharges every due obligation through its unique route or stops compilation before the artifact crosses the first eligible boundary.

This is a relative, selected-fact guarantee. It is not a correctness proof for the compiler.

## Typed correspondence

| Paper object | Implementation owner | Enforced property | Focused evidence |
|---|---|---|---|
| Fact set `F` and IDs | `CompilerFactId`, selected contract table | facts are typed/named rather than free-form telemetry labels | module-contract builder tests |
| `valid`, `invalid`, `unknown` | `CompilerFactValidity`, `CompilerFactState.GetValidity` | one fact cannot be simultaneously valid and invalid; absent facts are unknown | `CompilerFactStateTests` |
| Ordered boundaries `B` | `CompilerPipelineStage` | bytecode, AIR, optimized AIR, backend input have deterministic order | scheduler boundary tests |
| Effect contract `(requires, produces, preserves, invalidates)` | `PipelineEffectContract` | normalized finite sets and explicit stage | pipeline-effect tests |
| Effect transition | `PipelineEffectVerifier` | missing requirements and invalid preservation fail; invalidation changes state; production restores validity | `PipelineEffectVerifierTests` |
| Canonical route map | `CompilerFactVerifierRegistry` | one route descriptor `(rule, canonical owner)` per fact; conflicts or empty owner fail | registry/scheduler tests |
| Obligation `(fact, rule, owner, creation, first eligible)` | `VerificationObligation` | each invalidation with a route yields a named boundary-indexed obligation | `Validate_WhenVerifiableFactIsInvalidated_CreatesReverificationRequest` |
| Missing route fail-closed | `PipelineEffectVerifier`, `UT-PIPELINE-EFFECT-006` | invalidation without an executable canonical route is an error, not a silent omission | `Validate_WhenInvalidatedFactHasNoVerifierRoute_FailsClosedWithDiagnostic` |
| Demand recomputation | `P1DemandRecomputation`, `DemandedFacts` | only an explicit query schedules an invalidated fact; unknown query fails closed | P1D scheduler/pipeline tests |
| Due-obligation scheduling | `ModuleContractVerificationScheduler` | P2 selects obligations whose first eligible boundary is current; overdue obligation fails | selective deadline tests |
| Always verification | `P3Always` | all exposed routes run; due obligations retain owner/fact lineage | P3 deterministic-order tests |
| Discharge/rejection | `ModuleContractPipelineObserver` + production verifier | a successful route invocation discharges the obligation for that crossing; a diagnostic stops compilation | Wist boundary/E2E studies |

## Conservative implementation instance

The general model permits `creation_boundary <= first_eligible_boundary`. The current production integration chooses the conservative instance

`first_eligible_boundary = creation_boundary`

for obligations created by observed bytecode/AIR effects, because the canonical production verifier is executable at that boundary. The scheduler nevertheless rejects an obligation presented after its deadline, and tests exercise that fail-closed branch. The paper must not claim that the current implementation defers obligations across multiple boundaries.

## Relative guarantee assumptions

The guarantee depends on all of the following:

1. the modeled fact set contains the semantic relation at issue;
2. every transformation effect contract is complete relative to that fact set;
3. every invalidated fact has exactly one executable canonical route at its first eligible boundary;
4. the scheduler is invoked at every modeled boundary and executes every due obligation before crossing;
5. the verifier route is sound for the fact it owns;
6. successful verifier completion is treated as discharge and verifier rejection stops compilation.

## Explicit non-claims

The theorem does not establish:

- completeness of the fact vocabulary;
- completeness or correctness of declared effects;
- correctness of verifier implementations;
- correctness of transformations outside the modeled relations;
- whole-compiler semantic correctness;
- universal P2/P3 equivalence;
- whole-compilation performance improvement.

## Adversarial consistency findings

- **Repaired:** a fact invalidation with no registry route previously produced no obligation. This contradicted fail-closed missing-route language. `UT-PIPELINE-EFFECT-006` now makes the transition an error.
- **Bounded:** the implementation does not persist deferred obligations across observer boundaries. The paper therefore identifies the implementation as the immediate-deadline instance of the more general model.
- **Bounded:** P3 is an empirical comparison policy over registered routes, not a proof oracle for unmodeled facts.
- **Bounded:** P1D protects against stale cached results only when a consumer explicitly queries the fact. It intentionally has no semantic-boundary deadline.
