# CGO 2027 completion workstate

Last updated: 2026-07-31.

## Baseline

- Repository: `Misha1302/Wist2`
- Base: `master` at `c73b418c6e72e8b92371753a3a7b4a9f7adaa5f1`
- Research branch: `research/cgo27-selective-reverification`
- Draft PR: `#322`

## Completed milestones

- Four explicit verification policies are implemented and provider-verified.
- Production policy scheduling is integrated with fail-closed routing while preserving `P3_ALWAYS` as the default.
- Boundary experiment emits schema-v3 raw evidence and preserves the historical v2 corpus.
- Production test contract is 1,579/1,579 with package, validation, docs, rollout, benchmark and contract gates green on the production milestone.
- Wist source-to-result experiment collects 240 fresh-process records over 30 cases and separates five targeted faults, 24 valid controls and one stable pre-existing baseline runtime failure.
- A deterministic external blind-corpus author/freeze/import kit is present. Independent-corpus status remains `BLOCKED_EXTERNAL` until an external human corpus is frozen.

## Active validation

- Exact branch head is subject to GitHub provider verification after each research commit.
- The baseline-aware end-to-end validator is the canonical WP5 accounting owner.

## Remaining material results

- Provider-backed second-language package and report.
- Pinned-machine whole-compilation performance experiment with raw distributions and confidence intervals.
- Ablations.
- Related-work and claim/evidence ledger.
- Anonymous paper and artifact bundle.
- Externally authored blind corpus import and execution (`BLOCKED_EXTERNAL`).

## Authority

- No direct push to `master`.
- No package publication.
- No conference submission.
- No merge without explicit user authorization after green gates and final review.
