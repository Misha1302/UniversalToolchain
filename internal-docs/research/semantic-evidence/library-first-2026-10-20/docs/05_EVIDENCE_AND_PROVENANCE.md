# 05 — Provenance and evidence boundaries

## Revision chain

1. Historical `CompilationLab_UT_SemanticProof_Research_2026-10-16_FINAL.zip` — earlier current-source UT smoke and real TLC staleness evidence; production gate NOT implemented.
2. Historical `CompilationLab_Level2_Level3_Final_Implementation_Plan_2026-10-16.zip` — decision for trusted rule model, C# `prototype/` research (15/15 checks, 18,081 bounded direct-C# comparisons, 3/3 mutants, Q DLL unchanged), docs and logs.
3. **This** 2026-10-20 revision — user-approved **library-first plan update**. All `prototype/` and `logs/` copied exactly without modifying a line or changing a log. Documentation and API ownership/roadmap were revised. **No** standalone `SemanticEvidence.Core`/`Rules.Numeric`, UT adapter or guarded rewrite implemented or compiled as part of this revision.

## Source reference

The design remains anchored to `Misha1302/UniversalToolchain master@886bf241623e83566585bf2aec5b635fefe1675f`, historically verified 2026-10-16. Relevant source snapshots and hashes (verified earlier):

- `SsaOptimizerPipeline.cs`: `c2118ec97fff44333d0d8ceb8e2e434b463ea885d96c8c29ad6e44ea289c2f1c`
- `SsaRoundtripRoute.cs`: `846952af3a8d66e927834e940d0cc17a721925d517a2c93bfea1ab7553dc6bf4`
- `LanguageArtifactBuildPipeline.cs`: `7b65b7f147cece9d8e3148b987fd5a194eb1d5e933b2f1c965041789d445f9d3`
- `IrStageContract.cs`: `ca7b6b3285732570565136f67c736358f77e4e7682318402cf716ff019a0c38a`

Check current HEAD before modifying source; no assertion of latest HEAD is made in this revision.

## Runtime bootstrap provenance (current local inspection)

One coherent provided archive root `compilationlab-static-protocol-bundle-personalized-2026-09-14.1.0` was inspected; gate SHA256 `56903c252e46275f341ddcefaa8b6175e67ee492fb9c7c1983e18e0418d55d9b`. `MANIFEST.sha256` checked **557/557 entries** against actual archive bytes, 0 missing. The required owner files existed and were read for inspection. No trusted-host attestation; host identity is **UNVERIFIED**. The archive's local copy is not evidence that it was host-injected.

## Status and artifact claim

- **VERIFIED**: source package ZIP access, original 40-file SHA verification before edits, new plan documents, manifest/clean-unpack checks performed after packaging.
- **PRESERVED HISTORY**: logs and model code in `prototype/` and `logs/` are original experimental evidence, not new run results.
- **DESIGNED, NOT IMPLEMENTED**: standalone library, second host, actual UT adapter, new guarded IR commit, NuGet publishing, multi-platform CI.
- No GitHub master/PR write, no remote package publication.

This release is a **revised implementation plan**, not an implemented framework or proof of general semantic correctness.
