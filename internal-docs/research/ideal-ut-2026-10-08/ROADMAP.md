# Integrated Ideal UT roadmap — gated, not calendar promises

**Scope:** incremental experimental work, no immediate new stable Core API. Owners below denote responsibility boundaries, not assigned persons.

| Gate | Work and subsystem owner | Definition of done | Abort/revert |
| --- | --- | --- | --- |
| M0 | LanguageSdk truth + tooling-only read snapshot | selected `LanguagePlan`, exact chosen implementation digest, diagnostics and schema recorded; read-only/preview derived from same `LanguageCompiler` | any duplicate resolver/state |
| M1 | Independent testing/oracle harness | positive and negative oracle manifests, two independent semantic counterexamples detected (incl. checked overflow/effects), reproducible seed | "both backends agree" is sole oracle |
| M2 | Experimental evidence adapter + three independent packages | Shape + Range → unchanged BoundsOptimization; new RangeV2 gives more legal rewrites; stale/negative/contradiction/unknown gives fallback; compare direct-interface baseline | false-safe rewrite, shared-code edits or inferior to plain SDK |
| M3 | Actual typed AST↔AIR mapping and independent checker | checked 1:N correspondence, overflow/effect/heap semantics, mutation revokes transfer | implicit equivalence or requirement for universal SSA |
| M4 | Tooling/product and source-generator experiments | separate non-Wist language exercises grammar/binder/authoring and diagnostics; source generator shows fewer manual registrations | hidden planner or DSL dominated by escape hatches |
| M5 | Backend-specific freeze/specialize after legality gate | explicit hot path, exact implementation guard; measured compile+materialize+first/steady-state costs | speculation adds more cost or loses correctness |
| M6 | Optional bounded rules/index/local region feasibility | only if M2–M5 show independent transitive needs | no additional measurable value after held-out workloads |

```text
M0 (identity/read-only) ─┬─> M2 (N+1 semantic slice) ─> M3 (cross-IR) ─> M5
                        ├─> M1 (independent oracles) ──────────────┘
                        └─> M4 (product authoring, tooling) ──────────┘
M6 only after empirical triggers; stopping with ordinary typed SDK is success if simpler.
```

**Priority:** correctness and evidence before new inference machinery; independent authoring and user tooling may proceed alongside semantic research. The original research package describes tests E1–E8; see [EXPERIMENTS.md](EXPERIMENTS.md). Measure actual code and tooling costs, preserve negative cases and keep source/test/doc drift visible.

**Implementation gate:** baseline revision → reproducer and negative control → minimal PR → focused tests → full relevant suite + architecture guards → commit-tied evidence → re-audit IUT statuses. Report whether experiments run on UT itself. Do not weaken documentation/CI checks, claim NuGet publication or invent deadlines.
