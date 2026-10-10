# Ideal UniversalToolchain — research-to-decision dossier

**STATUS: PROPOSAL / RESEARCH, NOT APPROVED IMPLEMENTATION.** The original research dossier and 2026-10-09 inventory remain historical source material. This refactor maps them into decision gates; it does **not** change shipped UT APIs, assert scientific originality or claim execution of E1–E8.

Audited source revisions must not be conflated: original dossier `1d46f17c8dc28f434fa58bdf92f9f8278fa5aaee` (2026-09-20); historic inventory `002a71f0fcdabe7915ed139b297b3829b8060dc2` (2026-10-08); currently connector-observed master `886bf241623e83566585bf2aec5b635fefe1675f` (2026-10-09). Revalidate latest HEAD when applying changes. Current implementation truth: [CURRENT_ARCHITECTURE_STATUS.md](../../../docs/CURRENT_ARCHITECTURE_STATUS.md), [limitations](../../../docs/limitations.md).

## Documentation map — one owner per concern

| Need | Canonical owner |
|---|---|
| What has actually been built and where it stops | [current implementation vs Ideal IUT-01–13](CURRENT_VS_IDEAL.md); current architecture in docs/ |
| Design and authority invariants | [ARCHITECTURE.md](ARCHITECTURE.md) and [proposed API examples](PROPOSED_API_CONTRACTS.md) (not APIs that exist) |
| Historical why and research sources | [ORIGINS.md](ORIGINS.md), [SOURCES.md](SOURCES.md), [RESEARCH_TRACEABILITY.md](RESEARCH_TRACEABILITY.md) |
| Competition/prior art and ideas | [STRATEGY_PRIOR_ART.md](STRATEGY_PRIOR_ART.md), [IDEAS.md](IDEAS.md) |
| Development inventory, 222 + 5 suggestions | [historical inventory](DEVELOPMENT_INVENTORY_2026-10-09.md) — **unchanged** |
| Stable source-item IDs and proposed disposition | [INVENTORY_TRACEABILITY.csv](INVENTORY_TRACEABILITY.csv) — derived, not new approval |
| Measurable outcomes and staged decision gates | [ROADMAP.md](ROADMAP.md) |
| Evidence protocol and competing controls E1–E8 | [EXPERIMENTS.md](EXPERIMENTS.md) |
| One authoritative typed DoD / gate acceptance matrix | [DEFINITION_OF_DONE.md](DEFINITION_OF_DONE.md) |
| Allowed engineering slices S0–S8 | [ENGINEERING_BACKLOG.md](ENGINEERING_BACKLOG.md) |
| Dependency DAG / optional branches | [DEPENDENCIES_AND_CRITICAL_PATH.md](DEPENDENCIES_AND_CRITICAL_PATH.md) |
| Approver-owned ADR and falsifiers | [RESEARCH_DECISIONS.md](RESEARCH_DECISIONS.md) |
| Claude research → maintainer approval → Codex implementation | [RESEARCH_HANDOFF_CONTRACT.md](RESEARCH_HANDOFF_CONTRACT.md) |
| Independent DSL adoption, tooling, product | [LANGUAGE_ENGINEERING_PLATFORM.md](LANGUAGE_ENGINEERING_PLATFORM.md) |
| LangDev slides and claim discipline | [LANGDEV.md](LANGDEV.md) |

**Read order for researchers:** INVENTORY → CURRENT_VS_IDEAL → STRATEGY_PRIOR_ART → EXPERIMENTS → ROADMAP → DoD → ADR/handoff. **For implementers:** current architecture/code/tests → approved ADR+exact gate evidence → BACKLOG slice → DoD → tests → rollback. No roadmap `P0` or model output can stand in for signed implementation authorization.
