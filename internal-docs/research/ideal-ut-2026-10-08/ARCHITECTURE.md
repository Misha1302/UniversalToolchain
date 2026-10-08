# Proposed technical architecture (not a compiled API)

```text
Selected independent packages
  → LanguageDefinition
  → LanguageCompiler (sole global configuration resolver)
  → immutable LanguagePlan (routes, providers, policies, provenance)
  → LanguageRuntime (exact materialization; no new selection)
         |
   ProgramSnapshot (program/artifact revision, phase, anchors)
         |
   Optional typed evidence producers → scoped fact queries
         |                                  |
         └───────────── proof obligations ─┘
                       |
      guarded transform + independent validation
                       |
       revisioned artifact → selected backend
                       |
               specialized hot path
```

## Ownership and invariants

- A `LanguagePlan` concerns global **language configuration**. It is not the execution plan of all future programs. `ProgramSnapshot` and optional local feasibility/region plans are program-derived and cannot choose new packages, capability providers or runtime routes.
- Wist's typed semantic program, Bytecode and AIR are distinct boundaries. A generic UT service must not require Wist AST or universal mandatory SSA. Typed operation/value/region/source anchors may be mapped across different representations only with checked correspondence.
- Every proposition needs a stable typed schema/version, subject, domain, declared effects/laws when relevant, owner, polarity, authority and exact scope. Scope includes plan identity **plus selected executable hashes**, backend, phase, program revision, assumptions and trust policy. `PlanHash` is canonical data identity, not proof of executable authenticity.
- Accepted evidence states must distinguish **Proven (under named trust policy), Disproven, Unknown, Contradiction**. Missing proof is Unknown, never false or safe. Opposite trusted evidence is Conflict, not last-writer-wins. Running out of evaluation budget returns Unknown. Author assertion alone cannot certify a safety-sensitive law.
- "All dependencies satisfy X" requires explicit, appropriately checked dependency-set completeness. Do not introduce negation-as-failure for open-world relations.
- Provenance retains premises, provider version/digest, rule, verifier and revision. Change of program, backend, provider, mapping or trust policy invalidates impacted proof closure until reverified.
- A transformation declares `requires/produces/preserves/invalidates` obligations and an observation model: values, exceptions/traps, heap/side effects, overflow/FP behavior, ordering, target. Only all supported and trusted premises allow legality; Unknown means retain original code/guard.
- Feasibility is hard, cost is secondary; optimization profitability is distinct from semantic legality. The selected runtime must not rediscover global composition decisions.
- Generators, LSP, JSON/MCP tools and agent interaction are **consumers** of typed services, not semantic truth owners. A machine-parseable schema does not authorize a runtime mutation.

## Worked held-out case

`ShapeAnalysis` knows `len(a)`; `RangeAnalysis` proves `0 ≤ i < len(a)` on an exact control-flow path; unchanged `BoundsOptimization` asks `SafeIndex(a,i,path,rev)`. Only verified signedness, overflow policy, array-length stability, alias/effect facts, and scope matching permit removing `Read(a,i)` bounds check. New provider may increase precision **without modifying original consumer**. Array mutation, stale artifact or changing implementation identity revokes the proof; the check remains.

## Architectural alternative and promotion rule

Start with direct typed C# producer/consumer interfaces. Add an optional selected-world evidence service **only** if independent providers/consumers need shared typed facts and exact revocation without pairwise dependencies. Add a finite monotone Horn/Datalog-like derivation kernel only after real nontrivial transitive needs. Refuse a second planner, global mutable ontology, arbitrary SSA requirement, opaque soundness claims, in-process "sandbox" or agent-selected executable configuration.
