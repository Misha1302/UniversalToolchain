# CGO 2027 source-to-result end-to-end experiment

This non-packable executable drives the real Wist composition, compilation, optimization and backend path in fresh child processes.

## Corpus

- 30 deterministic Wist source cases;
- three equal strata: constant expressions, parameterized expressions and CIL/interpreter cross-checks;
- five model-authored exploratory cases with the `cgo27.replace-result-v1` optimizer mutation;
- four production verification policies;
- two fresh-process repetitions for every case/policy pair.

The fault optimizer replaces the computed result with the structurally valid sequence `Drop; load_i32(1)`. Its typed contract declares that this emission requires a deliberately unselected result-integrity capability and invalidates `AirVerified` at the optimized-AIR boundary. `P0_STRUCTURAL` and `P1_INVALIDATION` therefore demonstrate a silent wrong result, while `P2_SELECTIVE` and `P3_ALWAYS` must reject at optimized AIR with `MissingBackendCapability`.

This is a model-authored exploratory corpus. It is not the externally authored blind corpus required for the final paper claim.

## Run

```bash ci-run=false
./Tools/run-cgo27-end-to-end.sh artifacts/cgo27-end-to-end
```

The output contains raw JSONL, the materialized case catalog, environment metadata, a summary, a source snapshot and a recursive SHA-256 manifest.
