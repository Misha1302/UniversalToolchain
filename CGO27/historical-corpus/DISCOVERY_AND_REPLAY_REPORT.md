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

## Exact-revision attempt

Exact source for `eb851d4bf80f363969e04abdb4bcddf3e56830f3` was exported and manifest-checked. The historical regression corpus contains all three issue programs. An offline `--no-restore` build reached Roslyn compilation of the old dialect frontend after building most of the dependency graph, but the bounded execution environment terminated the compiler before the campaign began. Therefore:

- exact-revision replay: **`BLOCKED_RESOURCE`**;
- no new reproduction success is claimed;
- original pre-study 3/3 issue evidence remains the only positive replay evidence;
- the blocked attempt remains in every included case's accounting.

## Claim boundary

This corpus supports only the statement that three author-selected historical defects have issue-defined stable pre-study observations and map to modeled facts. It does not establish independent selection, natural-defect prevalence, complete repository history, or successful new replay on the current execution host.
