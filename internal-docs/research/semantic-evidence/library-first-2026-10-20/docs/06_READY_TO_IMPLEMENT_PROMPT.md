# 06 — Ready-to-run implementation brief for Codex / engineering agent

**Scope:** implement a **library-first** Level 2 + trusted Level 3 pilot. This brief does **not** authorize any GitHub write, push, branch creation on remote, publication to NuGet or public production API change.

## Source and constraints

- Read this complete revised archive and `docs/00_DECISION.md`, `docs/02_IMPLEMENTATION_ARCHITECTURE.md`, `docs/03_UT_INTEGRATION_ROADMAP.md`, `docs/07_LIBRARY_PACKAGING_AND_API.md`, `docs/08_MIGRATION_AND_ACCEPTANCE.md` before changing source.
- Historical UT audit baseline: `Misha1302/UniversalToolchain master@886bf241623e83566585bf2aec5b635fefe1675f`. Fetch the **actual** current HEAD before editing, note differences and preserve architectural owners. Review `LanguageCompiler -> selected LanguagePlan -> LanguageRuntime`, `LanguageArtifactBuildPipeline`, `SsaRoundtripRoute`, `SsaOptimizerPipeline`, `IrStageContract`, structural SSA verification, selected profile registrations.
- Work locally. Preserve old original P1/P2/Q `prototype/` and `logs/` from `evidence/P1_P2_Q_research_snapshot_2026-10-16.zip` as model/reference and immutable evidence; don't mechanically call its `Contracts` project the new library.

## Mandatory direction-of-dependency contract

```text
SemanticEvidence.Core (facts, session, derivations)  <-- SemanticEvidence.Rules.Numeric
                 ^                                             ^
                 |                                             |
                 +-------------- UniversalToolchain.SemanticEvidence (host adapter)
                                        |
                         UT owns selected plan and Rewrite Gate
```

- Core and Numeric compile **without references to any UniversalToolchain assembly, package or namespace**, without SSA/IR/Wist/`LanguagePlan` types and without a UT rewrite/commit API.
- Host constructs opaque `ContextKey`, `RevisionKey`, `ScopeKey`, `SubjectKey`; source authority is approved by real selected-plan registrations and supplied to library through a restricted admission protocol. Strings and plugin metadata alone are not trusted.
- L2 = authorized root facts. L3 = typed derived facts produced only by selected **trusted C# rule implementations**, with parent links, validity and invalidation. Do not implement runtime universal proof checker, a rule DSL, SMT, serialized arbitrary proof DAGs or a duplicate plugin planner.
- Rules: same-subject intersection, affine checked Int32 range, bound implication as fact; do not treat a fact as an arbitrary rewrite permit.
- **UT adapter ONLY** maps SSA values/scope, validates closed pure comparison rewrite, handles candidate isolation, re-checks exact selected-world+revision+root authority and atomically commits via the real IR owner. The generic library never publishes IR.
- Legacy selected optimizers unchanged by default, and not advertised as proof-gated. Feature off by default. Unsupported=>deny/skip, not silently verified.

## Execute in THIS order

1. **LF-00**: create portable Core `.csproj` and forbidden-dependency/API scan. Compile alone.
2. **LF-01**: port inference/DAG to portable contexts, split Numeric rules, negative tests. Old model remains untouched.
3. **LF-02**: independent second non-UT host built from same library binaries; P1/Q built before P2, Q byte-identical after; demonstrate actual distinct subject model, no UT types or central P2 switch.
4. **UT-L3-00/01**: inspect real selected UT source and create thin adapter + selected-plan authority and immutable SSA snapshots.
5. **UT-L3-02**: implement only one closed host-owned Rewrite Gate for pure signed Int32 comparison; candidate/commit split, atomic selected-owner publication, rollback.
6. **UT-L3-03/04**: real N+1/source-to-backend differential, negative bypass audit, honest direct typed C# baseline, p50/p95/memory/CI comparison. Do not publish package until acceptance.

## Non-negotiable negative tests

Wrong subject/scope/type/revision; same printed SSA name from different owner; checked overflow; conflicting ranges; stale/root revoked; foreign session; unselected/spoofed root provider; two competing commits; exception during inference/proposal; structurally valid but wrong rewrite; malformed SSA; changed policy/plan; direct guarded route bypass; source-to-backend return and exception behavior; P2 without Q recompilation; non-UT second host.

## Stop / deliverables

Stop and report NO-GO if Core cannot remain independent, a root authority cannot be bound to real selected implementation, a guarded candidate can bypass host commit, incorrect rewrite is permitted, or direct-C# baseline dominates without offsetting maintenance benefit. Never fabricate executed results or call a toy model a production-UT proof.

Deliver: minimal local diff/patch, exact source baseline, dependency/API diagram, independently buildable Core/Rules projects, second host, UT adapter code if safe, test files with PASS/FAIL/NOT_RUN, raw logs/checksums, reproducible scripts, honest comparison, and GO/NO-GO recommendation. No GitHub writes without separate user instruction.
