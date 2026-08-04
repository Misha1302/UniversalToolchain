# Adversarial review 4 — post-repair scientific and implementation audit

Review type: model-authored adversarial pass; not an independent external or program-committee review.
Scope: the previously identified CGO-readiness blockers, production implementation, formal claim boundary, exact historical replay, strongest-alternative comparison, anonymous paper, and reproducibility package.

## Resolved findings

1. **The theorem previously conflated a declared fact with a true fact.** The statement and proof now assume sound initial seeds and semantically sound and complete effect contracts relative to the modeled fact set. The result is explicitly a relative scheduling guarantee; it does not prove contract truth, fact-set completeness, transformation correctness, or whole-compiler correctness.
2. **The production path previously recreated fact state at every observer callback.** The implementation now maintains compilation-scoped fact state and pending obligations across bytecode, AIR, optimized-AIR, and backend-input boundaries. The state is keyed by the concrete compilation-input identity, protected against interleaving, and removed on final discharge or failure.
3. **Every obligation previously had an immediate deadline.** Canonical verifier routes now declare their earliest executable boundary. An invalidation creates an obligation whose first eligible boundary is the later of its creation boundary and the route boundary. A production conformance test observes an optimized-AIR invalidation, verifies that it remains pending, and discharges it only at backend input.
4. **The old resource blocker prevented exact historical reproduction.** The exact pre-fix source revision was restored and built offline. All three frozen issue-defined regressions reproduced in all three fresh-process attempts each: 3/3 cases and 9/9 attempts, with no flaky, inconclusive, or infrastructure outcomes.
5. **The manuscript did not confront the strongest practical alternative directly.** It now discusses verifier-after-every-pass and pass instrumentation, including the MLIR-style design point, and narrows the contribution to typed invalidation, canonical ownership, executable-boundary scheduling, and fail-closed routing.
6. **Baseline establishment and policy-dependent reverification were previously easy to conflate.** The implementation and manuscript now distinguish the initial semantic baseline from later P1/P1D/P2/P3 scheduling behavior.
7. **The earlier disclosure overstated what human authors had already done.** The anonymous disclosure now describes tool-assisted drafting, code generation, experiment orchestration, and adversarial checking without calling model-authored work independent review. Human verification and accountability remain required before submission.
8. **The canonical test receipt was stale.** Mechanical discovery now reports 1,632 passing tests: 555 core, 305 modules, 639 dialects, 82 language-SDK, 41 plan-fuzz, and 10 isolated integration tests, with zero failures or skips.

## Verification performed

- full 77-project test dependency graph built with zero warnings and zero errors;
- 1,632/1,632 canonical tests passed, plus test-count, retired-surface, and documentation mutation checks;
- 320/320 fresh-process source-to-result records validated, preserving P2/P3 classification and first-boundary parity on all 32 programs;
- boundary, challenge, demand, control, mechanism, and second-language validation gates remained satisfied;
- exact pre-fix historical campaign completed 3/3 cases and 9/9 attempts;
- eleven-page Letter PDF rebuilt with embedded fonts, no undefined references/citations, no overfull horizontal boxes, no direct identity/path leak, and all-page visual inspection;
- two anonymous supplement builds were byte-identical; clean-unpack quick/full replay, manifest verification, table regeneration, and anonymity scan passed.

## Remaining limitations

1. **No independent external corpus.** The exact historical cases arose in the same project and were selected by the authors. They strengthen ecological validity but do not constitute independent external validation.
2. **No historical P2 detection rate.** The reproduced revision predates P2 and no backport was executed. The evidence establishes exact pre-fix reproducibility only.
3. **No decision-grade whole-compilation cost comparison.** Hosted and local runs establish functional reproducibility, not a pinned-machine comparison against verifier-after-every-pass or manually selected boundaries.
4. **Route granularity remains coarser than individual semantic facts.** Several facts may share one verifier rule, so selectivity is bounded by the registered route rather than necessarily by a single internal check.
5. **Public-history deanonymization risk is irreducible.** The anonymous package removes direct identifiers, but the already-public development history can still support motivated inference.
6. **Human scientific signoff remains outstanding.** A human author must independently inspect the theorem assumptions, source changes, raw evidence, citations, disclosure, and final provider artifacts before submission.

## Adversarial verdict

`CONDITIONAL_GO_AFTER_EXACT_HEAD_CI_AND_HUMAN_SIGNOFF`.

The repaired work is materially stronger than the reviewed revision: the central lifecycle now exists in the production path, the formal guarantee matches its assumptions, and historical regressions are exactly reproducible. The remaining limitations bound generality and performance claims rather than contradict the core mechanism. Acceptance at CGO remains uncertain because external validation and decision-grade cost comparison are still absent; the work should not be described as independently validated or performance-superior.

This verdict authorizes neither merge, publication, artifact release, nor conference submission.
