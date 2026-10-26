# Independent Q1/Q2 execution audit — 2026-10-27

**Status:** prospective decision and journal planning, NOT proven research finding, journal acceptance, or HSE credit. Source: PR #382 HEAD `ba14e481b14cbf323bfa96f52b469950ab751495`; source dossier [README](../README.md), [RELATED_WORK](../RELATED_WORK.md), [PUBLICATION_MATRIX](../PUBLICATION_MATRIX.md), [RESEARCH_PROTOCOL](../RESEARCH_PROTOCOL.md), and [NEXT_SPEC](../NEXT_SPEC.md). Production code and frozen CGO27 evidence remain untouched.

## Executive decision

**Conditional primary:** typed semantic boundary contracts plus independent DSL maintenance (A+C), with a quantitative comparison against a *competent matched handwritten typed pipeline* using identical predicates/checkers, always-check alternative and independent domain oracle. **Fallback:** PlanFuzz (B) if matched-budget external fault discovery shows distinctive lifecycle/composition value. **Defer:** CPU/GPU portability (D): IR export is not demonstrated hardware parity. LLVM and MLIR already handle preserved-analysis invalidation, so selective reverification alone is not a defensible novel claim. Older four model-designed diagnostic cases yielded 8 direct + 12 matched + 2 scheduler observations, not 22 independent bugs: undeclared invalidation escapes selective checking; incomplete verifier misses even always-check. A full independent external trial has not occurred.

**Administrative hard gate:** `HSE_ELIGIBILITY_NOT_VERIFIED`. Exact HSE programme rule, deadline, publication status, index, ranking year/category, authorship requirements and permissible article types are missing. These cannot be replaced by generic HSE rules.

## Competing hypotheses

| Track | Hardest counterargument | Falsifiable contribution | Cheapest kill/switch gate |
|---|---|---|---|
| A contracts + reverification | LLVM/MLIR already support invalidation; wrong declarations and incomplete checker coverage defeat claims | Correctness and total maintenance cost improve vs identical hand-coded checks across independent change tasks | Pre-freeze six change tasks; UTS, manual typed, always-check and independent oracle; count all costs/outcomes. If no credible benefit or external transfer, pivot |
| B PlanFuzz | Tzer, ConfigFuzz, VAFuzz jointly mutate programs/configurations; superior oracle alone explains more discoveries | Lifecycle and cross-extension oracles reveal independently authored faults at equal search budget and shared oracle | Frozen external targets, matched joint-search baseline, repeated runs and blind defect classification. No distinct discovery means stop |
| C independent DSL evolution | MLIR interfaces already support third-party extensions; LOC ignores migrations/adapters/docs | Full cost of independent feature evolution decreases without regression | Independent maintainer changes in more than one DSL; total-cost inventory |
| D CPU/GPU correctness | MLIR/verification literature substantial; no end-to-end GPU hardware evidence in UT | Preserving an explicit narrow semantics on CPU/GPU hardware | Run tiny defined workload on actual hardware with independent oracle; otherwise defer |

Decision criteria: novelty 25%, independent evidence feasibility 25%, venue fit 15%, engineering effort 15%, time to manuscript 10%, practical value 10%. These are subjective decision weights, NOT acceptance probabilities. HSE rule is a hard veto. Sensitivity: if outside researchers cannot be obtained for maintenance study, B may win with real external faults; if only a near-term acceptance/indexing deadline is allowed, any journal route may be infeasible regardless of quality.

**Nearest work / methodological competitors:** [LLVM](https://llvm.org/docs/NewPassManager.html); [MLIR PM](https://mlir.llvm.org/docs/PassManagement/); [MLIR interfaces](https://mlir.llvm.org/docs/Interfaces/); [First-Class Verification Dialects](https://doi.org/10.1145/3729309); [Alive2](https://doi.org/10.1145/3453483.3454030); [SynthFuzz](https://doi.org/10.1109/ICSE55347.2025.00037); [ConfigFuzz](https://doi.org/10.1145/3580597); [Tzer](https://doi.org/10.1145/3527317); [VAFuzz](https://conf.researchr.org/details/icse-2026/icse-2026-research-track/188/Variability-Aware-Fuzzing); [emergent contract interfaces](https://doi.org/10.1145/2934466.2934471). See RELATED_WORK for study-method details and source access limits.

## Journal candidates (no HSE credit confirmed)

| Journal, ISSNs | Candidate angle and verified-in-dossier ranking | Status |
|---|---|---|
| ACM **TOSEM**, 1049-331X / 1557-7392 | Primary, independent empirical maintenance research; publisher 2025 JIF Q1 (6.9; 8/128) indexed extract; verify JCR category and HSE metric. ACM OA, journal-specific APC / coverage to reconfirm | CONDITIONAL Q1 scientific target; HSE NOT_VERIFIED |
| ACM **TOPLAS**, 0164-0925 / 1558-4593 | Conditional Q2 alternative for original PL mechanism, **not** simply an empirical cost story; publisher 2025 JIF Q2 (2.9; 57/128), 2024 JIF Q3. Verify required year/category | CONDITIONAL, not unconditional Q2 fallback |
| **Empirical Software Engineering**, 1382-3256 / 1573-7616 | High empirical fit; Springer 2025 JIF 3.4 without verified exact quartile; SJR 2025 best Q1 indexed extract, category unclear; hybrid, possible subscription route without APC; prior optional OA £2390/$3390/€2790 before taxes, re-quote | CONDITIONAL |
| **Journal of Systems and Software**, 0164-1212 | Applied SE external evaluation; ranking/year/category, current charges not confirmed | SCREENING |
| **IEEE Transactions on Software Engineering**, 0098-5589 / 1939-3520 | Requires strong multi-system evaluation; ranking and fees to verify | SCREENING |
| **Software Quality Journal**, 0963-9314 / 1573-1367 | If verifiability/quality angle predominates; ranking and fees not verified | SCREENING |

Nearest journal articles documented previously: TOSEM [LOOL](https://doi.org/10.1145/3807508), [Isolating Compiler Faults](https://doi.org/10.1145/3786786); TOPLAS [Denotation-based Compositional Compiler Verification](https://doi.org/10.1145/3797874), [Interactive Abstract Interpretation](https://doi.org/10.1145/3648441), [Gradual C0](https://doi.org/10.1145/3704808); EMSE [SSFuzz](https://doi.org/10.1007/s10664-025-10777-0), [CTDip](https://doi.org/10.1007/s10664-025-10642-0), [Tools and benchmarks evolve](https://doi.org/10.1007/s10664-025-10733-y). Full three-to-five-paper recent screening for each of six journals and full instructions/fees remains an explicit missing evidence gate before journal selection.

Do not mix JIF/JCR with CiteScore or SJR. Publication phases: protocol, submission, desk review, revise/resubmit, acceptance, online publication, indexing, formal HSE credit are **different**. Do not assume early-stage Registered Report or journal-first presentation suffices. Never submit overlapping work simultaneously; confirm CGO27 submission/acceptance receipts, not filenames.

## Pre-registered design

Freeze six meaningful change tasks **before** coding the matched control: actual external changes where possible; clean/declared bad contract/missing invalidation/false preservation/incomplete verifier/independent extension scenarios. This is pilot feasibility, not statistically powerful final sample. U = UT contract automation; M = competent handwritten typed implementation with exactly same verifiers/semantics; A = always-check; O = independent reference oracle, capable of catching wrong outputs in **both** U and M. Predefine correctness endpoint, full development effort (source, adapters, schema, tests, docs, migration, handling, maintenance) and whole-pipeline performance as separate endpoints. Record every assigned trial including incomplete, errors and negatives; do not pretend repeated seeds/trials are independent projects. Stratify by origin, cluster by project/task/participant; report uncertainty. Calibrate required sample and external access after pilot variance, before confirmatory collection. Version schemas, seeds, raw JSONL, compiler and machine identity, environment and hashes, before downstream filtering; do not alter historical CGO27 frozen data.

## Seven-day plan (proposed, not performed)

| Day from 27 Oct | Owner | Concrete deliverable | Decision gate |
|---|---|---|---|
| 1 | research lead | Six-task frozen manifest, HSE eligibility request, exact source/novelty ledger | No fixed tasks -> not a confirmatory study |
| 2 | Codex | Raw-first trial recorder with failure and interrupt retention tests | Raw must persist before scientific verdict |
| 3 | Codex + reviewer | Baseline M checker parity inventory, deterministic adapters | Missing predicates -> invalid comparison |
| 4 | Independent oracle owner | Separate reference evaluator plus shared-wrong-output mutant | Oracle must reject shared miscompilation |
| 5 | researcher | Pilot U/M/A/O task execution with full denominator and cost | Local GO only, not superiority claim |
| 6 | researcher + external maintainer | Authorized case, provenance and participant/permission sheet | No owner => external BLOCKED |
| 7 | critical reviewer | Signed GO/PIVOT/STOP table, pilot variance and journal fit check | No indefinite new framework build |

Days 30/60/90: 30 = full instrumented pilot + validated candidate external case + authorship/venue rules; 60 = external independent changes, repeated validated runs, replication and uncertainty; 90 = full manuscript draft, artifacts, reviewer simulations **if** data support it. Otherwise label as pilot-only. Beyond 90: time for editorial review/revisions/indexing is uncontrolled, so no guaranteed formal deadline. **Critical path:** freeze -> instrumentation+manual/oracle -> external feasibility -> confirmatory sample -> analysis -> paper -> editorial process. HSE administrative verification should happen in parallel.

## Codex iterations / tests / rollback

1. **I-01 raw-first recorder**: non-packable research-only runner, JSONL with version, task, arm, seed, source hashes, environment, raw trial, infrastructure/science statuses; persist completed observation before scientific comparison. Test deliberate mismatch survives and acknowledged records survive SIGKILL (no power-loss guarantee). FAIL if old parity gates are weakened or trial lost. Rollback: remove only new research runner files.
2. **I-02 matched manual typed baseline**: implement M using same verifier/predicates and matched applicability/context; add always-check A. Before coding freeze task names and predicates. Tests: same clean and declared-bad outcomes, negative undeclared-invalidation cases recorded not filtered, source hashes. FAIL if different checks or extra UTS advantages. Rollback isolated adapter.
3. **I-03 oracle+cost**: independent domain model catches equal-but-wrong route output, records integration and migration effort, separately cold/warm compiler end-to-end timings. FAIL if oracle shares treatment implementation or totals exclude failures. Rollback only new harness.

For existing four-case probe, reproducibility commands (not new proof of publication): `dotnet run --project internal-docs/research/heterogeneous-dsl-2026-10-26/journal-decision/probe/DiagnosticProbe.csproj -c Release -p:NuGetAudit=false -- /tmp/ut-probe-replay-new.json` and `python3 internal-docs/research/heterogeneous-dsl-2026-10-26/journal-decision/probe/verify_probe.py --replay /tmp/ut-probe-replay-new.json`. Review true output and current source hashes before recording PASS.

## Cost, cash vs economic (scenario assumptions only)

Define hourly rates `e,r,p,f` for engineering/research/PI/external participants, `C` for equipment/cloud/API, `J` journal charges and `Q` rejection/transfer/re-run costs. **Economic total:** `E*e + R*r + P*p + F*f + C + J + Q`. **Cash (unpaid self time):** paid external time plus actual C/J/Q cash. Quote local rates, journals, waivers, cloud and FX before monetary numeric totals.

| Track | Feasibility minimum | Working research plan | Stress/re-submission |
|---|---|---|---|
| A+C | E=50–90 h, R=70–120 h | E=180–300 h, R=260–420 h | E=400–650 h, R=500–800 h |
| B | E=60–110 h, R=80–130 h | E=220–370 h, R=280–460 h | E=450–750 h, R=550–900 h |

Hours are **planning bounds selected here**, not measured or externally validated forecasts. Recalibrate from day-7 and day-30 time logs. Q1/Q2 label does not dictate APC; publisher/OA route does. ACM journal APC/ACM Open need exact quote, not conference tariffs; hybrid EMSE subscription publication may avoid an optional OA charge. Rejection cost is added research/reformat/re-experiment hours plus any cash; deadline loss might dominate all charges.

## Pre-submission reviewers (adversarial)

PL/compilers: LLVM/MLIR overlap, missing formal semantic model, false declarations, incomplete verifier, weak manual baseline and no external projects. Methodologist: pseudoreplication, cherry-picked tasks, changing RQs, unmatched oracle, participant-learning effects, small independent sample and hidden migration costs. Editor (TOSEM): no generalizable contribution, substantial overlap with CGO27, inadequate venue fit, missing replication package and insufficient independent artifacts. Each is a **major/critical failure gate**, not cosmetic formatting. Preserve negative findings and potential PIVOT/STOP.

## Stage-0 paper pitch (English, no fabricated results)

**Title:** *Do Typed Semantic Boundary Contracts Improve the Maintenance of Extensible Language Pipelines? A Matched Empirical Study.*

**Problem/gap:** independent DSL extensions modify semantic invariants, while analyses need preservation/invalidation. Existing mature toolchains manage invalidation, but the extra maintenance/correctness benefit of UTS-style declarative obligations over equal handwritten typed checks remains unestablished.

**Falsifiable RQs:** RQ1 does U improve independently adjudicated correct task completion versus M? RQ2 does U reduce *total* maintenance/integration time? RQ3 how do incorrect preservation declarations and incomplete checking affect correctness and cost? RQ4 does the result replicate beyond UT?

**Candidate contributions:** explicit model of maintenance obligations; strong matched manual baseline; independent external change tasks; comprehensive raw cost/failure ledger and replication archive. All conditional on successful experiment.

**Design:** freeze independent tasks/protocol, run U/M/A/O arms, independent evaluator, participants/projects as clusters, full denominator with missing trials reported, CIs and threats. **Outline:** introduction, prior work, mechanism, study, results (pending), limitations, replication. **Primary venue:** TOSEM if multi-project data is substantively novel.

**Fallback B title:** *Configuration, Lifecycle and Composition Oracles for Extensible Compiler Frameworks: A Matched-Budget Study*. It needs independently discovered faults beyond what joint program/config fuzzers and equal oracles find.

## Integrity/status

2026-10-27 GitHub connector verified PR #382 as open/draft/not merged, head `ba14e481...`. Fedora device online; its old checkout `/home/micodiy/razakov/sharp/UniversalToolchain` at `e7f71a8f...` has untracked user content. A clean isolated worktree was created `/home/micodiy/razakov/sharp/UniversalToolchain-journal-audit-20261027` from exact PR head. This note is documentation only. No independent journal-scale empirical result, publication, HSE eligibility decision, actual participant agreement, journal-specific current APC invoice, or article acceptance is asserted. **Required private input:** redacted HSE programme provision and deadline; do not commit identifying documents.
