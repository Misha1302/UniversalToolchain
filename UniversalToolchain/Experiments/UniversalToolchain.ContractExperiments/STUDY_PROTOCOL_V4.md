# Contract-guided reverification study protocol v4

Status: active protocol for the CGO 2027 submission-hardening branch.
Historical protocols: `STUDY_PROTOCOL_V2.md` and `STUDY_PROTOCOL_V3.md` remain immutable evidence and are not rewritten by this protocol.

## Research question

When an optimization invalidates a semantic fact, how does obligation-guided reverification compare with structural checks, invalidation tracking, demand-driven recomputation, and unconditional reverification on the same artifacts and verifier implementations?

## Policies

- `P0_STRUCTURAL`: structural and capability checks only.
- `P1_INVALIDATION`: tracks typed fact invalidation and obligations, but does not schedule semantic recomputation.
- `P1D_DEMAND_RECOMPUTATION`: schedules a canonical verifier only when a downstream query explicitly demands an invalid fact.
- `P2_SELECTIVE`: discharges every obligation at its first eligible boundary, whether or not a later query occurs.
- `P3_ALWAYS`: runs every canonical verifier applicable to the represented boundary.

P2 and P3 use the same canonical owner, artifact and verifier implementation. Their outcome, diagnostic family and first detection boundary must agree on all frozen fault cases. P1D is evaluated on a matched queried/unqueried pair and is not relabelled as P1 or P2.

## Frozen historical corpus

The v3 historical denominator remains byte-identical:

- 40 primary fault instances representing 32 operator shapes;
- 10 post-freeze challenge operators;
- 100 valid controls per historical policy across five historical control families;
- three deterministic repetitions per fault instance and historical policy.

`mutations.csv` contains only the historical primary/challenge catalog in the original five-column format. Its required SHA-256 is `e830125293770b512e540a4ae3a003c407258916aea2d7f65d95b08cdadbb183`. `oracles-v3.json` and `validate_oracles.py` validate only P0, P1, P2 and P3 historical rows and historical control strata. P1D observations, the demand pair and `clean-demand` controls do not enter the historical denominator.

## Demand-driven baseline corpus

The separate `demand-v4` dataset contains two otherwise matched mutations:

- `DEMAND-01`: the invalidated fact is queried downstream;
- `DEMAND-02`: the same fact is not queried downstream.

`demand-mutations-v4.csv` has SHA-256 `59bf35a7f1e974b62fea3863ebb041966bbbcc14635c7911de4b08eff724262a`. `demand-oracles-v4.json` freezes the expected query bit and per-policy outcome/diagnostic/boundary triple. `validate_demand_oracles.py` also requires 20 accepted `clean-demand` controls per policy. This dataset is reported separately from the historical corpus.

## Raw evidence

Active runs emit JSONL conforming to `raw-result-schema-v4.json`. Version 4 adds `P1D_DEMAND_RECOMPUTATION`, `demand-v4`, and the required Boolean `demand_query` field. The historical `raw-result-schema-v3.json` remains unchanged.

Every active run must pass, in order:

1. internal raw-record and policy invariants;
2. historical oracle/catalog validation;
3. demand oracle/catalog validation;
4. statistical analysis;
5. recursive SHA-256 manifest verification.

A changed historical catalog, changed demand catalog, mixed commit/run identity, unknown policy, unstable triplet, P2/P3 disagreement, incorrect P1D queried/unqueried behavior, rejected valid control or false positive invalidates the run.

## Timing and claim boundary

Boundary-kernel timing is diagnostic only. `whole_compilation_elapsed_ns` remains null. No whole-compilation or application speed claim is permitted without the separately pinned performance protocol and raw distributions.

The primary/challenge and demand cases are author-designed. Historical bug replay and any external corpus are separate datasets and must retain distinct provenance labels.
