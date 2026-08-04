# Historical corpus discovery and replay report

Date: 2026-08-04
Protocol: `PROTOCOL.md` v1
Cutoff: `2026-07-29T23:59:59Z`

## Discovery accounting

The GitHub issue scan returned 12 pre-cutoff issue records; pull requests were not counted as issues. A broad pre-cutoff semantic-fix commit-message search and an exported-source regression-test scan were then screened under the frozen inclusion/exclusion rules. Exact issue creation timestamps are retained in `candidates.json` rather than rounded to a calendar day.

The frozen table contains 24 records:

- 3 included;
- 11 excluded;
- 10 blocked;
- 0 invalid replays.

No record was silently dropped after policy output. The three included cases are issue-specified regressions #302, #303 and #307. Their source programs, expected outcomes or diagnostic families, pre-fix evidence, and fixing revision were recorded before this study. Each issue reports stable fresh-process confirmation of 3/3 with zero flaky or infrastructure attempts.

## Exact-revision replay

Exact source for `eb851d4bf80f363969e04abdb4bcddf3e56830f3` was exported, manifest-checked, restored from an offline package feed, and built with .NET SDK 10.0.301/runtime 10.0.9. The exact historical graph completed with zero warnings and zero errors. The frozen three-case regression campaign then ran each case in three fresh processes.

Outcome:

- exact-revision replay: **`REPRODUCED_EXACT_PREFIX`**;
- included cases reproduced: 3/3;
- stable attempts: 9/9;
- flaky cases: 0;
- inconclusive cases: 0;
- infrastructure failures: 0;
- distinct finding classes: 2.

The detailed immutable receipt is `EXACT_REPLAY_RECEIPT_2026-08-04.md`; the machine-readable summary is `exact-replay-summary.json`.

## Claim boundary

This corpus supports the statement that all three frozen, author-selected historical defects reproduce on the exact pre-fix revision under the issue-defined oracles. It does not establish a historical P2 detection rate because the replayed revision predates P2 and no policy backport was executed. It also does not establish independent selection, natural-defect prevalence, or complete repository-history coverage.
