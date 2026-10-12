# The 222-candidate register — independent triage audit (2026-10-12)

**Scope:** count and decision-oriented review of [the existing sole inventory](DEVELOPMENT_INVENTORY_2026-10-09.md) as found at `master@886bf241623e83566585bf2aec5b635fefe1675f`, inventory blob `1bff6c1fabcbdb5a515c85a37902ac1316f0e45f`. **Not a replacement backlog, not 222 verdicts, and not a source of statuses.** A content count does not imply implementation verification.

## Mechanical count from actual source

The 15 category headings were found under `## I. Основной реестр`. Count rule: lines starting with the register's tagged item prefix `- \`[P0]`, `- \`[P1]`, etc. Section II–IX prose and the five out-of-inventory recommendations are excluded.

| Domain | Entries | P0 | P1 | P2 | P3 |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 Core architecture/composition | 14 | 5 | 5 | 3 | 1 |
| 2 Semantics/evidence | 20 | 12 | 7 | 0 | 1 |
| 3 IR/SSA/optimization | 22 | 8 | 10 | 3 | 1 |
| 4 Language authoring SDK | 16 | 4 | 9 | 3 | 0 |
| 5 IDE/LSP/incremental | 12 | 2 | 7 | 3 | 0 |
| 6 Debugging/diagnostics/trace | 12 | 1 | 9 | 2 | 0 |
| 7 MCP/AI authoring | 14 | 0 | 11 | 2 | 1 |
| 8 CLI/Web UI/workflows | 13 | 0 | 10 | 3 | 0 |
| 9 Backends/performance | 15 | 2 | 8 | 4 | 1 |
| 10 Testing/correctness | 17 | 6 | 10 | 1 | 0 |
| 11 Security/trust | 12 | 3 | 6 | 3 | 0 |
| 12 Developer APIs/ecosystem | 14 | 1 | 10 | 3 | 0 |
| 13 Product/adoption | 11 | 0 | 8 | 2 | 1 |
| 14 Research/questions | 16 | 4 | 9 | 2 | 1 |
| 15 Infrastructure/CI/debt | 14 | 3 | 8 | 2 | 1 |
| **Total** | **222** | **51** | **127** | **36** | **8** |

**Important interpretation:** 51 P0 lines are *relative importance inside proposed features*, not 51 unconditional immediate commitments. Most lines originate in an earlier audit and require updated implementation/source checking before classification.

## Fifteen domains — decision-oriented independent critique

| Domain | Keep as near-term conditional owner concern | Strong alternative / holdout | Defer or refuse |
| --- | --- | --- | --- |
| 1 Architecture | same LanguageCompiler, exact selected-world identity, N+1 | direct typed C# interfaces with frozen consumer | independent planner, persistent plan cache without measured need |
| 2 Semantics | narrow typed `SafeIndex` obligation with policy, provenance and conflict | direct range/shape callback with local invalidation | global FactStore, generalized rule engine until ≥2 distinct transitive uses |
| 3 IR | explicit invariant/verifier on actual Wist AIR/SSA; PR #371 external-provider test only after merge decision | ordinary handler + verified converter | mandatory SSA, full e-graph optimizer |
| 4 Language SDK | independently authored complete *different* DSL and precise error spans | handwritten parser / ableC / language workbench | all-purpose generator before author trial |
| 5 IDE | revision-safe document snapshots, binder-aligned diagnostic prototype | existing language-server framework | full multi-language IDE without pilot |
| 6 Tracing | improve existing Debug Trace v2 and stage evidence, no source-declared proof | structured logs + stable IR dumps | graphical explainer as authority |
| 7 AI | contract retrieval + blinded authoring benchmark | plain docs/IDE agent with source access | forbidding source reads categorically or trusting agent proof |
| 8 CLI/UI | typed diagnostic/plan projection, one current planner | existing `wistc` and documented API | parallel UI model duplicating selection state |
| 9 Execution | interpreter/CIL expanded observable parity, measured compile/runtime | simple compiled delegates and standard backend | adaptive tiering absent profiles |
| 10 Testing | independent semantics oracle with negative mutants | trusted reference interpreter/formal bounded checker | shared-bug parity as proof |
| 11 Security | exact selected executable hashes and trust-boundary tests | conservative pass whitelist, process sandbox where needed | treating plan validity as hostile-code isolation |
| 12 DX | bounded public API stability and clean external consumer | plain typed C# package and templates | large compatibility promise without versioned release |
| 13 Product | narrow restricted policy/pricing pilot with real users | ordinary embeddable rules engine | speculative commercial support model |
| 14 Research | E1 primary discriminator; E2 only if E1 wins | baseline A and MLIR typed interfaces | publish universal-new-architecture claim |
| 15 Infra | doc/source schema consistency and negative guards | targeted lint/CI with reproducible commands | early repository split |

This table is a **proposed prioritization lens**. It does not mutate the 222 original records' owner status.

## Near-duplicates and dependency groups to cross-link, NOT delete

1. **E1 cluster:** `Semantic Composition`, `N+1 Extensibility`, `Semantic Query API`, `E1 Semantic Interoperability`, `Real Bounds-Elimination Test`. These are problem/capability/API/experiment/fixture **views of one decision path**, not five separate mandatory subsystems. Canonical question: E1; canonical design status: IDEAS; primary test gate: M2/S2.
2. **Revision/identity cluster:** `ProgramSnapshot`, `Semantic Subjects`, `Scoped Evidence`, `Executable Provenance`, `Evidence Invalidation`. They cooperate, but `DocumentSnapshot` in the editor is *not* necessarily the same object type as compiler `ProgramSnapshot`; reuse identity primitives only if lifecycle assumptions align.
3. **Correctness cluster:** `Proof Trust Enforcement`, `Independent Semantic Oracles`, `Proof Obligations`, `Preservation Certificates`, `E2 Cross-Representation Proof`. Separate *trusted source*, *test oracle*, *action precondition* and *proof of transport*. Don't conflate.
4. **IR extensibility cluster:** `Bytecode Verification`, `Bytecode Tag Registry`, `Declarative Bytecode/handler` proposals and PR #371. Any new registry must be selected by `LanguagePlan` to satisfy real independent extension; an internal Wist static registry does not meet this gate.
5. **Authoring/product/AI cluster:** `Grammar SDK`, `Parser Generation`, `Tool Schema Contracts`, `MCP Adapter`, `Independent Agent Oracles`. Prefer one machine-readable public contract projection reused by CLI/IDE/agent clients, not three competing sources of semantics.

A “duplicate” here is a candidate for a *cross-reference*, not proof that two tasks have identical acceptance criteria. Keep historical negative results and scoped hypotheses.

## A source-grounded drift finding and action

The inventory flags schema-documentation drift in domain 15. It is real at this HEAD: [LanguageLockFile.cs](../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageLockFile.cs) sets `public const int SchemaVersion = 6`, while [contribution-planning.md](../../../docs/language-authoring/contribution-planning.md) described **schema-v5 lock**. The language author's [versioning guide](../../../docs/language-authoring/versioning-and-migrations.md) and [current architecture](../../../docs/CURRENT_ARCHITECTURE_STATUS.md) already say v6. This review's branch corrects *only that lock-schema sentence* to v6. It does **not** change independent feature manifest schemas, which may legitimately be v5; conflating their versions would cause a different bug.

## Decision thresholds and rejection

Do not promote all 51 P0s. The minimum critical path is baseline+oracle → N+1 actual UT mutation and strongest C# control → adversarial proof authority → cross-IR only on justified evidence. Independent authoring, documentation and agent-token experiments proceed in parallel without requiring the speculative universal fact ontology. If direct interfaces are equally correct and cheaper, mark the generalized evidence proposal **REJECTED** in [IDEAS.md](IDEAS.md) with the attached negative experiment; preserving 222 historical candidates remains correct.

The source register is not edited by this audit; its original 222 entries and source citations stay intact.
