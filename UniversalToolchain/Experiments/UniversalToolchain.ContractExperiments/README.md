# UniversalToolchain contract-guided reverification experiment

This non-packable project evaluates executable cross-layer contracts over production UniversalToolchain verifier components. It calls the real contract-table, Bytecode, AIR, facts/effects, ownership, capability, and reverification checks; it does not replace them with a synthetic detector.

The experiment is retained as research infrastructure. It is not part of the public Wist package surface and it is not executed by application consumers.

## Compared modes

- **B0 — structural baseline:** AIR structure and target-capability checks only; no module ownership, Bytecode drift, or compiler-fact/effect enforcement.
- **B1 — typed contracts:** B0 plus selected module/ownership, Bytecode, and facts/effects checks; requested reverification is recorded but unresolved requests are not fail-closed.
- **B2 — full protocol:** B1 plus mandatory failure on unresolved reverification requests.

## Evidence sets

The frozen primary catalog contains **40 fault instances representing 32 independent operator shapes** in five families. Identifier-only paired instances remain in the raw records, but primary statistical inference uses operator shapes rather than renamed copies.

A separate **10-operator post-freeze challenge set** exercises diagnostic operators absent from the primary catalog. It is robustness evidence, but it is author-designed and neither blind nor independently authored.

The negative-control corpus contains **100 valid configurations per mode**, stratified equally across ownership, Bytecode, facts/effects, AIR structure, and capability selection. These controls validate the experiment model; they are not a population-level false-positive estimate.

Every primary and challenge mutation instance is repeated three times per mode. Any unstable detection, diagnostic, or owner-boundary classification invalidates the run. A secondary microbenchmark uses 33 counterbalanced samples per mode and five process-level replicates.

## Canonical run

The dedicated workflow runs and archives the complete study, so this command is excluded from repository-wide Markdown execution:

```bash ci-run=false
CONTRACT_EXPERIMENT_REPLICATES=5 \
  ./Tools/run-contract-experiment.sh artifacts/contract-experiment
```

The output contains raw JSONL, the mutation/operator catalog, stratified controls, environment metadata, process-level timing replicates, generated analysis, the frozen protocol, exact runner sources, and a recursive SHA-256 manifest.

## Interpretation boundary

- Primary and challenge faults are author-designed.
- The challenge set uses operators absent from the primary catalog, but it was not selected by an independent evaluator.
- The experiment invokes production verifier components at compiler boundaries; it does not mutate every end-to-end Wist source-to-execution path.
- The timing result is isolated validation-kernel cost, not whole-compilation or application overhead.
- Results must not be generalized to unrelated compilers without independent replication.
- No detection count, significance result, or overhead may be reported unless regenerated from archived raw records for the exact commit.
- PlanFuzz remains a separate configuration-aware relational testing system; neither experiment substitutes for the other.
