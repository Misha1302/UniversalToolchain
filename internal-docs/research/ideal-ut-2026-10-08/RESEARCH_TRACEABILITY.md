# Research traceability and acceptance audit (2026-10-08)

**Scope:** preserve the organization and epistemic status of the two user-provided research packages. This folder is **curated documentation**, not a verbatim archive. The original ZIPs and their manifests are **not committed to this branch**; they remain necessary for exact source-level historical reproduction. `SOURCES.md` identifies them. Do not cite this derivative index as primary evidence for computations or histories absent from the repo.

## The original 14 deliverables: exact source-to-repository map

The first ZIP's `00_README.md` and `09_COMPLETENESS_REAUDIT.md` define fourteen deliverables; this table prevents them from disappearing through over-condensation.

| # | Original requested deliverable | Original document(s) | Curated destination | Residual limitation |
| --- | --- | --- | --- | --- |
| 1 | Executive research report | `01_EXECUTIVE_AND_OPTIONS.md` / combined report | `README.md`, `STRATEGY_PRIOR_ART.md` | Full prose not copied |
| 2 | Current UT architecture audit | `02_AUDIT_AND_GAPS.md` | `CURRENT_VS_IDEAL.md` | No complete fresh local UT checkout |
| 3 | 13 Ideal UT gaps | `02_AUDIT_AND_GAPS.md` | `CURRENT_VS_IDEAL.md` | Statuses revision-bound |
| 4 | Scientific prior art | `03_PRIOR_ART_AND_COMPETITORS.md` | `STRATEGY_PRIOR_ART.md`, `SOURCES.md` | Selective review, no systematic literature meta-analysis |
| 5 | Competitive landscape | `03_...`, `10_COMPETITIVE_DIMENSIONS.md` | `STRATEGY_PRIOR_ART.md` | Full 18×8 competitor score matrix remains in ZIP |
| 6 | Strategic A–H options | `01_EXECUTIVE_AND_OPTIONS.md` | `STRATEGY_PRIOR_ART.md` | No user demand surveys |
| 7 | Unified target architecture | `04_TARGET_ARCHITECTURE_AND_APIS.md` | `ARCHITECTURE.md` | Proposed, not a merged implementation |
| 8 | Public C# API proposals | `04_TARGET_ARCHITECTURE_AND_APIS.md` | `PROPOSED_API_CONTRACTS.md` | Illustrative signatures, not compile-verified public SDK |
| 9 | End-to-end user journeys | `05_LANGUAGE_TOOLING_PRODUCT.md` | `LANGUAGE_ENGINEERING_PLATFORM.md` | No real independent user session |
| 10 | Experimental plan | `06_EXPERIMENTS_AND_ROADMAP.md`, `11_EXECUTED_EXPERIMENT_AND_REPLAY.md`, `prototype/` | `EXPERIMENTS.md`, `ENGINEERING_BACKLOG.md` | Only isolated prototypes locally replayed |
| 11 | Integrated roadmap | `06_...`, `08_CODEX_EXECUTION_TASKS.md` | `ROADMAP.md`, `ENGINEERING_BACKLOG.md` | No guaranteed implementation dates |
| 12 | Risks, rejected ideas | `01_...`, `06_...` | `IDEAS.md`, `STRATEGY_PRIOR_ART.md` | Architectural decision gates provisional |
| 13 | Open research questions | `06_EXPERIMENTS_AND_ROADMAP.md` | `EXPERIMENTS.md` | Requires held-out tests |
| 14 | Evidence ledger | `07_EVIDENCE_LEDGER.md`, `11_EXECUTED_EXPERIMENT_AND_REPLAY.md` | `SOURCES.md`, this file | Raw complete ledger remains in ZIP |

The original source also specifies **20 closure conditions** in `09_COMPLETENESS_REAUDIT.md`. They are not 20 completed production properties. Material remaining: repository-wide audit and reproducible build, UT-integrated independent author and interop experiment, measured benchmarking, external researcher replication, user/customer validation, exhaustive prior-art review and demonstrated superiority. All 14 *research deliverables* had a bounded written output in the source package, but that is distinct from implementation or validation of Ideal UT.

## Revised AI/Ideal UT package: what was and was not transferred

- `IDEAL_UT_MODEL.md`: selected-world vs authoring-world, exact revision identity, small trusted kernel, optional specialization. See `ARCHITECTURE.md`.
- `HYPOTHESIS_DOSSIERS.md`: H1 single planner, H2 semantic evidence, H3 generated authoring, H4 cross-representation, H5 specialization, H6 explainability, H7 independent agent oracles, H8 agent tool projections. H9 incremental indexing, H10 limited rewrites, H11 read-only preview, H12 differential parity. See `IDEAS.md` and `EXPERIMENTS.md`.
- `DECISION_RATIONALE.md`, `DECISION_MATRIX_REVISED.csv`: ordinal 0–5 estimates and overlapping uncertain intervals, **not measurements**. Direct interfaces (A0) are on a near-term cost frontier; no scalar winner. See `STRATEGY_PRIOR_ART.md`.
- `IMPLEMENTATION_SLICES.md`, `BENCHMARK_PROTOCOL_REVISED.md`: reversible slices, trust/cost benchmarks. See `ENGINEERING_BACKLOG.md`.
- `ADVERSARIAL_REVIEW_REVISED.md`, `EVIDENCE_REGISTER_REVISED.md`: defects, missing data and authority bounds. See `SOURCES.md`.
- `schemas/`, `spikes/`: executable **toy** schema validators and evidence models, not current UT APIs.

**Missing historical identity:** the exact original A–R scoring rows and pre-revision verification attachments were unavailable to the revised research authors. P1–P5 are explicitly reconstructed/provisional and must never be presented as recovered original labels.

## Audit receipts and limits

On 2026-10-08, rechecked both source ZIP manifests: **26/26 checks each**. Replayed source-archive Python prototype **8/8**, revised Python spikes **27/27**, and separate .NET 10 four-assembly harness **15/15** under SDK 10.0.301. The .NET harness is isolated from the real UT repository. These receipts establish local prototype behavior only; not general soundness, production API integration or runtime/IR parity.

The original research's pinned UT CI-success claims pertain to exact `1d46f17` and are separately documented in `09_COMPLETENESS_REAUDIT.md`; this dossier's own PR checks must be separately interpreted by its new head SHA.

## New claim-promotion rule

To upgrade PROPOSED → EXPERIMENTALLY DEMONSTRATED: identify actual UT source/commit, reproduce tests on selected `LanguagePlan`, run negative mutants, compare direct interface and MLIR-style alternative with equal conditions, retain machine-readable logs and external holdout cases. To upgrade to SUPPORTED: merge code, tests, public contracts, examples, release evidence and verified user documentation. A schema/test report alone never performs this promotion.
