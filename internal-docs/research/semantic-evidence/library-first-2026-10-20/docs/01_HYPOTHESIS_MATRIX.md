# 01 — Research hypotheses, including library-first amendment

Status taxonomy: `EXECUTED_MODEL` = standalone 2026-10-16 C# model; `HISTORICAL_UT` = actual UT tests in previous research; `SOURCE_AUDIT` = code reading; `DECISION` = chosen design, **not an experiment**; `UNVERIFIED` = acceptance test still required.

| ID | Hypothesis / requirement | Conclusion and evidence |
|---|---|---|
| H01 | Every trusted C# inference rule requires a separate proof checker | **NO — DECISION**. Trust the host rule implementation; enforce preconditions. |
| H02 | Level 2 and 3 need separate stores/schedulers | **NO — EXECUTED_MODEL**. One scoped root+derived graph worked. |
| H03 | Facts must be indexed by correct subject/scope | **YES — EXECUTED_MODEL**. Cross-subject mutants failed. |
| H04 | Plugin self-asserted ID is authorization | **NO — EXECUTED_MODEL**. Spoof mutation caught; actual UT registration identity still unverified. |
| H05 | Level 3 derives results impossible with normal C# | **NO — EXECUTED_MODEL**. 18,081 same decisions in bounded test. |
| H06 | Independent P2 adds knowledge without recompiling Q | **YES IN MODEL**. Q SHA unchanged. Not executed as UT N+1. |
| H07 | Global persisted DAG required | **NO — DECISION**. Bounded session graph suffices for pilot. |
| H08 | Staleness/revocation can deny facts without re-proving rules | **YES IN MODEL**. Separate finite TLC staleness study also passed, not compiler proof. |
| H09 | DAG diamonds are invalid cycles | **NO — EXECUTED_MODEL**. Correct memoized traversal passed diamond test. |
| H10 | Atomic publication matters | **YES IN MODEL**. Only one of 32 commits won; UT native publication unverified. |
| H11 | UT's structural SSA verifier ensures semantic preservation | **NO — HISTORICAL_UT**. Well-formed 0→1 pass accepted in deliberate low-level probe; stock Wist not thereby unsafe. |
| H12 | Partial roots batch import acceptable | **NO — EXECUTED_MODEL**. All-or-nothing import validated. |
| H13 | Arbitrary external proof DAG safe without checker | **NO — DECISION**. Only trusted rule-created derived nodes in v1. |
| H14 | Fact derivation automatically grants arbitrary rewrite | **NO — DECISION**. UT-specific fixed rewrite and atomic gate required. |
| H15 | Generic UT proof gate already exists | **NO — SOURCE_AUDIT**. Pipeline checks structure/contract, not proof-aware semantics. |
| H16 | Proof gate covers every UT path by default | **UNVERIFIED**. Selected route/owner bypass audit required; don't claim global coverage. |
| H17 | TLC and Python fixed finite protocol agree | **HISTORICAL_EXECUTION**. 22,568 distinct states; does not prove program transformations. |
| H18 | Arbitrary malicious in-process extension is isolated | **NO — DECISION**. Not a safety claim or sandbox. |
| H19 | UT integration performance and release budgets met | **UNVERIFIED**. C# model not representative of full UT build. |
| **H20** | Portable kernel should compile without UT dependencies | **DECISION / UNVERIFIED**. Mandatory G1 test; original `Contracts` prototype is not extracted. |
| **H21** | A non-UT host can consume exactly the same public API | **DECISION / UNVERIFIED**. Build tiny separate host (e.g. SQL query analyzer or standalone toy-language AST). Must not import UT abstractions. |
| **H22** | A portable core can authenticate root truth by itself | **NO — DESIGN FACT**. Host supplies authority; core validates admission capability and session constraints. Host spoof resistance is adapter-specific. |
| **H23** | Portable library should own Rewrite Gate | **NO — DECISION**. Only real UT owner knows which IR snapshot is published and can atomically commit. |
| **H24** | Reusing rules is independent of SSA | **CONDITIONAL / UNVERIFIED**. Int32 range/affine math portable; subject semantics and overflow model come from host. Test non-UT consumer. |
| **H25** | Immediate public NuGet package and SemVer freeze justified | **NO — DECISION**. Internal experimental package/projects until two hosts and bounded API stability checks. |
| **H26** | Separate library automatically reduces complexity | **UNVERIFIED**. Measure adapter LoC, API change frequency, allocations and indirect calls against direct C# alternative. |
| **H27** | Previous 15/15 and 18,081 counts prove new library | **NO**. Old model-only evidence remains unchanged, migration acceptance yet to run. |

**Research conclusion:** Basic trust semantics are chosen. New unresolved items are *portability*, *UT-specific authority/publication* and *practical benefit*, not the need for a universal proof checker.
