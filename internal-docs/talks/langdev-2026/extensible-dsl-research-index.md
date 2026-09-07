---
title: Extensible DSL research pack index
description: Entry point for the LangDev 2026 research on DSL evolution and extensible programming motivation.
audience: LangDev 2026 speaker, maintainers
status: research-index
researchDate: 2026-09-07
sourceRevision: 40117eb68c630f7129c120aaaadc69be8f4ecbfb
---

# Extensible DSL research pack

This directory contains the evidence-backed motivation work produced on 2026-09-07 for the LangDev 2026 narrative.

## Files

- [Motivation thesis](./extensible-dsl-thesis.md) — the combined architectural conclusion, strongest baseline, break-even criterion, relationship to shared IR and XP.
- [Evidence dossier](./extensible-dsl-evidence.md) — claim-to-evidence mapping, empirical/industrial sources, evidence strength and explicit non-claims.
- [Scientific DSL scenario](./scientific-dsl-evolution-case-study.md) — a careful computational-science scenario that separates shared scientific compiler infrastructure from the stronger source-language-extensibility claim.
- [Presentation claim map](./extensible-dsl-claim-map.md) — safe/unsafe talk wording, evidence slide candidate, narrative order and hostile Q&A additions.

## Main result

The research does **not** support the universal claim that DSLs should be extensible.

It supports a narrower and stronger motivation:

> Extensible language architecture becomes useful when a DSL evolves into a family of related variants and maintaining those variants through a monolith or clone-and-own starts duplicating linguistic/compiler assets. At that point explicit feature reuse and global composition ownership become engineering problems worth solving.

The current UniversalToolchain mapping is:

```text
independently owned features/contributions
              ↓
       LanguageCompiler
              ↓
        LanguagePlan
              ↓
     shared semantic / IR route
              ↓
      optimizers / backends
```

The planner is evidence of the project's chosen solution to structural composition ownership. External sources establish the surrounding problem class; they do not prove UniversalToolchain superiority.

## Evidence boundary

The pack intentionally distinguishes:

- empirical evidence that DSL evolution/co-evolution occurs;
- evidence that clone-and-own can create maintenance costs;
- industrial evidence that language extensions have been useful;
- analogies for shared scientific/compiler infrastructure;
- UniversalToolchain-specific architectural interpretation;
- open questions that still require comparative experiments.

Use these files as research notes and presentation evidence, not as a claim that the current alpha implementation has solved language evolution in general.
