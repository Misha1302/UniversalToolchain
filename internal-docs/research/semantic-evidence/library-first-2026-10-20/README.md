# CompilationLab — SemanticEvidence: library-first Level 2 + Level 3 implementation plan

**Revision:** 2026-10-20 (supersedes architecture ownership in 2026-10-16 plan). **Status:** design frozen for a *scoped pilot*, NOT implemented in UT or released as a standalone package. **Reference UT commit:** `886bf241623e83566585bf2aec5b635fefe1675f` (historical audited baseline; refresh before source changes). This document is part of a research-only draft PR; it does not assert that any library or UT adapter has been implemented.

## One-sentence decision

Create **standalone .NET `SemanticEvidence.Core` (+ optional `SemanticEvidence.Rules.Numeric`)** for typed contextual facts, trusted inference and dependency DAG; put **all UT semantics, selected-plan admission and actual Rewrite Gate/publication into a separate `UniversalToolchain.SemanticEvidence` adapter**. The library must compile and run without any UniversalToolchain dependency.

## Read in order

1. `docs/00_DECISION.md` — amended ADR / alternatives / what stays trusted.
2. `docs/02_IMPLEMENTATION_ARCHITECTURE.md` — module boundaries, identities, rule semantics and example API.
3. `docs/07_LIBRARY_PACKAGING_AND_API.md` — package layout, assemblies, allowed references, API stability, independent library demo.
4. `docs/03_UT_INTEGRATION_ROADMAP.md` — exact phased implementation, ownership, negative-path tests, stop criteria.
5. `docs/08_MIGRATION_AND_ACCEPTANCE.md` — migration map from existing prototype + required validation commands.
6. `docs/01_HYPOTHESIS_MATRIX.md` — what was actually tested vs what is chosen vs remains to test.
7. `docs/04_VERIFICATION_AND_ADVERSARIAL_TESTS.md`, `docs/05_EVIDENCE_AND_PROVENANCE.md` — evidence scope; `docs/06_READY_TO_IMPLEMENT_PROMPT.md` — ready-to-use Codex brief.

## Preserved evidence, not the new implementation

The original `prototype/` and `logs/` trees are preserved **byte-for-byte inside** `evidence/P1_P2_Q_research_snapshot_2026-10-16.zip` (30 files, with an internal SHA-256 manifest). The working Git tree intentionally contains no historical `*.csproj` files: the repository-wide project-ownership validator must see only actual UT projects. In that old research prototype, `prototype/Contracts/SemanticCore.cs` mixes generic inference with simulated rewrite permission and uses SSA-flavored subjects. **It is not the proposed independent library.** Do not rename it and call extraction complete; perform the migration described in `docs/08_MIGRATION_AND_ACCEPTANCE.md`.

Previous replay, if .NET 10 is available:

```bash
tmp=$(mktemp -d)
unzip -q evidence/P1_P2_Q_research_snapshot_2026-10-16.zip -d "$tmp"
cd "$tmp/prototype"
DOTNET=/path/to/dotnet ./run_repro.sh
DOTNET=/path/to/dotnet python3 run_mutation_tests.py
```

This only replays the historical C# model; it does not prove portable API independence, release safety, a real UT-backed Rewrite Gate, or a published NuGet package.

## Integrity

`SHA256SUMS.txt` and `VERIFY_PACKAGE.sh` cover current Git-tracked research files and the complete preserved historical snapshot, including 30 original source/log files verified through the snapshot's internal `PRESERVED_SHA256SUMS.txt`. The historical bytes are unchanged. The original 2026-10-16 ZIP is a separate immutable predecessor.
