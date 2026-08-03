# CGO27 hardening mutation receipt — 2026-08-03

Baseline tree: local deterministic snapshot of GitHub branch head `c50d4ac598c2e414c96bcecdfc6cbfc89e4be979`.

## Allowed mutations exercised

- first-class compiler-fact/verification-obligation contracts and scheduler;
- focused owning tests and Wist policy adapter;
- numeric-promotion owner and P07 regression;
- versioned boundary, Wist E2E and Language T experiment runners;
- active test-count manifest and active documentation mirrors;
- paper sources, formal audit and predeclared historical-corpus protocol;
- exact-head/historical source-export CI.

## Protected-region comparison

The following protected historical regions have no diff against the baseline:

- `CGO27/artifact/evidence/**`;
- existing raw experiment evidence and provider receipts under `CGO27/**/evidence/**`;
- historical P0--P3 input corpora and existing frozen denominators;
- unrelated package/runtime changes introduced by PR #324.

New protocol versions add P1D and demand-specific cases without rewriting historical P0--P3 rows. P07 keeps its original program and oracle; the repaired result is recorded in a new E2E schema version.

## Mechanical checks

- protected historical evidence path diff: empty;
- documentation-status validator: PASS;
- Core TRX: 546 passed, 0 failed, 0 skipped;
- Modules TRX: 305 passed, 0 failed, 0 skipped;
- Dialects TRX: 639 passed, 0 failed, 0 skipped;
- canonical total with unchanged remaining suites: 1,623 passed.

Exact GitHub-head CI and final experiment reruns remain required after push.
