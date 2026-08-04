# CGO 2027 submission readiness

Current verdict: `CONDITIONAL_GO_AFTER_EXACT_HEAD_RECEIPT_AND_HUMAN_SIGNOFF`.

The scientific draft is materially stronger than revision `d2067422`: the production path now implements deferred cross-boundary obligation persistence, the theorem states sound-seed and truthful/complete-effect assumptions, and related work directly compares MLIR-style verification after every pass. This verdict does not predict acceptance and does not authorize merge, publication, or conference submission.

## Completed submission gates

- the formal guarantee, proof sketch, and implementation now agree on sound seeds, sound/complete effects relative to the selected vocabulary, canonical routes, earliest executable boundaries, and fail-closed enforcement;
- compilation-scoped state carries a real obligation from optimized AIR to a newly observed backend-input boundary and discharges it before backend compilation;
- focused lifecycle/orchestration tests cover carry-forward, no premature execution, final discharge, callback ordering, route failures, and cleanup after exceptions;
- the executable P1D baseline and matched queried/unqueried counterexample remain intact;
- versioned boundary, source-to-result, public-SDK-language, and ablation studies retain their frozen denominators and regenerate from raw evidence;
- P07 remains repaired independently without changing its source or oracle;
- historical screening remains fully accounted: 24 candidates = 3 included + 11 excluded + 10 blocked; exact pre-fix replay reproduces the three included cases in all 9 fresh-process attempts;
- the anonymous Letter manuscript is 11 total pages, has no undefined citations/references or overfull horizontal boxes, and explicitly discusses verifier-after-every-pass as the strongest operational alternative;
- the Generative AI disclosure no longer presents model-authored tests, reviews, or workflow orchestration as independent evidence;
- the canonical test contract is 1,632 passing tests after mechanical recount, including nine new lifecycle/conformance tests.

## Claims that remain blocked or conditional

- `BLOCKED_EXTERNAL`: no externally authored frozen corpus; therefore no independent/external-validity claim;
- `BLOCKED_PINNED_MACHINE`: no pinned performance host; therefore no whole-compilation speedup claim and no claim of superiority over per-pass verification;
- exact pre-fix reproduction is complete (3/3 cases, 9/9 attempts), but no historical P2 detection rate is claimed because the old revision predates P2 and no policy backport was executed;
- sound initial seeds, truthful/complete effect contracts, and verifier soundness are assumptions, not consequences of the scheduler;
- P2/P3 parity is empirical on the evaluated corpora, not a universal equivalence theorem;
- the public branch, working title, and intermediate results have already been exposed. Sanitization prevents direct leaks but cannot undo motivated search-based deanonymization. Chairs should be consulted, and a final submission title/metadata should be prepared privately rather than committed publicly;
- final submission requires human inspection of the exact final diff, workflow logs, primary-source citations, generated tables, PDF, and supplement.

## Required final provider receipt

The delivered revision must have all required workflows green on one exact head: canonical .NET tests, contract experiment, source-to-result experiment, Language T, ablations, paper, archival artifact, anonymous supplement, documentation/architecture checks, benchmark smoke, rollout smoke, and package compatibility. Provider artifacts must then pass independent manifest, anonymity, clean-unpack, reproduction, font, page-size, and all-page visual checks.

Merge, direct `master` push, NuGet publication, public supplementary publication, author-list changes, and actual conference submission remain forbidden without separate explicit approval.
