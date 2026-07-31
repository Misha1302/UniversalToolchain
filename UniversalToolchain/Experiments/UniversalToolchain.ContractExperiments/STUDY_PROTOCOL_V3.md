# Contract-guided reverification study protocol v3

Status: pre-analysis implementation protocol for the CGO 2027 completion branch.
Baseline: `master` commit `c73b418c6e72e8b92371753a3a7b4a9f7adaa5f1`.
Historical protocol: `STUDY_PROTOCOL_V2.md` remains immutable evidence for the earlier B0/B1/B2 study and is not rewritten by this protocol.

## Research question

Can typed invalidation obligations preserve the fault-detection behavior of unconditional semantic reverification while executing fewer verifier rules on boundaries whose semantic facts remain valid?

The comparison is meaningful only after both correctness and cost are recorded from the same runner, the same cases, the same externally frozen oracle, and the same production verifier components.

## Policies

The runner exposes exactly four policies:

- `P0_STRUCTURAL`: runs existing structural IR/AIR and target-capability checks. It does not enable module ownership, Bytecode contract drift, or compiler-fact obligation routing.
- `P1_INVALIDATION`: adds typed module, ownership, Bytecode, and facts/effects validation. Invalidated facts are removed from the available fact state. Reverification requests are recorded but are not discharged automatically.
- `P2_SELECTIVE`: extends `P1_INVALIDATION`. Every invalidation mapped to a verifier rule creates an obligation. Only requested rules are executed, in deterministic rule-id order, before the boundary can be accepted. An obligation is failed when the routed production verifier rejects the mutated artifact or no executable route exists.
- `P3_ALWAYS`: extends `P1_INVALIDATION`. At each represented semantic boundary, every verifier applicable to that concrete boundary is executed even when no invalidation created an obligation. It uses the same artifacts and verifier implementations as `P2_SELECTIVE`.

`P2_SELECTIVE` and `P3_ALWAYS` must agree on outcome, diagnostic family, and first detection boundary for every frozen fault and valid-control case. A disagreement invalidates the run.

## Frozen corpora and denominators

This protocol reuses, without changing identifiers, expected diagnostic families, or denominators:

- 40 primary fault instances representing 32 operator shapes in five families;
- 10 post-freeze author-designed challenge operators;
- 100 valid controls per policy, stratified equally across five families;
- three deterministic repetitions per primary/challenge instance and policy.

The generated `mutations.csv` must be byte-identical to the v2 baseline catalog. New end-to-end, second-language, external, and ablation corpora are separate datasets and may not enter these denominators.

## Oracle

`oracles-v3.json` is a source-controlled artifact separate from the active runner. For each frozen fault, it contains:

- expected rejection;
- expected diagnostic family;
- expected first eligible boundary;
- operator identity, corpus, and family.

For controls, the oracle is acceptance with no diagnostic family and a fixed set of five strata. Runtime observations do not overwrite the oracle. `validate_oracles.py` checks raw expected fields, fault identifiers, the historical mutation-catalog SHA-256, triplet stability, and P2/P3 outcome–diagnostic–boundary parity before statistical analysis. A deliberately corrupted oracle/raw pair must be rejected.

## Reverification execution

For invalidated core facts, the canonical registry maps facts to production verifier rule IDs. The runner invokes the corresponding production verifier against a case-specific artifact:

- `core.verifier.bytecode-contract` uses `BytecodeVerifier`;
- `core.verifier.air-contract` uses `AirVerifier`.

`P2_SELECTIVE` invokes only rules named by `ReverificationRequest`. `P3_ALWAYS` invokes the verifier applicable to the represented Bytecode or AIR boundary even when the request set is empty. The current frozen corpus does not represent a backend-input boundary; therefore `core.verifier.backend-input-contract` is not counted as applicable in this runner.

## Raw evidence

The primary output is JSONL conforming to `raw-result-schema-v3.json`. Each line contains the policy, case and oracle identity, actual classification, per-rule invocation counts, verification/pipeline timing, allocation and peak working-set observations, obligation/fact counters, repetition, seed, and process status.

The boundary experiment deliberately writes `whole_compilation_elapsed_ns: null`. Whole-compilation latency belongs only to the separate end-to-end harness and may not be inferred from the boundary-kernel timing.

Invalid JSON, a missing required field, mixed commits/run IDs, unknown policies, inconsistent invocation totals, negative counters, unstable repetitions, changed denominators, a changed oracle field or catalog hash, a rejected valid control, `P2/P3` outcome–diagnostic–boundary disagreement, or a missing `P3` extra invocation on clean fact boundaries invalidates the run.

## Timing scope

The built-in timing loop is an isolated verifier-kernel diagnostic. It uses 33 counterbalanced samples and does not establish whole-compilation overhead. It exists to detect gross policy-cost regressions and to verify that `P3_ALWAYS` actually performs additional work. Decision-grade performance claims require the separately pinned benchmark protocol, raw distributions, warm-up rules, bootstrap confidence intervals, and a fixed hardware/software identity.

## Interpretation boundaries

- Primary and challenge faults are author-designed; neither is an independently authored blind corpus.
- The current runner exercises production verifier components at compiler boundaries, not every Wist source-to-result path.
- Equality of `P2_SELECTIVE` and `P3_ALWAYS` on this fixed corpus is necessary but not sufficient for a general equivalence claim.
- No submission claim may call this timing whole-compilation overhead.
- Historical v2 results remain historical and are not recomputed under renamed policies.
