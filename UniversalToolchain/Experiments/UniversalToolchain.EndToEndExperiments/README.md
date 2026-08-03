# CGO 2027 source-to-result end-to-end experiment

This non-packable executable drives the real composed-language frontend, compilation, optimization and backend path in fresh child processes.

## Versioned corpus

Protocol/schema v3 preserves the historical v2 denominator and adds the demand baseline without rewriting old evidence:

- historical v2 set: 30 deterministic source cases in three ten-case strata;
- valid controls include the unchanged `P07` source `x * 2 + y`, whose pre-existing mixed numeric type failure is fixed by a separately tested runtime change;
- five historical model-authored exploratory fault cases;
- demand v3 set: two matched fault cases, `D01` with an explicit downstream query for `AirVerified` and `D02` without that query;
- five policies: P0, P1, demand-driven P1D, obligation-guided P2, and always-verify P3;
- two fresh-process repetitions for every case/policy pair.

The fault optimizer replaces the computed result with the structurally valid sequence `Drop; load_i32(1)`. Its typed contract declares that this emission requires a deliberately unselected result-integrity capability and invalidates `AirVerified` at the optimized-AIR boundary.

P1D recomputes only when a downstream consumer explicitly queries the invalidated fact. It must therefore reject `D01` but preserve the no-protocol symptom for matched case `D02`. P2 and P3 must reject both at the first eligible optimized-AIR boundary. This difference is the executable counterexample to treating lazy demand recomputation as equivalent to boundary-indexed obligation discharge.

This remains a model-authored exploratory corpus. It is not the externally authored blind corpus required for an independence claim.

## Run

```bash ci-run=false
./Tools/run-cgo27-end-to-end.sh artifacts/cgo27-end-to-end-v3
```

Raw JSONL is written and flushed before validation. The output also contains the materialized case catalog, prevalidation state, environment metadata, validated summary, source snapshot, exact source identity and a recursive SHA-256 manifest.
