# 04 — Test and evidence matrix (updated for library-first)

## Already executed, historical (NOT the new library)

The archived `prototype/` and `logs/` contain the **original 2026-10-16** C# research model: 15/15 assertions, 18,081 bounded direct C# comparisons with no disagreements, independent P2 and SHA-stable Q, 3/3 mutation detections and one winning commit among 32 contenders. That prototype uses `SemanticPrototype` / `Contracts`, simulates a compiler world, contains `RewritePermit` in its session and is **not** the standalone SemanticEvidence design. None of these historical results are silently upgraded to the new artifact's new-package tests.

Earlier UT source-level experiments observed valid SSA but incorrect 0→1 transformation accepted by a deliberately low-level pass, malformed SSA rejected; Wist restricted arithmetic produced 4,680 matching comparisons. TLC (22,568 finite states) validated a different staleness protocol. They **do not** verify a standalone library, UT adapter or guarded rewrite.

## Required new standalone acceptance tests (all NOT_RUN as of this plan revision)

| ID | Test | Required result |
|---|---|---|
| PORT-01 | Build Core and Numeric as isolated .NET projects | PASS, no UT reference or namespace |
| PORT-02 | Dependency graph static scan / `project.assets` review | No reverse dependency Core→host/adapter |
| PORT-03 | Second non-UT consumer, independent domain subjects | Same libraries and public API produce valid derivation |
| PORT-04 | Build Q before P2, then add P2 | Byte-identical Q DLL; no central library recompilation |
| PORT-05 | Core exposed API reflection test | No SSA, AIR, Wist, LanguagePlan, IR publication or `TryCommit` API |
| PORT-06 | Default independent session, foreign-host session handles | No cross-session fact reuse |
| PORT-07 | Invalid root batch; cancelled/failed trusted rule | No partial facts or derived nodes |
| PORT-08 | Wrong subject, type, scope, semantic model, revision | Refuse; never promote to known fact |
| PORT-09 | Checked arithmetic overflow and contradiction | Explicit Unknown/Unsupported/Conflict, no fabricated bound |
| PORT-10 | Diamond DAG and attempted cycles | Valid diamond accepted, true cycles rejected |
| PORT-11 | Root revoke/world advance; graph limits | Dependent facts unusable; exhausted => no permit |
| PORT-12 | Concurrent queries/derivations | No stale exposed result under declared linearization contract |
| PORT-13 | Core and Numeric package compatibility | Deterministic build; no unreviewed third-party DI/runtime dependency |

## Required actual UT acceptance tests (all NOT_RUN for new split)

| ID | Test | Required result |
|---|---|---|
| UT-01 | Adapter only references Core/Rules (not reverse) | Build solution and inspect project graph |
| UT-02 | Selected `LanguagePlan` binds root authority and world | Forged/unselected producers fail closed |
| UT-03 | IR epoch/scope/subject binding | Stale world, ABA, wrong SSA value rejected |
| UT-04 | Fixed rewrite, pure comparison only | Safe expression rewritten; unsafe `x=7` not rewritten |
| UT-05 | Semantically wrong but well-formed proposal | Rejected by closed guarded path; no silent old artifact mutation |
| UT-06 | Malformed candidate, exception in proposer | Structural rejection and rollback, unchanged IR |
| UT-07 | Root revocation between inference and commit | Rejected at owner publication boundary |
| UT-08 | Two parallel commits, plan/policy change | At most one commit; wrong world rejected |
| UT-09 | Alternate route/source registration bypass | No bypass **within declared proof-enabled scope**, otherwise NO-GO |
| UT-10 | Real P1/P2/Q N+1; actual source→backend oracle | Unchanged Q DLL, no differences in supported observable subset |
| UT-11 | Normal Wist regression/default SSA policy | No unexpected behavior changes |
| UT-12 | Typed direct C# baseline and performance | Added complexity/overhead justified by evidence |

## Adversarial assumptions

- Trusted rules are code axioms, **not formally re-proved** per use. Test application checks, not mathematical theorem soundness.
- A malicious in-process CLR plugin can bypass typed APIs using reflection/unsafe; this is not a security boundary or sandbox.
- A host-supplied world key is not proof of truthful world identity. Only the UT adapter has actual selection and mutation authority.
- Root assumptions derived from unvalidated external inputs cannot be treated as global runtime facts. Tests must include inputs violating an assumed range unless the host enforces the range.
- Atomic Core graph operations do **not** imply atomic UT IR writes; both layers have distinct lifecycles.

## Test procedure

Run preserved legacy replay first (see `README.md`). Independently build Core+Numeric with a fresh `dotnet restore/build/test`, then second-host tests, then actual UT opt-in integration. Record SHA digests and stdout/stderr; do not conflate PASS for old model with NOT_RUN for new API. Gate release only on exact-commit tests/CI; no preemptive NuGet publish.
