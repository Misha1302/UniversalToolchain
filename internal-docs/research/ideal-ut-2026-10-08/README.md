# Ideal UniversalToolchain — research dossier (2026-10-08)

**STATUS: PROPOSAL / RESEARCH. No current UT APIs or behavior are changed by these documents.**

Audit revision: [UniversalToolchain@1d46f17](https://github.com/Misha1302/UniversalToolchain/tree/1d46f17c8dc28f434fa58bdf92f9f8278fa5aaee), 2026-09-20. LangDev deck: [18-slide production HTML](https://github.com/Misha1302/lang-dev-presentation-2026/blob/main/index.html), studied on 2026-10-08.

## Thesis

Current UT answers **which independent language packages, contributions, capabilities, artifact paths and runtime implementations form one deterministic language**. Ideal UT asks the harder question: **can independently authored language components also compose semantic knowledge, make transformations legally conditional on that knowledge, revoke it across revisions/representations, and still produce concrete efficient execution?**

The proposed *semantic evidence* service must not be a second language planner, mandatory SSA, a claim of formal universal correctness, a sandbox, or an agent-owned authority. It may be rejected entirely if the strongest alternative—ordinary typed C# interfaces and explicit adapters—solves the held-out cases more simply.

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

## Source of truth

Actual code and tests > [current architecture](../../../docs/CURRENT_ARCHITECTURE_STATUS.md) > revision-bound experimental records > these proposals > historical recollection. This research folder must not be used to claim implementation or release readiness. Future work is activated only by held-out experiments or measured developer needs.
