# Ideal UniversalToolchain — research dossier (2026-10-08)

**STATUS: PROPOSAL / RESEARCH. No current UT APIs or behavior are changed by these documents.**

Audit revision: [UniversalToolchain@1d46f17](https://github.com/Misha1302/UniversalToolchain/tree/1d46f17c8dc28f434fa58bdf92f9f8278fa5aaee), 2026-09-20. LangDev deck: [18-slide production HTML](https://github.com/Misha1302/lang-dev-presentation-2026/blob/main/index.html), studied on 2026-10-08.

## Thesis

Current UT answers **which independent language packages, contributions, capabilities, artifact paths and runtime implementations form one deterministic language**. Ideal UT asks the harder question: **can independently authored language components also compose semantic knowledge, make transformations legally conditional on that knowledge, revoke it across revisions/representations, and still produce concrete efficient execution?**

The proposed *semantic evidence* service must not be a second language planner, mandatory SSA, a claim of formal universal correctness, a sandbox, or an agent-owned authority. It may be rejected entirely if the strongest alternative—ordinary typed C# interfaces and explicit adapters—solves the held-out cases more simply.

## Independent 2026-10-12 review

This folder is the **single canonical Ideal UT research entrypoint**, not a second architecture authority. [Research governance](RESEARCH_GOVERNANCE.md) names the existing document owners for current implementation, hypotheses, inventory, evidence, experiments and roadmap; new dated reviews do **not** create parallel statuses.

| New incremental review | Purpose |
| --- | --- |
| [Independent architecture review](REVIEW_2026-10-12.md) | 2026-10-12 HEAD audit, three genuine architecture alternatives, eight decisions, conditional chosen direction |
| [Original note annotated](ANNOTATED_NOTE_2026-10-12.md) | All fifteen representation questions separately, ambiguities preserved, hypotheses and rejection controls |
| [Prior-art comparison delta](PRIOR_ART_DELTA_2026-10-12.md) | Papers and mature implementations, concrete competitor mechanisms and adaptation limits |
| [Falsification addendum](FALSIFICATION_ADDENDUM_2026-10-12.md) | E1–E8, M0–M6, S0–S8 independent oracles, positive/negative/holdout cases and kill criteria |
| [Research governance](RESEARCH_GOVERNANCE.md) | Single-owner knowledge base, statuses, traceability, inventory preservation and maintenance workflow |
| [222-item inventory audit](INVENTORY_AUDIT_2026-10-12.md) | Actual 15-domain counts, proposed priority lens, overlap groups and one verified documentation drift correction |

**Current head considered:** `master@886bf241623e83566585bf2aec5b635fefe1675f` (observed 2026-10-12). The prior dossier's 2026-09-20 audit and the inventory's 2026-10-08 audit are retained as **historical pinned baselines**, not overwritten. PRs [#370](https://github.com/Misha1302/UniversalToolchain/pull/370) and [#371](https://github.com/Misha1302/UniversalToolchain/pull/371) were open and unmerged when reviewed. All newly added research files are documentation only: no new UT runtime API, integrated E1–E8 test or formal proof is claimed.

## 2026-10-12 executable trust-boundary cycle (stacked research PR)

The [frozen experimental protocol](SSA_TRUST_PREREG_2026-10-12.md) and its [decision memo](SSA_TRUST_DECISION_2026-10-12.md) test a **real UT SSA dead-call elimination path** against a CLR side-effect oracle and direct host-approval alternative. This does not complete E1, prove hostile-package runtime reachability or introduce a production semantic-evidence API. Documentation of necessary exact-count CI reconciliation is retained in [amendment A1](SSA_TRUST_PROTOCOL_AMENDMENT_2026-10-12.md) and [amendment A2](SSA_TRUST_DOC_SYNC_2026-10-12.md). The experiment is in a separate branch stacked on research PR #376, and its results must be interpreted at the specific CI commit.

## Documentation map

| Purpose | Document |
| --- | --- |
| Historical roots and research questions | [ORIGINS.md](ORIGINS.md) |
| Current vs proposed; thirteen IUT gap requirements | [CURRENT_VS_IDEAL.md](CURRENT_VS_IDEAL.md) |
| Target owner boundaries, scopes, evidence and legality | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Candidate ideas, alternatives, non-goals and decisions | [IDEAS.md](IDEAS.md) |
| Sequenced roadmap with completion and rollback gates | [ROADMAP.md](ROADMAP.md) |
| Falsification and comparison protocol | [EXPERIMENTS.md](EXPERIMENTS.md) |
| Real LangDev narrative, verified claims vs outlook | [LANGDEV.md](LANGDEV.md) |
| Source lineage, links, uncertainty, reproducibility | [SOURCES.md](SOURCES.md) |
| Source packages → all 14 deliverables, 20 DoD and AI hypothesis provenance | [RESEARCH_TRACEABILITY.md](RESEARCH_TRACEABILITY.md) |
| Strategic options, original 18 competitive dimensions, strongest alternatives | [STRATEGY_PRIOR_ART.md](STRATEGY_PRIOR_ART.md) |
| Proposed C# semantic contracts and rule-of-use caveats | [PROPOSED_API_CONTRACTS.md](PROPOSED_API_CONTRACTS.md) |
| Independent Language Engineering Platform: LSP, debugging, author/end-user journeys | [LANGUAGE_ENGINEERING_PLATFORM.md](LANGUAGE_ENGINEERING_PLATFORM.md) |
| Task-ready S0–S8 reversible implementation backlog | [ENGINEERING_BACKLOG.md](ENGINEERING_BACKLOG.md) |

## Supplemental development inventory (2026-10-09)

[Full UniversalToolchain → Ideal UT development inventory](DEVELOPMENT_INVENTORY_2026-10-09.md) consolidates **15 domains and 222 candidate development items** with priority/status labels, motivations, technical caveats, dependencies, non-goals and source links. It also separates five additional recommendations from the source-backed inventory.

This is a **supplement to, not one of, the original 14 research deliverables**. It is a revision-bound planning reference in Russian, not an approved release roadmap, evidence of current APIs, or a claim that every item should be built. For an English summary oriented to new readers, see [Future direction](../../../readme.md#future-direction-universaltoolchain-beyond-wist).

## Source of truth

Actual code and tests > [current architecture](../../../docs/CURRENT_ARCHITECTURE_STATUS.md) > revision-bound experimental records > these proposals > historical recollection. This research folder must not be used to claim implementation or release readiness. Future work is activated only by held-out experiments or measured developer needs.
