# UniversalToolchain contract-guided reverification experiment

This non-packable project evaluates four verification policies over production UniversalToolchain contract-table, ownership, Bytecode, AIR, facts/effects, capability, and reverification components. It does not replace those components with a synthetic detector.

The experiment is research infrastructure, not part of the public Wist package surface. The canonical runner restores and builds it against the exact current production API before collecting results.

## Compared policies

- **`P0_STRUCTURAL`** — structural AIR and target-capability checks; no semantic obligation routing.
- **`P1_INVALIDATION`** — typed contracts and facts/effects; invalidations are tracked, but requested semantic reverification is not automatic.
- **`P2_SELECTIVE`** — `P1` plus deterministic discharge of only the verifier rules requested by invalidated facts.
- **`P3_ALWAYS`** — `P1` plus unconditional execution of every verifier applicable to the represented semantic boundary.

`P2_SELECTIVE` and `P3_ALWAYS` share the same runner, cases, artifacts, oracles, and production verifier implementations. The runner rejects any correctness disagreement between them.

## Evidence sets

The historical corpus is preserved exactly:

- **40 fault instances / 32 primary operator shapes** in five families;
- **10 author-designed post-freeze challenge operators**;
- **100 valid controls per policy**, equally stratified across five families;
- three repetitions per fault instance and policy.

`mutations.csv` is mechanically compared with the v2 catalog. New external, end-to-end, second-language, and ablation datasets must remain separate.

## Outputs

The runner emits:

- schema-v3 raw JSONL with exact oracle/actual fields;
- per-rule verifier invocation counts and verification time;
- invalidation, obligation, discharge/failure, and fact-reverification counters;
- pipeline time, per-thread allocation, and process peak working set;
- generated summaries and exact paired tests;
- environment metadata, source snapshot, and recursive SHA-256 manifest.

`whole_compilation_elapsed_ns` is intentionally `null` in this boundary experiment. The built-in timing loop is an environment-sensitive verifier-kernel diagnostic, not whole-compilation or application overhead.

## Canonical run

```bash ci-run=false
CONTRACT_EXPERIMENT_REPLICATES=5 \
  ./Tools/run-contract-experiment.sh artifacts/contract-experiment
```

The complete policy and evidence contract is in `STUDY_PROTOCOL_V3.md`; `STUDY_PROTOCOL_V2.md` remains immutable historical evidence.

## Claim boundary

- The primary/challenge sets are not independently authored.
- Boundary detection is not an end-to-end source-to-result evaluation.
- Kernel timing is not decision-grade performance evidence.
- No result may be reported unless regenerated from archived raw records for the exact commit and validated by `analyze_results.py`.
