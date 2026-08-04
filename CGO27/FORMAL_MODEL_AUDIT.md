# Formal model to implementation audit

Status: implementation-backed audit after deferred-lifecycle repair.

## Central claim

Obligation-guided reverification turns declared compiler-state invalidations into named obligations with canonical owners and earliest executable boundaries. Under explicit trust assumptions, P2 either discharges each due obligation through its unique route or stops compilation before the artifact crosses that boundary. This is a selected-fact scheduling/enforcement guarantee, not a compiler-correctness proof.

## Typed correspondence

| Paper object | Implementation owner | Enforced property | Focused evidence |
|---|---|---|---|
| Fact set and three-valued state | `CompilerFactId`, `CompilerFactState` | facts are named; absent facts remain unknown | fact-state tests |
| Ordered boundaries | `CompilerPipelineStage` plus `PreparedExecutionBuilder` callbacks | bytecode, AIR, optimized AIR, backend input, then backend compilation | orchestration-order test |
| Effects | `PipelineEffectContract`, `PipelineEffectVerifier` | requirements/preservation checked; production restores; invalidation creates an obligation | pipeline-effect tests |
| Route map `(rule, owner, earliest)` | `CompilerFactVerifierRouteDescriptor`, `CompilerFactVerifierRegistry` | one normalized owner and earliest executable boundary per fact | registry and deferred-route tests |
| Pending lifecycle | `ConditionalWeakTable<CompilationInput, CompilationLifecycleState>` | facts and obligations survive observer returns within one compilation and are isolated by input identity | carry-forward, retry, and cleanup tests |
| Obligation `(fact, rule, owner, creation, first eligible)` | `VerificationObligation` | first eligibility is `max(creation, route.earliest)` | immediate and deferred obligation tests |
| P1/P1D | scheduler plus demanded-fact set | passive invalidation does not enforce a deadline; P1D executes only demanded invalid facts | non-enforcing lifecycle and demand-pair tests |
| P2/P3 | `ModuleContractVerificationScheduler` | P2 selects due obligations; P3 invokes every route exposed at the current modeled boundary; overdue/mismatched routes fail | scheduler and policy tests |
| Production discharge | `ModuleContractPipelineObserver.BeforeBackend` and production AIR verifier | a backend-input obligation created at optimized AIR is discharged immediately before backend compilation | `Selective_CarriesDeferredBackendObligationAcrossBoundaries` |

## Concrete deferred instance

The production path now contains a nontrivial lifecycle:

`OptimizedAir: invalidate backend.input-verified -> pending(firstEligible=BackendInput)`

`BackendInput: invoke core.backend-input -> valid/discharged -> backend compile`

The observer stores state per `CompilationInput`, requires strictly increasing callbacks, merges later stage seeds without overwriting invalidated facts, and removes state after the final boundary or any failed callback. P1/P1D may retain passive invalidation state without converting it into a mandatory boundary failure; only P2/P3 enforce deadlines.

## Relative guarantee assumptions

1. Initial fact seeds are semantically sound.
2. Effect declarations are truthful and invalidation-complete relative to the selected finite fact vocabulary.
3. Every created obligation has exactly one sound executable canonical route at its first eligible boundary.
4. The production observer is invoked at every modeled boundary in order and retains state across them.
5. Every due P2 obligation is scheduled before crossing its deadline.
6. Verifier rejection, unknown routing, owner conflict, or missed deadline stops compilation.

## Explicit non-claims

The mechanism does not prove seed/effect metadata, discover unmodeled relations, prove verifier implementations, establish whole-compiler correctness, show universal P2/P3 equivalence, or establish a performance advantage over MLIR-style verification after every pass.

## Closed review findings

- **Closed:** the previous production observer recreated state independently at each stage. State is now compilation-scoped and a real deferred route crosses observer callbacks.
- **Closed:** the proof previously treated declared production as semantic truth. Sound seeds and truthful/complete effects are now explicit assumptions.
- **Closed:** passive P1 obligations initially became routing failures after persistence was added. Enforcement diagnostics are now restricted to P2/P3; P1/P1D behavior is covered end-to-end.
- **Closed:** related work previously omitted verifier-after-every-pass. The paper now treats it as the strongest operational alternative and narrows novelty accordingly.
- **Still bounded:** external validity, historical policy replay, pinned-machine cost, and motivated deanonymization remain unresolved external conditions.
