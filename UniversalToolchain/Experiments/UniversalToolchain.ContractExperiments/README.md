# UniversalToolchain contract experiment

This non-packable project evaluates executable cross-layer contracts for the ICSE 2027 SRC study. It calls the production contract-table, Bytecode, AIR, facts/effects, ownership, and capability verifiers; it does not replace them with a synthetic detector.

## Compared modes

- **B0 — structural baseline:** existing AIR structure and target-capability checks; no module ownership, Bytecode drift, or compiler-fact/effect enforcement.
- **B1 — typed contracts:** B0 plus selected module/ownership, Bytecode, and facts/effects checks; requested reverification is recorded but is not fail-closed.
- **B2 — full protocol:** B1 plus mandatory failure on unresolved reverification requests.

## Evidence sets

The primary catalog is frozen as **40 fault instances representing 32 operator shapes** in five families. Identifier-only paired instances remain in the raw records, but all primary statistical inference uses the 32 operator shapes.

A separate **10-operator post-freeze challenge set** exercises diagnostic operators absent from the primary catalog. It is useful robustness evidence, but it was designed by the project author after the primary study and is neither blind nor independently authored.

The negative control corpus contains **100 valid configurations per mode**, stratified equally across ownership, Bytecode, facts/effects, AIR structure, and capability selection. These controls check the experiment model; they are not a population-level false-positive estimate.

Every primary/challenge mutation instance is repeated three times per mode. Any unstable detection, diagnostic, or owner-boundary classification invalidates the run. A secondary microbenchmark uses 33 counterbalanced samples per mode and five process-level replicates.

## Canonical run

The following commands are excluded from repository-wide Markdown smoke checks because the dedicated CI workflow runs and archives the complete study.

```bash ci-run=false
ICSE_EXPERIMENT_REPLICATES=5 \
  ./Tools/run-contract-experiment.sh artifacts/contract-experiment
```

The output contains raw JSONL, the mutation/operator catalog, stratified controls, environment metadata, process-level timing replicates, analysis, frozen study protocol, exact source files used by the runner, and a recursive SHA-256 manifest.

## Interpretation boundary

- Primary and challenge faults are author-designed.
- The challenge set uses new operators but was not selected by an independent evaluator.
- The experiment invokes production verifier components at compiler boundaries; it does not mutate every end-to-end Wist source-to-execution path.
- The timing result is the cost of isolated boundary validation, not whole-program compilation or execution overhead.
- Results must not be generalized to unrelated compilers without independent replication.
- No test count, detection rate, or overhead may be reported unless regenerated from archived raw records for the exact commit.
