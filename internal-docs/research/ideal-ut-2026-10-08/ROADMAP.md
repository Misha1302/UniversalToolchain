# Ideal UT — falsifiable research → engineering → product → release roadmap

> **PROPOSAL, NOT APPROVED IMPLEMENTATION.** Prepared 2026-10-09 against connector-observed `master@886bf241623e83566585bf2aec5b635fefe1675f`; historical inventory baseline `002a71f0fcdabe7915ed139b297b3829b8060dc2`. No new tests or UT builds were performed by this documentation rewrite. Research PASS is not implementation approval.

## Outcome, not mechanism

The goal is independent non-Wist authoring, reliable extension of semantic knowledge without editing old producers/consumers, safe optimization decisions, useful reusable tooling, efficient execution and reproducible external adoption. See [Definition of Done](DEFINITION_OF_DONE.md), [experiments](EXPERIMENTS.md), [backlog](ENGINEERING_BACKLOG.md), [traceability](INVENTORY_TRACEABILITY.csv), [research decisions](RESEARCH_DECISIONS.md), [DAG](DEPENDENCIES_AND_CRITICAL_PATH.md) and [handoff](RESEARCH_HANDOFF_CONTRACT.md).

**Candidate competition:** A direct typed interfaces/explicit adapters; B MLIR-style typed operation interfaces + explicit conversions; C scoped facts/provenance/obligations. Do not adopt C merely because it works: it must solve held-out N+1 cases materially better than A/B under comparable total cost, accuracy and trust. Alternative A winning is a successful research outcome. Existing `LanguageCompiler` is sole global composition authority. `LanguagePlan` describes immutable selected configuration, not a correctness certificate; `LanguageRuntime` materializes that selection. SSA is optional, never a universal prerequisite.

## Decisions and phase transitions

- **R0 Baseline:** G00 → G01. Without independent, mutation-sensitive oracle, no correctness-sensitive prototype can pass.
- **R1 Competing research:** G02 (E1) → G03 (E7) → optional G04 (E2) → G05 (E3). If A/B wins G02, semantic-service track terminates; retain simpler typed SDK and continue product track.
- **P1 Independent author adoption (parallel):** G06 (E4) → G07 (E5). This does not depend on adopting C.
- **P2 Measurements:** G08 (E6) after relevant bounded program or DSL slice. Set target workload/decision margin BEFORE observing result.
- **E1 Narrow implementation:** G09 conditional on applicable research and product gates, approved ADR, exact reversible slice; research findings alone do not authorize Codex changes.
- **L1 Release maintenance:** G10 only for an integrated and independently reproduced candidate; it does not imply NuGet publication.

## North-star outcomes and bounded demonstrators

All baseline costs are **UNMEASURED** unless explicitly cited as existing behavior. Each row tests a user-visible outcome rather than asserting a subsystem must exist. Failures can lead to retaining the current hand-built SDK.

| User / outcome | Minimum independent demonstration | Metric | Known baseline / comparator | Deal-breaker | Gate and evidence grade |
|---|---|---|---|---|---|
| External `.NET` language author: build one language without Wist | fresh non-Wist Rules DSL with grammar/binder/types/diagnostics and executable backend | setup steps, changed LOC, clean restore/run, correction time | Current low-level LanguageAuthoring and manual C#; measured baseline **not yet recorded** | hidden Wist import, cannot build separately | G06; DOC on existing SDK, HYPOTHESIS on ergonomic improvements |
| Independent analysis provider author: add N+1 knowledge | Range, Shape, unchanged Bounds consumer + independent RangeV2 | prior-package code edits=0; precision delta, false-safe=0; total cost | A typed interfaces/manual adapters and B typed operations; unmeasured | any false-safe or A/B equally effective with lower cost | G02; HYPOTHESIS |
| Optimizer author: transformation only with valid obligations | real bounds-check emits/removes on positive/negative paths | false-safe=0 across seeded mutants | current IR pass + independent reference; no E1-integrated result | Unknown or Contradiction treated as safe | G01–G03; HYPOTHESIS |
| IR author: transfer fact across real 1:N lowering safely | checked mapping witness AST→AIR/optional SSA | accepted safe correspondence, 0 invalid transfers | re-analyze at destination vs source-map-only negative | trap/effect/overflow difference missed | G04; HYPOTHESIS |
| Runtime integrator: choose local legal work under fixed global config | same LanguagePlan for distinct program snapshots | hash identity fixed, legal transforms differ, no runtime reselection | static legal route and current CIL/interpreter; unmeasured | second global planner, illegal cheaper path | G05; HYPOTHESIS |
| Editor user: diagnostics/hover without version races | edit valid→invalid→valid, stale responses shuffled | stale overwrite=0, batch/editor symbol parity | existing compiler diagnostics and full reparsing | second binder or stale result commits | G07; HYPOTHESIS |
| Host integrator/release maintainer: reproducible, usable hot path | clean consumer and exact candidate pack, runtime cost curve | planning/compile/first/steady/allocation/amortization, SHA replay | handwritten pipeline, interpreter and current compiled delegate; unmeasured | unreplayable speed/security claims, incompatible older consumer | G08/G10; HYPOTHESIS |

## Gate contracts (single-owner, NOT_RUN until recorded evidence)

### G00 — Freeze actual HEAD, selected commits, test surface, current vs proposal and independent control fixtures

- **ID:** G00
- **Type:** RESEARCH
- **Owner:** Evidence & verification
- **Experiments:** E8
- **Prerequisites:** none
- **Scope:** Freeze actual HEAD, selected commits, test surface, current vs proposal and independent control fixtures
- **Excluded scope:** Production changes, semantic proof claims
- **Exact evidence:** dated source audit, exact commit/tree, SDK/OS/tool receipts, PR merge states
- **Entry criteria:** readable current sources and current selected-world identity
- **Deliverables:** Baseline manifest, link/command inventory, 2 independent negative-oracle specifications
- **PASS:** All required identities explicit and replay recipe reviewable
- **FAIL:** identity missing or baseline confused with historic inventory
- **UNKNOWN/stop:** No comparable provenance = UNKNOWN; stop data-dependent decisions
- **Verification:** read-only source inspection + git rev-parse HEAD; dotnet --info when available
- **Trust/security boundary:** No executable trust inferred from plan hash; do not copy secrets
- **DoD reference:** DOC-RESEARCH, DOC-E8
- **Rollback/abort:** Discard stale receipts; refreeze at new HEAD
- **Follow-on:** G01, G06
- **Source anchors:** DEVELOPMENT_INVENTORY_2026-10-09.md; docs/CURRENT_ARCHITECTURE_STATUS.md

### G01 — Independent oracle and negative/mutation corpus on actual UT routes

- **ID:** G01
- **Type:** RESEARCH
- **Owner:** Semantic Verification
- **Experiments:** E8
- **Prerequisites:** G00
- **Scope:** Independent oracle and negative/mutation corpus on actual UT routes
- **Excluded scope:** Any new compiler optimization
- **Exact evidence:** oracle source, input/output traces, typed observational comparison, seed, counterexamples
- **Entry criteria:** Frozen baseline; independent oracle not sharing transform implementation
- **Deliverables:** Value/error/trap/effect/overflow reference, mutation fixtures and measured baseline
- **PASS:** Independent oracle detects seeded checked-overflow AND effect-order mutants; fixed seeds rerun
- **FAIL:** Both backends agree only, or seeded counterexample survives
- **UNKNOWN/stop:** Abort dependent legality experiments if oracle nondiscriminating
- **Verification:** reference interpreter or external specification vs candidate, replay seed and inject two mutants
- **Trust/security boundary:** Oracle cannot be generated by proposed transformer alone
- **DoD reference:** DOC-RESEARCH, DOC-CORE
- **Rollback/abort:** Return to oracle design on undetected mutation
- **Follow-on:** G02, G03, G04, G06
- **Source anchors:** EXPERIMENTS.md; docs/limitations.md

### G02 — N+1 Range/Shape/Bounds independent package hypothesis vs typed interfaces and op interfaces

- **ID:** G02
- **Type:** RESEARCH
- **Owner:** Semantics/LanguageSdk
- **Experiments:** E1
- **Prerequisites:** G00,G01
- **Scope:** N+1 Range/Shape/Bounds independent package hypothesis vs typed interfaces and op interfaces
- **Excluded scope:** Shipped fact store or public API
- **Exact evidence:** all separate build outputs; old package SHA equality; precision/correctness/cost comparison
- **Entry criteria:** Frozen triple-control corpus + agreed semantic observations
- **Deliverables:** A/B/C runnable controlled slice, zero-old-code edits proof, held-out examples and failure logs
- **PASS:** 0 false-safe check removals; prior producer+consumer edit LOC=0; safe held-out precision delta >0; C must beat A/B on material total costs to adopt C
- **FAIL:** Any false-safe, hidden implementation coupling, no precision benefit, or simpler equal-quality A
- **UNKNOWN/stop:** UNKNOWN/BLOCKED when selected baseline, negative oracle, sample data or independent verification is unavailable
- **Verification:** Three independent packages and real check emitted/not emitted; independent oracle A/B/C same inputs
- **Trust/security boundary:** Unknown/Contradiction never authorize; trust proof separate G03
- **DoD reference:** DOC-RESEARCH, DOC-CORE
- **Rollback/abort:** Drop experimental adapter without changing production; choose A/B/DEFER
- **Follow-on:** G03, G04, G05, G09 or product-only G06
- **Source anchors:** EXPERIMENTS.md; PROPOSED_API_CONTRACTS.md

### G03 — Adversarial evidence, invalidation and executable identity in bounded slice

- **ID:** G03
- **Type:** RESEARCH
- **Owner:** Trust & exact identity
- **Experiments:** E7
- **Prerequisites:** G01,G02
- **Scope:** Adversarial evidence, invalidation and executable identity in bounded slice
- **Excluded scope:** Sandbox, general plugin security guarantees
- **Exact evidence:** digest mismatches, stale/path/backends, forged Pure, contradictions, mutation logs
- **Entry criteria:** Chosen experimental evidence shape from G02; hostile input corpus
- **Deliverables:** Trust-policy provenance; all falsifiers trigger nonoptimization and exact failure reason
- **PASS:** 0 false-safe transforms for forged Pure/digest/ProgramSnapshot/backend/path/document mutation; negative oracle agrees
- **FAIL:** Single malicious claim authorizes transform or stale evidence survives
- **UNKNOWN/stop:** UNKNOWN/BLOCKED when selected baseline, negative oracle, sample data or independent verification is unavailable
- **Verification:** Replay corruption matrix; compare results to independent execution and source digest
- **Trust/security boundary:** Provider self-assertion is NOT verification; runtime requires OS sandbox separately
- **DoD reference:** DOC-RESEARCH, DOC-CORE
- **Rollback/abort:** Disable semantic trust path and fall back to original execution
- **Follow-on:** G04, G05, G09
- **Source anchors:** EXPERIMENTS.md; docs/limitations.md

### G04 — 1:N real AST↔AIR optional SSA evidence mapping, scoped preservation witnesses

- **ID:** G04
- **Type:** RESEARCH
- **Owner:** IR/SSA correctness
- **Experiments:** E2
- **Prerequisites:** G01,G03
- **Scope:** 1:N real AST↔AIR optional SSA evidence mapping, scoped preservation witnesses
- **Excluded scope:** Mandatory SSA or source-map-equals-proof
- **Exact evidence:** real mapped nodes + mismatch corpus + independent observer
- **Entry criteria:** G03 truth boundary and verified baseline oracle
- **Deliverables:** Mapping witness and invalidation contract exercised in UT, including 1:N
- **PASS:** Positive correspondence under declared observations; checked overflow/effects/alias/exception/heap mutant mapping rejected
- **FAIL:** Source span treated as semantic proof or mismatch allows rewrite
- **UNKNOWN/stop:** UNKNOWN/BLOCKED when selected baseline, negative oracle, sample data or independent verification is unavailable
- **Verification:** Independent reference executes before/after under same observations; tamper mapping
- **Trust/security boundary:** Scope/preservation obligation explicit, Unknown blocks transformation
- **DoD reference:** DOC-RESEARCH, DOC-CORE
- **Rollback/abort:** Remove mapping bridge, recompute or prohibit fact transfer
- **Follow-on:** G05, G09 or representation-specific alternative
- **Source anchors:** ARCHITECTURE.md; EXPERIMENTS.md

### G05 — Same frozen LanguagePlan, different program-local legal transformations; legality before cost

- **ID:** G05
- **Type:** RESEARCH
- **Owner:** Local optimization legality
- **Experiments:** E3
- **Prerequisites:** G01,G03; G04 only for cross-IR facts
- **Scope:** Same frozen LanguagePlan, different program-local legal transformations; legality before cost
- **Excluded scope:** Second global planner; new package/provider selection
- **Exact evidence:** same selected plan hash + execution identities + optimization traces for two programs
- **Entry criteria:** Program-specific evidence and independent oracle
- **Deliverables:** Independent local feasibility slice and report of passes and intact mandatory ordering
- **PASS:** Distinct legal choice in two programs with identical frozen plan; invalid obligations keep original; 0 missed required safety barriers
- **FAIL:** Replans package providers or routes; cheapest illegal transform wins
- **UNKNOWN/stop:** UNKNOWN/BLOCKED when selected baseline, negative oracle, sample data or independent verification is unavailable
- **Verification:** Freeze plan bytes, feed two programs, capture route and emitted IR, inspect negative
- **Trust/security boundary:** LanguagePlan not certificate of arbitrary optimization legality
- **DoD reference:** DOC-RESEARCH, DOC-CORE
- **Rollback/abort:** Disable local selection; use static legal routes
- **Follow-on:** G08, G09
- **Source anchors:** ARCHITECTURE.md; EXPERIMENTS.md

### G06 — Independent non-Wist DSL with real grammar, binder, types, diagnostics and execution

- **ID:** G06
- **Type:** PRODUCT
- **Owner:** Language Authoring SDK
- **Experiments:** E4
- **Prerequisites:** G00,G01
- **Scope:** Independent non-Wist DSL with real grammar, binder, types, diagnostics and execution
- **Excluded scope:** Rebranding Wist example; generic parser workbench promise
- **Exact evidence:** external package source, restore/build logs, clean-room user session
- **Entry criteria:** Public SDK on exact pinned commit; reference hand-built pipeline control
- **Deliverables:** Two independent consumer builds + authored DSL and user workflow observations
- **PASS:** Non-Wist source→diagnostics→execution on clean project; target sample documented and independently reproducible
- **FAIL:** Hidden Wist dependency, inability to run outside monorepo, unsafe language restrictions
- **UNKNOWN/stop:** UNKNOWN/BLOCKED when selected baseline, negative oracle, sample data or independent verification is unavailable
- **Verification:** Fresh external project restore/run plus semantic negative tests; compare manual C#
- **Trust/security boundary:** No automatic sandbox claims; allowed host surface explicit
- **DoD reference:** DOC-PUBLICAPI, DOC-PRODUCT
- **Rollback/abort:** Retain manual SDK and abandon facade if no material ergonomics
- **Follow-on:** G07, G08, G09
- **Source anchors:** LANGUAGE_ENGINEERING_PLATFORM.md; docs/language-authoring/

### G07 — Shared binder-derived diagnostics + read-only LSP/CLI/tooling adapter

- **ID:** G07
- **Type:** PRODUCT
- **Owner:** Language services/Tooling
- **Experiments:** E5
- **Prerequisites:** G06
- **Scope:** Shared binder-derived diagnostics + read-only LSP/CLI/tooling adapter
- **Excluded scope:** Second binder, agent authority or autonomous edits
- **Exact evidence:** versioned DocumentSnapshot, stale response test, true source spans
- **Entry criteria:** Runnable independent DSL from G06
- **Deliverables:** Editor valid→invalid→valid protocol trace, comparison to full re-analysis
- **PASS:** Stale version rejected 100% of adversarial runs; same compiler symbols/diagnostics in batch/editor
- **FAIL:** Old diagnostics overwrite latest state or agent bypasses authorization
- **UNKNOWN/stop:** UNKNOWN/BLOCKED when selected baseline, negative oracle, sample data or independent verification is unavailable
- **Verification:** Race test with reordered asynchronous results; independently inspect batch symbols
- **Trust/security boundary:** MCP/LLM may propose, not certify or perform edits without approval
- **DoD reference:** DOC-TOOLING, DOC-PRODUCT
- **Rollback/abort:** Turn off adapter; keep batch compiler authority
- **Follow-on:** G08, G09
- **Source anchors:** LANGUAGE_ENGINEERING_PLATFORM.md; docs/limitations.md

### G08 — Fair end-to-end and steady-state comparison; amortization and overhead

- **ID:** G08
- **Type:** RESEARCH
- **Owner:** Performance verification
- **Experiments:** E6
- **Prerequisites:** G00,G01 and selected G05 or G06
- **Scope:** Fair end-to-end and steady-state comparison; amortization and overhead
- **Excluded scope:** Universal zero-cost or speedup claim
- **Exact evidence:** raw latency/alloc/memory/throughput distributions with same semantics
- **Entry criteria:** Predeclared workloads, variants, warmups, baseline and decision margin
- **Deliverables:** Cost report with uncertainty and per-stage units; rerun variance logged; winning claim predeclared before sample
- **PASS:** Same workloads/semantics and all stage costs measured with raw distributions; preregistered amortization criterion satisfied or faithfully reported as negative
- **FAIL:** Mismatched semantics/config, cherry-picked steady-only numbers
- **UNKNOWN/stop:** UNKNOWN/BLOCKED when selected baseline, negative oracle, sample data or independent verification is unavailable
- **Verification:** Benchmarks repeat planning/materialize/compile/first/steady and compare A/B/C/manual
- **Trust/security boundary:** No optimization may bypass legality for speed
- **DoD reference:** DOC-RESEARCH, DOC-E8
- **Rollback/abort:** Reject specializer if net cost not recovered at target call counts
- **Follow-on:** G09 or defer
- **Source anchors:** EXPERIMENTS.md; docs/architecture/future-work.md

### G09 — One smallest approved vertical slice on current baseline; separate public API review

- **ID:** G09
- **Type:** ENGINEERING
- **Owner:** Core integration review
- **Experiments:** E1,E2,E3,E4,E5,E6,E7
- **Prerequisites:** Relevant successful G02+G03+G05 for semantics OR G06+G07 for product; G08 if perf claim
- **Scope:** One smallest approved vertical slice on current baseline; separate public API review
- **Excluded scope:** Refactor all Wist, global type ontology, broad release
- **Exact evidence:** approved ADR, pinned slice, exact negative tests, clean external restore/build
- **Entry criteria:** Decision signed by designated maintainer; research evidence reviewed; branch not master
- **Deliverables:** Implementer handoff, test receipts and diff limited to approved scope
- **PASS:** all slice DoD met and independent regression/parity unchanged; no forbidden dependency
- **FAIL:** Unauthorized implementation, invalid oracle, broken old consumer or no rollback
- **UNKNOWN/stop:** UNKNOWN/BLOCKED when selected baseline, negative oracle, sample data or independent verification is unavailable
- **Verification:** Focused tests + full relevant suite + git diff --check + external integration
- **Trust/security boundary:** No second planner, mandatory SSA, AI Proven or sandbox inference
- **DoD reference:** DOC-CORE, DOC-PUBLICAPI
- **Rollback/abort:** Revert experimental slice; keep research receipts
- **Follow-on:** G10 or research REVISIT
- **Source anchors:** ENGINEERING_BACKLOG.md; RESEARCH_HANDOFF_CONTRACT.md

### G10 — Release candidate evidence for exact SHA and package identity

- **ID:** G10
- **Type:** RELEASE
- **Owner:** Release & package maintenance
- **Experiments:** E8
- **Prerequisites:** G09; G06 for SDK authoring release
- **Scope:** Release candidate evidence for exact SHA and package identity
- **Excluded scope:** Automatic NuGet publication; stable 1.0 assumption
- **Exact evidence:** CI/workflow URLs on exact commit, compatibility/consumer matrix, reproducible pack hashes
- **Entry criteria:** Approval for distribution, release checklist, tested rollback artifact
- **Deliverables:** Candidate signoff, migration table, release notes, package smoke and rollback drill
- **PASS:** Linux+Windows and pinned external consumer verified; exact package contents reproducible under declared environment
- **FAIL:** Any unchecked breaking change, stale CI, untested package or rollback
- **UNKNOWN/stop:** UNKNOWN/BLOCKED when selected baseline, negative oracle, sample data or independent verification is unavailable
- **Verification:** ./build.sh --skip-pack for characterization; package-specific canonical release steps with real prerequisites
- **Trust/security boundary:** No implicit sandbox or arbitrary-code safety guarantee
- **DoD reference:** DOC-RELEASE, DOC-E8
- **Rollback/abort:** Stop publish; restore previous package and documented migration path
- **Follow-on:** Maintain/re-evaluate periodically
- **Source anchors:** RELEASE_CHECKLIST.md; docs/CURRENT_ARCHITECTURE_STATUS.md

## Historical M0–M6 crosswalk

| Original milestone | Current gates | Condition / branch |
|---|---|---|
| M0 plan identity/read-only | G00, G07 | Read-only plan projection; no second planner |
| M1 independent oracle | G01 | Hard prerequisite for correctness claims |
| M2 N+1 evidence slice | G02, G03 | Reject if A/B is materially better |
| M3 cross-IR | G04 | Only after E1/E7 bounded need; optional SSA |
| M4 tooling/product | G06, G07 | Independent even if semantic service rejected |
| M5 route specialization | G05, G08, G09 | Legality first, payback measured before integrating |
| M6 bounded inference/indices | **DEFER** until G02–G08 expose >=2 measured independent transitive cases | Standalone solver is not justified |

## IUT-01–13 traceability crosswalk

The IUT status labels remain historical and revision-bound in [CURRENT_VS_IDEAL.md](CURRENT_VS_IDEAL.md); this table maps obligations, **not** reassigned implementation statuses.

| Historical requirement | Required gates / evidence |
|---|---|
| IUT-01 Open-world extensibility | G02 N+1, G06 independent author |
| IUT-02 Global config + local feasibility | G00 selected authority, G05 local plan |
| IUT-03 Semantic interoperability | G02 E1 |
| IUT-04 Evidence-bearing judgments | G02/G03 E1+E7 |
| IUT-05 Representation neutrality | G04 E2 |
| IUT-06 Evidence lifecycle | G03 E7 |
| IUT-07 Correctness-first decisions | G01/G05 E3 |
| IUT-08 No pairwise glue | G02 E1 separate binaries |
| IUT-09 Specialization | G08 E6, conditional G09 |
| IUT-10 Minimal vocabulary | G02; otherwise typed interfaces A |
| IUT-11 Trust/security | G03 E7; sandbox not implied |
| IUT-12 Coherent cross-layer contract | G02/G04/G05/G09 bounded slice |
| IUT-13 Stable ownership | G00/G09 no second planner |

## Capability clusters (conditional dispositions, not 222 commitments)

| Cluster | Coverage | Working disposition | Promoted only by |
|---|---|---|---|
| C01 planner identity | composition/selected world | ADOPT existing / characterize | G00 |
| C02 semantic N+1 | types, facts, proofs | RESEARCH (A/B/C) | G02 |
| C03 trust & invalidation | provenance, version scopes | RESEARCH | G03 |
| C04 IR & optimization | AST/AIR/SSA obligations | DEFER cross-IR until evidence | G04, G05 |
| C05 language authoring | grammar, binder, independent packages | PROTOTYPE product | G06 |
| C06 shared language services | docs/LSP/CLI/tool adapters | RESEARCH/PROTOTYPE | G07 |
| C07 execution cost | CIL/interpreter/perf | ADOPT current paths / MEASURE options | G08 |
| C08 independent oracles | negative/mutation/PlanFuzz | ADOPT characterization; extend | G01 |
| C09 SDK releases/ecosystem | compatibility, migration, adoption | DEFER release | G09, G10 |
| C10 speculative extensions | e-graphs, bounded logic, MCP, UI, SIMD | DEFER; selective REJECT for duplicate planner, mandatory SSA | new measured case + ADR |

## Rolling review

Every gate review records reviewed HEAD, completed experiment run IDs, no-go cases, compare-to-A/B result, owner decision and changed mapping in the traceability CSV; do not mutate historical inventory. `NOT_RUN` is not `PASS`. A `FAIL` may eliminate an optional architecture with no product failure. A new HEAD invalidates stale unrerun evidence as necessary. Actual accepted implementation always requires separate maintainer approval, explicitly recorded in [research decisions](RESEARCH_DECISIONS.md).
