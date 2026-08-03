# UniversalToolchain contract-guided reverification experiment

This non-packable project evaluates five verification policies over production UniversalToolchain contract-table, ownership, Bytecode, AIR, facts/effects, capability, and reverification components. It does not replace those components with a synthetic detector.

The experiment is research infrastructure, not part of the public Wist package surface. The canonical runner restores and builds it against the exact current production API before collecting results. The historical v2 implementation remains in `Program.cs` but is excluded from compilation; the active runner is the `Cgo27Program.*.cs` partial class set.

## Compared policies

- **`P0_STRUCTURAL`** — structural AIR and target-capability checks; no semantic obligation routing.
- **`P1_INVALIDATION`** — typed contracts and facts/effects; invalidations are tracked, but requested semantic reverification is not automatic.
- **`P1D_DEMAND_RECOMPUTATION`** — recomputes an invalid fact only when an explicit downstream query demands it.
- **`P2_SELECTIVE`** — `P1` plus deterministic discharge of every boundary obligation at its first eligible boundary.
- **`P3_ALWAYS`** — `P1` plus unconditional execution of every verifier applicable to the represented semantic boundary.

`P2_SELECTIVE` and `P3_ALWAYS` share the same runner, cases, artifacts, and production verifier implementations. Both the runner and the independent evidence gate reject disagreement in outcome, diagnostic family, or first detection boundary.

## Evidence sets and oracle

The historical corpus is preserved exactly:

- **40 fault instances / 32 primary operator shapes** in five families;
- **10 author-designed post-freeze challenge operators**;
- **100 historical valid controls per historical policy**, equally stratified across five families;
- three repetitions per fault instance and policy.

`mutations.csv` remains byte-identical to the v2/v3 historical catalog and is validated by `oracles-v3.json` plus `validate_oracles.py`. The matched queried/unqueried P1D baseline is a separate `demand-v4` dataset with `demand-mutations-v4.csv`, `demand-oracles-v4.json`, and `validate_demand_oracles.py`. It does not enter historical denominators. New external, end-to-end, second-language, historical-replay and ablation datasets also remain separate.

## Outputs

The runner emits:

- schema-v4 raw JSONL with exact oracle/actual fields and explicit demand-query identity;
- per-rule verifier invocation counts and verification time;
- invalidation, obligation, discharge/failure, and fact-reverification counters;
- pipeline time, per-thread allocation, and process peak working set;
- an oracle-validation receipt, generated summaries, and exact paired tests;
- environment metadata, source snapshot, and recursive SHA-256 manifest.

`whole_compilation_elapsed_ns` is intentionally `null` in this boundary experiment. The built-in timing loop is an environment-sensitive verifier-kernel diagnostic, not whole-compilation or application overhead.

## Canonical run

```bash ci-run=false
CONTRACT_EXPERIMENT_REPLICATES=5 \
  ./Tools/run-contract-experiment.sh artifacts/contract-experiment
```

The active policy and evidence contract is in `STUDY_PROTOCOL_V4.md`; v2 and v3 remain immutable historical evidence.

## Claim boundary

- The primary/challenge sets are not independently authored.
- Boundary detection is not an end-to-end source-to-result evaluation.
- Kernel timing is not decision-grade performance evidence.
- No result may be reported unless regenerated from archived raw records for the exact commit, accepted by both oracle validators, and validated by `analyze_results.py`.
