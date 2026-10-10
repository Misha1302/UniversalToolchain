# Engineering backlog — proposals activated only by research and authorization

**NOT APPROVED FOR IMPLEMENTATION.** Historical task names S0–S8 are retained, with new gates as activation conditions. A research conclusion, Claude agent output, or P0 inventory label does **not** allow Codex to modify code. All engineering tasks must be independently approved in [RESEARCH_DECISIONS.md](RESEARCH_DECISIONS.md), use a separate branch, capture current SHA, pass relevant [DoD](DEFINITION_OF_DONE.md), and remain reversible. When A (typed C#) wins, C-specific slices are closed without treating that outcome as project failure.

| Historic task / slice | Subsystem owner | Gate required to *propose* engineering | Smallest vertical result | Negative oracle / abort | Delivery DoD | State |
|---|---|---|---|---|---|---|
| S0 baseline characterization | Verification | G00 approval for read-only observation | pin selected HEAD, compiler/SDK, tests; external Acme clean build, actual planner/runtime signatures | baseline drift or tests unavailable => BLOCKED, do not claim PASS | DOC-E8 | PROPOSED |
| S1 semantic schema *spike only* | Semantic contracts | G01 then conditional G02 hypothesis | separate versioned typed Range/Length contract, UT integrated if possible | Unknown/versions/collision never imply safe | DOC-POC, DOC-RESEARCH | PROPOSED |
| S2 real bounds-elimination slice | IR optimizer | successful G02 and G03, then maintainer approval at G09 | three separate outputs, real emitted check, unchanged consumer, RangeV2 | overflow, alias, stale path, negative index preserve check; false-safe => revert | DOC-CORE | PROPOSED |
| S3 provenance/invalidation | Trust / runtime | G03 research result + G09 approval | immutable program/executable context; recheck scoped dependencies | forged Pure/digest/backend/revision cannot authorize; stale shared mutable cache => revert | DOC-CORE | PROPOSED |
| S4 two independent DSLs | Language Authoring SDK | successful G06 research / explicit G09 approval | clean external Rules/Policy + semantically distinct second family | hidden Wist import, incorrect binder diagnostics or invalid backend => revert | DOC-PUBLICAPI, DOC-PRODUCT | PROPOSED |
| S5 shared language services / LSP | Language services | successful G07 research / explicit G09 approval | read-only project of batch binder, minimal diagnostics/hover/completion | stale result overwrites new document or duplicate resolver => revert | DOC-TOOLING | PROPOSED |
| S6 local feasibility | Local optimization | G05 + G03 + G09 approval | same frozen plan, two programs with different legal transforms | reselects packages/providers/routes or mandatory ordering bypass => revert | DOC-CORE | PROPOSED |
| S7 independent alternatives and benchmark | Performance verification | G08 acceptance, baseline G00/G01 | compare manual A, op-interface B, scoped C + complete cost distribution | mismatched workloads or false-safe => invalidate results | DOC-RESEARCH, DOC-E8 | PROPOSED |
| S8 decision/promotion | Architecture review | G09 after relevant research | recorded A/B/C decision, minimal implementation slice and release gates | missing data, no approval, no plan for rollback => remain research-only | DOC-RESEARCH, DOC-RELEASE | PROPOSED |

## Implementation slice contract (required for *each* approved PR)

`slice_id; hypothesis_id; decision_id; exact selected baseline; approved scope/paths; excluded code; one owner; test oracle independent of implementation; negative/mutation fixtures; verification commands validated on baseline; result identity; compatibility; exception/heap/effect/overflow/FP scope; security/trust; performance costs; fallback; rollback commit; reviewer; evidence links`. An actual Codex assignment must cite an `ACCEPTED` ADR and `G09` permission; absence means research-only, no new API. Run the commands only when a working .NET checkout exists; do not claim that this documentation patch ran them.

## Minimum example PR slicing and acceptance

- **S0 PR (docs/tests only):** record actual `git rev-parse HEAD`; `dotnet --info`; `./build.sh --skip-pack` only if environment and prerequisites support it; note missing tools. Identify current `LanguageCompiler`, `LanguagePlan`, `LanguageRuntime`, real AST/Bytecode/AIR/SSA paths, and independent language examples before asserting their behavior.
- **S2 PR (conditional):** one real `Read(a,i)` bounds-check pass with a transparent guard and retained baseline path. Tests must show safe check removal and unsafe check retention; independent oracle must cover overflow, alias/effects and stale facts; old consumer/producers remain byte-for-byte unchanged across RangeV2 update.
- **S4 PR (conditional):** runnable external `.NET` language package restoring only published or explicitly pinned SDK dependencies. Demonstrate grammar/binder/types/source spans and real failures; measure change cost against manual pipeline.
- **S5 PR (conditional):** one editor document state owner; compiler/binder remain authoritative; freeze immutable source versions; cancellation/response reordering negative tests.
- **S6 PR (conditional):** legality evaluated for program-local facts *after* selected `LanguagePlan`; no local capability/provider re-selection; preserve mandated passes and exact backend executor.

## Non-goals / deprecations

No second global planner, mandatory SSA, automatic proof from untrusted provider annotations, broad mutable ontology, feature-specific switches in generic Core, eager source generator, untargeted package-version solver, universal speedup claim, automatic sandbox/security claim, or unrequested production-code migration. PR #370 (DSL-evolution comparison) and #371 (declarative Wist bytecode prototype) remained OPEN and UNMERGED at this audit; their branch results are independent proposals, not master behavior. Historic estimates do not constitute delivery dates.
