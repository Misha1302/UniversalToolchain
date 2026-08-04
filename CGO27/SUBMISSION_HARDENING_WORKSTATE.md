# CGO 2027 submission hardening workstate

Last updated: 2026-08-04.

## Request lock and safety boundary

- Repository: `Misha1302/Wist2`.
- Baseline branch: `master`; exact baseline head: `7840550ddbc8eb3762bd60babde3427eab02ab48`.
- Working branch: `research/cgo27-submission-hardening`; draft PR #325.
- Historical research/evidence source remains immutable at `d6271eb2ecc4cba881ecce1263cf8e2bdc232f4a`.
- Forbidden without separate approval: merge, direct push to `master`, NuGet publication, author-list changes, public supplementary publication, and conference submission.

## Official submission constraints

Resolved from the official CGO 2027 Main Conference CFP and to be rechecked immediately before submission:

- second-round paper deadline: 10 September 2026 AoE;
- Standard Research Paper, ACM `sigplan,screen,review,anonymous` format;
- 11 pages of main text excluding bibliography;
- Letter paper, page and line numbers, double-blind manuscript;
- supplementary material anonymized and uploaded separately; the paper remains self-contained.

## Final observable result

A branch-backed, exact-head-tested Standard Research Paper and locally delivered anonymous supplementary archive, with raw-data lineage, deterministic manifests, clean quick/full replay, anonymity receipts, three model-authored adversarial reviews, submission metadata, and a bounded readiness verdict that makes no external-independence or whole-compilation-speed claim.

## Protected scope

Historical P0--P3 denominators, frozen inputs, raw evidence, manifests, receipts, unrelated package/runtime behavior, public release history, and `master` remain protected. New demand cases and repaired studies are separately versioned.

## Completion ledger

| Result | Status | Verified observable result |
|---|---|---|
| Current baseline and CFP | DONE | exact baseline and official rules recorded |
| Formal obligation model | DONE | sound seed/effect assumptions, canonical-owner routes with earliest executable boundaries, fail-closed scheduling, and corrected induction sketch |
| Demand-driven baseline | DONE | executable P1D; matched queried/unqueried cases in boundary, System W, and System T studies |
| Historical screening | DONE_BOUNDED | frozen accounting: 24 = 3 included + 11 excluded + 10 blocked; no cases silently removed |
| Historical exact-prefix replay | DONE_BOUNDED | exact pre-fix graph builds offline; 3/3 cases reproduce in 9/9 fresh-process attempts, with no flaky/infrastructure result; no historical P2-rate claim |
| External frozen corpus | BLOCKED_EXTERNAL | human-authored frozen archive absent; author packet and import validator ready |
| P07 repair | DONE | unchanged source/oracle now returns 13 under CIL/interpreter and all policies; regression coverage added |
| Running example and figures | DONE | one code-backed example and two monochrome print-scale figures visually inspected |
| Primary-source related work | DONE | bibliography ledger, strongest-alternative prose, and feature comparison table |
| Pinned-machine performance | BLOCKED_PINNED_MACHINE | runner/protocol ready; no whole-compilation speedup claim |
| Paper rewrite | DONE_LOCAL | 11-page anonymous Letter PDF; strongest per-pass-verifier comparison, corrected assumptions, lifecycle conformance, bounded disclosure |
| Anonymous supplement | DONE | deterministic neutral package; manifest, anonymity, quick/full replay, and table regeneration pass |
| Adversarial reviews | DONE_BOUNDED | professional PC-style review findings were converted into code/model/baseline/anonymity fixes; external evidence blockers remain |
| Canonical code validation | LOCAL_GATE_RUNNING | focused lifecycle suite passes; mechanically updated contract is 1,632 tests; exact-head provider CI remains required |
| Draft PR and provider receipt | FINAL_GATE | PR remains draft; exact-head workflows and provider identities must refer to the same final revision |

## Final scientific boundary

The central contribution is a composition contract: invalidation creates a uniquely owned, boundary-indexed verification obligation that may persist until its earliest executable boundary and must then be discharged or compilation fails closed. The theorem is conditional on sound initial seeds, truthful and complete effects relative to the selected vocabulary, unique sound routes, scheduler coverage, and verifier soundness. It is not a proof of the whole compiler. P2/P3 parity is empirical on the evaluated corpora. System T is a second public-SDK package but not independent. Historical screening is transparent and exact pre-fix reproduction succeeds, but no historical P2 rate is available because the replayed revision predates the policy.

## Iteration log

- 2026-08-03: resolved current `master`, created the hardening branch and draft PR, and froze the mutation/protection contract.
- 2026-08-03: implemented first-class obligations, P1D, fail-closed routing, P07 numeric promotion, versioned experiments, and focused/full regression coverage.
- 2026-08-03: completed boundary v4, System W schema v3 (320 records), System T schema v2 (70 observations), and eight mechanism ablations.
- 2026-08-03: completed frozen historical screening and preserved the initial resource-limited exact-revision replay attempt.
- 2026-08-03: built deterministic archival and anonymous artifacts; clean-unpack, anonymity, and full reproduction passed.
- 2026-08-04: expanded the manuscript to ten pages, added explicit units/oracle methodology, complete historical accounting, strongest-alternative feature comparison, and anonymous Generative AI disclosure; local PDF preflight and all-page visual inspection passed.
- 2026-08-04: implemented compilation-scoped pending-obligation state, a production backend-input hook, route availability metadata, deferred carry-forward/discharge, cleanup on failure, and nine focused conformance tests; corrected P1/P1D so passive obligations do not become enforcement failures.
- 2026-08-04: corrected theorem assumptions, added an explicit MLIR verifier-after-every-pass comparison, narrowed the performance claim, made the GenAI/anonymity limitations literal, and rebuilt the paper to 11 Letter pages without undefined references or overfull horizontal boxes.
- 2026-08-04: restored and built exact pre-fix revision `eb851d4bf80f363969e04abdb4bcddf3e56830f3` offline; all three frozen regressions reproduced in 9/9 fresh-process attempts with no flaky, inconclusive, or infrastructure outcomes.
- 2026-08-04: final exact-head provider receipt is the remaining mechanical gate; no merge, publication, or submission action is authorized.
