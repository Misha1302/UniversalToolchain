# Meetup narrative evidence: guarded options for a 10–12 minute talk

**Status:** future talk research, **not a slide rewrite or approved title**.
**Reviewed:** 2026-10-23. **UT base:** [`de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1`](https://github.com/Misha1302/UniversalToolchain/tree/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1).
**Presentation limitation:** the specific Scientific_Open_Source_Meetup_2026 HTML/PDF, 9m30s script, evaluator memo and final abstract were *not available*. No statement here asserts that the existing deck has been audited. One earlier LangDev 18-slide deck is listed in the Ideal UT dossier but is not a substitute for this talk.

## EVIDENCE-BRIEF: what we can safely say

**Allowed, with source links and scope**

- UT's existing language-authoring SDK resolves independently authored contribution metadata into a deterministic `LanguagePlan` and executes selected exact routes through a typed runtime; [source](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs), [route](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs), [runtime assembler](../../../UniversalToolchain/UniversalToolchain.Runtime/LanguageRouteRuntimeAssembler.cs).
- A non-Wist pricing-language sample composes its own parser, interpreter and compiled executor; this is evidence of generic language authoring, **not semantic N+1**; [source](../../../samples/Acme.PricingLanguage/Program.cs).
- The project has *experimental, non-packable* PlanFuzz configuration-aware differential/relational oracle tooling and ContractExperiments on verifier boundaries; historically recorded results and limitations are pinned in [PlanFuzz status](../../proposals/planfuzz/implementation-status.md), [ContractExperiments protocol](../../../UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md) and [VERIFICATION](../../../VERIFICATION.md).
- LLVM AA, MLIR External Models/DataFlow/Bufferization and LLVM Attributor already offer important parts of independent analysis composition; [source comparison](PRIOR_ART_COMPARISON.md).
- General cross-representation trust-bearing semantic-evidence composition in UT is **proposed and not established**; [proposal](ARCHITECTURE.md), [API sketches](PROPOSED_API_CONTRACTS.md).

**Forbidden without new evidence**

- “We invented independent analyses that improve existing optimizer code” — false novelty against LLVM/MLIR.
- “A new fact automatically teaches any existing optimizer a new optimization” — absent known consumer query or transform.
- “UT mathematically proves arbitrary third-party modules/optimizations safe” — no such generic theorem/kernel.
- “The system carries verified semantic truth across every AST/Bytecode/AIR/SSA lowering” — untested proposal.
- “No adapters / no cost / zero overhead / faster than LLVM or MLIR” — no controlled measurement.
- “A malicious managed plugin is sandboxed by a plan hash” — explicitly unclaimed.
- “PlanFuzz finds more bugs than conventional fuzzers” — no equal-budget comparison.
- “ContractExperiments' kernel timing equals whole compilation overhead” — explicitly prohibited by study protocol.

## NARRATIVE_CANDIDATES (editorial options, not experiment-derived numerical ranking)

These are qualitative editorial ratings, **not measured audience outcomes**. “Originality” assesses a defensible *specific presentation angle*, not peer-reviewed scientific novelty.

| Candidate | Noncompiler accessibility | Practical value | Engineering depth | Research angle | Working code support | Distinctive angle | Concrete demo | Overclaim risk | 10–12 min fit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **A. PlanFuzz: testing configurations, not only source programs** | Medium–High | High for plugin/compiler developers | High | Strong falsifiable oracle design | High (experimental code and historical fixed regressions) | Test selected/excluded extension surfaces and route activation | Strong once exact replay is rehearsed | Moderate; must not claim comparative superiority | **Strongest initial candidate** |
| **B. Fail-closed contracts: semantic obligations after transformations** | Medium | High for safety-sensitive pipelines | High | Strong bounded experimental comparison | High (production boundaries + nonpackable study) | Demonstrate why structural valid ≠ verified semantics | Strong for one intentionally corrupted artifact | Moderate–High; avoid universal formal-proof language | Strong if constrained to a single fault |
| **C. One language definition, one plan, several concrete routes** | High | High for DSL/toolchain teams | Medium–High | Modest: engineering architecture and invariants | High (current UT/Acme) | Determinism, exact provider identity, no runtime rediscovery | Simple Acme config/route switch | Low with careful claims | **Safest broad-audience version** |
| **D. A failed hypothesis: can independent semantic evidence beat plain C# and MLIR?** | Medium | Conditional | High | Potentially very strong *negative-result* research story | Low for general kernel; limited C# control | Fairly comparing the strongest rivals, admitting negative evidence | Control baseline plus unexecuted proposal only | High if presented as completed | Good **as open research**, not a success demo |

### Candidate A — PlanFuzz

**Hook:** A compiler can return the same value and still activate a contribution that was supposed to be excluded, or produce different routes under permuted registration order. A source-only oracle may not ask these configuration questions.

**Sequence:** a tangible extension/surface bug → selected-world graph → `O-004` excluded-owner and `O-005` extension noninterference oracle → fresh-process replay/minimization → one concrete limitation. Distinguish previously fixed regressions from current discovery and unique defects from normalized finding classes.

**Why credible:** [real nonpackable code and replay contracts](../../proposals/planfuzz/implementation-status.md) already exist. **Missing:** equal-budget baseline, measured new-bug yield, rehearsed current-head output, independent holdout. Rehearse before putting numbers or a live demo on slides.

### Candidate B — Contract-guided reverification

**Hook:** Compiler transformations can preserve structural shape while invalidating facts that the next stage relies upon. A pass may need an obligation to re-check—not a mere assertion that its output type is correct.

**Sequence:** original artifact → deliberate fact invalidation → `ReverificationRequest` → P0 structural versus P2 selective and P3 always → recorded diagnostic boundary and explicit cost boundary. **Never** equate metadata and mathematically proven semantics.

**Why credible:** actual `ModuleContractVerificationScheduler`, `AirVerifier`/`BytecodeVerifier` and frozen study protocol. **Missing:** external fault corpus, applicability to whole compilation and stage-specific perf intervals. Historic author-designed fault counts are not population claims.

### Candidate C — Deterministic composition / exact runtime

**Hook:** A DSL might appear configurable until compilation/runtime pick different contributors or backend routes. One plan and exact materialization isolate that failure category.

**Sequence:** language feature descriptors → `LanguageCompiler` closes dependencies → canonical immutable `LanguagePlan` → precise backend route → `LanguageRuntime` validates actual selected source → one Acme interpreter/compiled result. Distinguish config/route correctness from semantics of arbitrary passes.

**Why credible:** [LanguageCompiler](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs), [runtime](../../../UniversalToolchain/UniversalToolchain.Runtime/LanguageRuntime.cs), [Acme](../../../samples/Acme.PricingLanguage/Program.cs). **Missing:** independent adoption/productivity comparison. A single sample is enough to show the pipeline, not ecosystem maturity.

### Candidate D — Negative or open research on independent semantics

**Hook:** “Add one analysis; old optimizer improves.” Then show LLVM AA and MLIR counterexamples immediately. Ask what *harder* property we actually need—revision-aware trust across independent source and runtime packages and mismatched representations.

**Sequence:** old assumption → strongest competitors → tiny C# baseline (10 decision assertions, **not actual check elimination**) → exact unsolved E1 requirements → precommitted failure criteria. This is an intellectually honest *open research* talk only.

**Why risky:** the most impressive kernel behaviors are not shipped; a 10-minute talk may look like a proposal rather than a concrete open-source result. Use only if the audience explicitly wants a research process/negative-result talk.

## Cautious preferred decision

**First candidate to prototype for stage 3: A (PlanFuzz)**, because it supplies the most demonstrable combination of engineering mechanism, adversarial failure and observable test machinery, subject to a current-head rehearsal. **Safe fallback: C**, especially for a mixed developer audience. B is competitive for compiler verification or safety-oriented listeners. D is valuable research material but currently the weakest completed-demo story.

This is an **editorial recommendation from implementation/evidence maturity**, not an objective speaker-audience measurement. Do not choose final title until deck/script and run artifacts can be inspected; avoid ranking candidates by imagined view counts or popularity.

## Provisional 10–12 minute story template

For A, as an editable planning outline (not the actual presentation):

1. **0:00–1:00 — Concrete failure:** a source program is correct but a forbidden component activates.
2. **1:00–2:30 — Why source-only tests miss the chosen axis:** package surface, route, component identity.
3. **2:30–4:45 — One selected `LanguagePlan`:** demonstrate metadata, route and activation witness.
4. **4:45–7:30 — One real PlanFuzz oracle + replay:** show expected/actual result and reduction, with confirmed run identity.
5. **7:30–9:30 — Technical failure boundaries:** unknown/inconclusive are not success, seeded fault ≠ real bug.
6. **9:30–11:00 — Independent prior art and future checks:** fair baseline and what has not been shown.
7. **11:00–12:00 — Takeaway:** composition requires testing both program behavior and selected configuration.

For a strict **9m30s** slot: cut secondary pipeline taxonomy and reserve one minute for the negative-boundary takeaway. Do not make a live demonstration dependency without a tested recorded fallback.

## What stage 3 must verify before editing any slide

- Obtain and actually read the current `Scientific_Open_Source_Meetup_2026` HTML/PDF, `Сценарий_9м30с.md`, `Независимая_оценка_и_валидация.md`, `Тема_и_аннотация.md`; preserve their intended audience, explicit organizer rules and actual duration.
- Pin new UT source and latest runnable evidence; if code changed, re-check claim ledger and replay a representative test on that head.
- Choose **one** primary narrative, one reproducible demo, one serious competitor and one falsifiable or explicitly bounded claim.
- For every slide claim create an evidence mapping: exact source path+commit or run ID; claim type `IMPLEMENTED`/`TESTED`/`PROPOSED`/`UNTESTED`; allowed wording; forbidden overclaim.
- Conduct independent 9m30s and 10–12m timed rehearsals for appropriate slots. Only then change deck/script; **no presentation edit is part of stage 2**.

## Four adversarial checks

**LLVM/MLIR specialist:** candidate A must not pretend configuration-aware fuzzing was invented here or that regular LLVM/MLIR tests cannot test passes/diagnostics; advantage over their current testing ecosystems is unproven.
**Researcher:** use no novelty/p-value claim from an author-designed fault corpus without holdouts and fair baselines.
**Systems architect:** candidate C is ordinary but source-backed; an elaborate semantic graph is not necessary to explain it.
**Speaker:** candidate D contains too many missing demonstrations; it is not a suitable “we solved it” story.

**Status after review:** alternatives identified; none selected as final. Follow [stage-3 handoff](#what-stage-3-must-verify-before-editing-any-slide). See [question answers](CRITICAL_QUESTIONS.md) and [findings](SEMANTIC_COMPOSITION_FINDINGS.md).
