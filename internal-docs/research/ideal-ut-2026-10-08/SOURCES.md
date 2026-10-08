# Sources, evidence grades and missing information

Research date 2026-10-08. Compiler baseline `1d46f17c8dc28f434fa58bdf92f9f8278fa5aaee` (2026-09-20). This dossier was synthesized from **two supplied archives** plus the exact UT and presentation repository sources, rather than copied verbatim.

## User-supplied archives (content inspected; manifests checked locally)

1. `UniversalToolchain_Research_Package_2026-10-08(2).zip`. Root `UniversalToolchain_Research_2026-10-08/`; files `00_README.md`, `01_EXECUTIVE_AND_OPTIONS.md`, `02_AUDIT_AND_GAPS.md`, `03_PRIOR_ART_AND_COMPETITORS.md`, `04_TARGET_ARCHITECTURE_AND_APIS.md`, `05_LANGUAGE_TOOLING_PRODUCT.md`, `06_EXPERIMENTS_AND_ROADMAP.md`, `07_EVIDENCE_LEDGER.md`, `08_CODEX_EXECUTION_TASKS.md`, `09_COMPLETENESS_REAUDIT.md`, `10_COMPETITIVE_DIMENSIONS.md`, `11_EXECUTED_EXPERIMENT_AND_REPLAY.md`, isolated prototype and separate .NET 10 experiment. `SHA256SUMS.txt` records package contents.
2. `UT_AI_IdealUT_Research_Revised_2026-10-08_r1(1).zip`. Root `UT_AI_IdealUT_Research_Revised_2026-10-08_r1/`; `IDEAL_UT_MODEL.md`, `DECISION_RATIONALE.md`, `DESIGN_ALTERNATIVES.md`, `HYPOTHESIS_DOSSIERS.md`, `IMPLEMENTATION_SLICES.md`, `BENCHMARK_PROTOCOL_REVISED.md`, `ADVERSARIAL_REVIEW_REVISED.md`, evidence register, schemas and Python spikes. `MANIFEST.sha256` records package contents. Its missing original **A–R labels and original verification attachments** were explicitly not reconstructed: P1–P5 and scoring remain provisional.

Raw immutable archives are **not** copied into this repo by this docs PR; the index captures decision-ready conclusions. If maintaining an archival repository of exact source bytes becomes necessary, add the original supplied archives as separate immutable evidence with independently checked manifests.

## Canonical repository links

[UT current architecture](../../../docs/CURRENT_ARCHITECTURE_STATUS.md) · [source `LanguageCompiler.cs`](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs) · [`LanguagePlan.cs`](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlan.cs) · [`LanguageRuntime.cs`](../../../UniversalToolchain/UniversalToolchain.Runtime/LanguageRuntime.cs) · [2026-09-20 semantic note](../../../docs/research/semantic-fact-proof-layer-2026-09-20.md) · [future-work triggers](../../../docs/architecture/future-work.md) · [LangDev 2026 evidence record](https://github.com/Misha1302/lang-dev-presentation-2026/blob/main/REBUILD_EVIDENCE.md).

Prior art: [MLIR interfaces](https://mlir.llvm.org/docs/Interfaces/) · [ableC](https://melt.cs.umn.edu/ableC/) · [LLVM New Pass Manager](https://llvm.org/docs/NewPassManager.html) · [Alive2](https://web.ist.utl.pt/nuno.lopes/pubs.php?id=alive2-mem-cav21) · [Soufflé provenance](https://souffle-lang.github.io/provenance) · [Salsa](https://salsa-rs.github.io/salsa/) · [Truffle](https://www.graalvm.org/latest/graalvm-as-a-platform/language-implementation-framework/) · [MPS](https://www.jetbrains.com/help/mps/typesystem.html).

## How much is actually known?

- **Observed in sources:** architecture boundaries, typed planner/runtime, deck narrative and dated research files at named revisions.
- **Reported only by independent prototypes:** Python toy 8 tests, separate four-assembly .NET 15 assertions; not run here against real UT.
- **Proposed:** generalized semantic evidence, proof transport between IRs, new-program lawful optimization, agent semantics, real SIMD implementation.
- **Unknown:** cross-compiler correctness, commercial demand, soundness for untrusted plugins, exact historical A–R matrix, current performance against well-designed controls, CI results of this branch.

To change a claim status: pin new code commit and environment; run held-out E1/E2/E6 with negative cases and appropriate controls; publish result logs and code/fixture refs; only then update this document and the IUT table.
