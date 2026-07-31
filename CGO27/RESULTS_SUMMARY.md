# CGO 2027 results summary

Status: evidence-backed research summary; not a submission receipt.

## Boundary study

The frozen Wist boundary corpus preserves its historical identifiers and denominators.

| Corpus | P0 structural | P1 invalidation | P2 selective | P3 always |
|---|---:|---:|---:|---:|
| Primary operator shapes | 12/32 | 28/32 | 32/32 | 32/32 |
| Challenge operators | 1/10 | 10/10 | 10/10 | 10/10 |
| Valid controls rejected | 0/100 | 0/100 | 0/100 | 0/100 |

P2 and P3 agree on outcome, diagnostic family and first detection boundary for all 42 evaluated primary/challenge operator shapes. Historical review holdouts remain outside these denominators.

## Wist source-to-result study

Provider-backed artifact for commit `acc60612361f240d5bd24f148ea7fa6eb5e1f111`:

- workflow run `30661725052`;
- artifact ID `8805491648`;
- artifact digest `sha256:f3f34c33e2595f95d061fddc9fae213818024831151334d73aa12db11ff3754b`;
- 30 source programs in three strata;
- four policies;
- two fresh-process repetitions per case/policy;
- 240 raw records;
- five targeted optimizer faults;
- 24 valid controls;
- one pre-existing baseline runtime failure (`P07`) reported separately.

For the five targeted faults:

- CIL P0/P1 executions produce a wrong result;
- interpreter P0/P1 executions reach a later backend/runtime failure;
- P2/P3 reject at the optimized-AIR contract boundary;
- P2/P3 classification parity is 30/30 across all source cases.

The `P07` failure occurs identically under all four policies without fault injection. It is neither a protocol fault nor a valid control.

## TensorRules second-language package

Provider-backed public-SDK artifact for the same commit:

- workflow run `30661725387`;
- artifact ID `8805405891`;
- artifact digest `sha256:07fb7e7e9da11f8875a2bb58b291a01903756de2ccd9af85dc5117adc89dc404`;
- two valid examples;
- two intrinsically invalid examples;
- eight semantic fault operators;
- 48 observations;
- P2/P3 parity 12/12;
- no Wist project references;
- checksum-verified artifact.

TensorRules is model-authored and is described as a second language package, not an independently authored language.

## Exact-head verification

Commit `acc60612361f240d5bd24f148ea7fa6eb5e1f111` completed `.NET CI`, validation, Docs Check, package compatibility, Contract Experiment, Wist end-to-end, TensorRules, rollout, benchmark smoke and published-package smoke successfully.

## Performance

No decision-grade whole-compilation result exists. Hosted-CI and verifier-kernel timing are smoke diagnostics only. Performance claims remain blocked until the pinned-machine protocol is satisfied.

## External corpus

No external human-authored corpus has been supplied. The deterministic author/freeze/import kit exists, but independent-corpus claims remain `BLOCKED_EXTERNAL`.
