# 08 — Exact changes vs previous plan, migration, and acceptance

## Decisions changed since 2026-10-16

| Prior frozen plan | Revised library-first plan |
|---|---|
| `UT-L3-01`: kernel lives internally in `UniversalToolchain.Ssa.Optimization` | **`LF-00/01` first:** independent `SemanticEvidence.Core` + numeric rules; no UT reference |
| `ICompilerEvidenceSession` is an internal UT-only API | Public portable read/query API; authority/provenance construction stays host-adapter controlled |
| Prototype session `TryCommit` models fact use + publication | **Split:** Core only derives/queries; `UniversalToolchain.SemanticEvidence` exclusively owns Rewrite Gate/IR commit |
| SSA-centric `SubjectKey` inside kernel | Opaque portable key; UT adapter resolves actual SSA value/scope/snapshot; non-UT host supplies its own model |
| P1/P2/Q demo model proves basic inference | Keep as **unchanged historical evidence**; add separate *second host* as NEW portability acceptance |
| One combined UT-native change set | Three independently reviewable changes: Core, Numeric, opt-in UT adapter |
| Implementation may begin inside UT immediately | **Begin with LF-00 and LF-01**, proceed into UT only after PORT-01/02/03 |
| Package story unspecified | Library-first, internal experimental packages, optional later NuGet / separate repository |

## Migration mechanics

1. **Preserve** original `prototype/` and `logs/`. They document 15 checks, 18,081 bounded comparisons and 3 mutation detections; do not move/rename them into a package and silently call them standalone.
2. Extract only the core logic of root batch admission, scoped facts/handles, trusted parent derivation, liveness/revocation and graph validation. Redesign `World/Subject/Scope` into opaque generic host-bound IDs. Do not import `SemanticPrototype.RewritePermit`, `TryCommit`, `RewriteApplied` or `ProgramView` as public core contracts; rewrite the latter as portable read-only contexts.
3. Port checked arithmetic as trusted rules into `SemanticEvidence.Rules.Numeric`. Keep roots/derivations and integer model explicit. Tests prove typed premise identity, not just matching strings.
4. Run all old model tests unchanged as regression (preserved in ZIP). Add fresh tests of *new* Core; do not reuse old PASS numbers as new-package evidence.
5. Implement a toy non-UT host referencing only the new Core+Numeric DLLs. Reproduce P1→P2→Q and optionally demonstrate a completely different subject domain.
6. Implement selected `LanguagePlan` mapping in thin UT adapter and **only there** an opt-in guarded transformation. Keep a strict log of protected old UT behavior.
7. Compare strong typed C# baseline and decide whether the package wins in maintenance/provenance/diagnostics despite ordinary typed C# being equally expressive.

## Deliverable acceptance sequence

```text
G0 ORIGINAL EVIDENCE PRESERVED (checked SHA)             : expected PASS on revised ZIP
G1 PORTABLE CORE+RULES BUILD, ZERO UT DEPENDENCIES      : NOT_RUN (requires implementation)
G2 INDEPENDENT NON-UT HOST, Q SHA STABLE               : NOT_RUN
G3 CURRENT UT SELECTED AUTHORITY AND NO BYPASS          : NOT_RUN
G4 REAL UT GUARDED REWRITE + BACKEND ORACLE             : NOT_RUN
G5 PERFORMANCE/CI/ABI/PUBLIC RELEASE DECISION          : NOT_RUN
```

**A documentation edit is not feature implementation.** No public `SemanticEvidence.Core` API has yet been built by this revision. `LF-00` is the next executable action.

## What to verify for the archive itself

- `bash VERIFY_PACKAGE.sh` returns `PACKAGE_INTEGRITY=PASS`.
- Compare every `prototype/` and `logs/` file SHA to predecessor ZIP; **all bytes must match**.
- Clean-unpack ZIP then re-run verification; ensure there are no `obj/bin` or accidental local SDK binaries.
- No GitHub write. Package is standalone deliverable; it does not update the original plan archive in-place.
