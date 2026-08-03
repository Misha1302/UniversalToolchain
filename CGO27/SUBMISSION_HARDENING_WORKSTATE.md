# CGO 2027 submission hardening workstate

Last updated: 2026-08-03.

## Request lock

- Repository: `Misha1302/Wist2`.
- Baseline branch: `master`.
- Exact baseline head: `7840550ddbc8eb3762bd60babde3427eab02ab48`.
- Working branch: `research/cgo27-submission-hardening`.
- Historical research/evidence source remains immutable at `d6271eb2ecc4cba881ecce1263cf8e2bdc232f4a`.
- Forbidden without separate approval: merge, direct push to `master`, NuGet publication, author-list changes, public supplementary publication, or conference submission.

## Official submission constraints resolved on 2026-08-03

- CGO 2027 second-round paper deadline: 10 September 2026.
- Standard research paper target; ACM `sigplan,screen,review,anonymous` format.
- Main text limit: 11 pages excluding bibliography.
- Double-blind review; all supplementary material must be anonymized and uploaded separately.
- Paper must be self-contained; reviewers are not required to inspect supplementary material.
- Letter paper, page numbers, line numbers, black-and-white-readable figures, English PDF.

Source authority: official CGO 2027 Main Conference CFP. Re-check immediately before submission.

## Final observable result

A branch-backed, exact-head-tested Standard Research Paper and a locally built anonymous supplementary archive, with raw-data lineage, clean-unpack replay, anonymity/reproduction receipts, three model-authored adversarial review reports, submission metadata draft, and a readiness verdict that does not overclaim external independence or whole-compilation speedup.

## Dominant uncertainty

Whether owned, boundary-indexed verification obligations add a materially stronger enforceable guarantee than realistic demand-driven invalidation, and whether the implementation/evaluation support that distinction without theorem/code mismatch.

## Mutation allowlist

Active hardening may modify only:

- `CGO27/**` except immutable historical raw evidence and receipts;
- `UniversalToolchain/UniversalToolchain.ModuleContracts/**`;
- directly owning Wist policy adapters and focused tests/experiments required by the new policy;
- `eng/test-counts.json` only after mechanically recounting NUnit cases;
- exact-head CI workflows required for the hardening studies;
- paper build tooling and generated tables/figures.

Protected regions:

- existing frozen corpus inputs, historical P0--P3 denominators, raw evidence, manifests, and provider receipts;
- unrelated product/package surfaces introduced by PR #324;
- public release history.

Every commit must mechanically compare protected paths against baseline and record intentional deltas.

## Workstream ledger

| Result | Status | Observable done condition | Validation |
|---|---|---|---|
| Current baseline and CFP | DONE | exact `master` and official rules recorded | GitHub commit comparison; official CFP inspection |
| Formal obligation model | IN_PROGRESS | definitions, theorem, assumptions, induction sketch agree with code | model/code trace audit; adversarial theorem review |
| Demand-driven baseline | IN_PROGRESS | executable P1D policy, focused tests, versioned schema and corpus results | targeted tests; boundary, Wist E2E, TensorRules where applicable |
| Historical bug corpus | IN_PROGRESS | predeclared inclusion criteria and complete provenance table over eligible pre-study bugs | Git history/issues scan; replay/backport accounting |
| External frozen corpus | BLOCKED_EXTERNAL | validated human-authored frozen archive exists before policy execution | freeze/import receipt and full accounting |
| P07 repair | PENDING | root cause fixed separately, regression added, E2E corpus versioned and rerun | focused regression; all-policy fresh-process matrix |
| Running example/figures | PENDING | one code-backed example and two monochrome print-scale figures | source trace; PDF visual inspection |
| Primary-source related work | PENDING | manually verified bibliography and strongest-alternative comparison | DOI/title/venue audit |
| Pinned-machine performance | BLOCKED_PINNED_MACHINE | machine protocol passes and >=30 process replicates/configuration exist | environment receipt; raw distributions; paired bootstrap CIs |
| Full paper rewrite | PENDING | 9--11 substantive main-text pages, claims bounded by evidence/theorem | paper preflight; claim audit; visual PDF review |
| Anonymous supplement | PENDING | sanitized deterministic archive, quick/full clean replay | marker + manual identity review; manifests; clean unpack |
| Three review passes | PENDING | no unresolved blocking scientific/experimental/submission findings | bounded repair and full revalidation |
| Draft PR | PENDING | branch commits pushed and draft PR accurately describes evidence/blockers | exact-head CI and artifact identities |

## Initial confirmed gaps

1. The implementation records fact availability/invalidation and canonical verifier routes, but the paper/code do not yet expose a first-class obligation with creation boundary and first eligible boundary.
2. Current `P1Invalidation` records invalidation without an executable downstream-demand recomputation policy, making it weaker than the strongest realistic alternative.
3. Existing relative claims are corpus-bounded but the manuscript lacks a precise conditional theorem and induction argument.
4. The external author packet exists, but no external frozen corpus exists; independence remains blocked.
5. No predeclared, complete historical bug-corpus extraction has been executed.
6. P07 remains a pre-existing all-policy runtime failure and must be fixed/versioned rather than removed.
7. The paper is six pages including references and therefore materially underdeveloped relative to the 11-page limit.
8. Whole-compilation performance remains blocked; verifier-work counts may be reported only qualitatively.
9. The existing public repository/title create de-anonymization risk; a separate neutral local supplement is required.

## Iteration log

- 2026-08-03: resolved `master` as `7840550ddbc8eb3762bd60babde3427eab02ab48`, three commits ahead of historical evidence source; PR #324 changed runtime/package behavior and canonical tests from 1,579 to 1,597.
- 2026-08-03: confirmed official second-round deadline and submission-format/anonymity constraints.
- 2026-08-03: created hardening branch from exact current `master`.
