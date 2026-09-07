---
title: LangDev extensible DSL claim map
description: Presentation-safe claims, evidence levels, counterarguments and wording for the extensible-language motivation.
audience: LangDev 2026 speaker
status: research-claim-map
researchDate: 2026-09-07
sourceRevision: 40117eb68c630f7129c120aaaadc69be8f4ecbfb
---

# LangDev extensible DSL claim map

## One-sentence thesis

> A fixed DSL can be the right answer. UniversalToolchain becomes interesting when that DSL starts turning into a family of independently evolving language variants and we want to share language features and compiler infrastructure instead of cloning the compiler.

## Strongest public claims

### Claim A — DSL evolution is a real engineering event

**Say:**

> DSLs do not necessarily freeze after version one. A practitioner survey found language evolution in 86% of respondents, and a large Xtext study observed frequent grammar/instance updates across real repositories.

**Evidence:**

- Borum & Seidl, MODELS 2022, DOI 10.1145/3550355.3552413.
- Zhang, Strüber & Hebig, Empirical Software Engineering 2026, DOI 10.1007/s10664-025-10775-2.

**Confidence:** high.

### Claim B — changing a DSL can force other artifacts to change

**Say:**

> Language evolution is not only a parser change. Existing programs/models, transformations, editors and other tooling may have to co-evolve.

**Evidence:**

- Mengerink et al., large-scale industrial DSL repository, MODELSWARD 2018.
- Tolvanen et al., co-evolution framework and workbench evaluation.
- JetBrains MPS migration/branching documentation.

**Confidence:** high.

### Claim C — copying language features has maintenance costs

**Say:**

> Fork-and-own is a valid baseline, but explicit linguistic reuse can avoid duplication and change-propagation costs.

**Evidence:**

- Bertolotti, Cazzola & Favalli, JSS 2023: explicit reuse mechanisms compared with clone-and-own using ECMAScript evolution scenarios; clone-and-own worsened code-quality measures through duplication/coupling.

**Confidence:** high for the studied mechanisms; medium for generalizing to all compiler architectures.

### Claim D — language extensions have been useful outside toy examples

**Say:**

> Domain-specific language extensions have been used in commercial embedded development. In the mbeddr SmartMeter case, extensions such as physical units and state machines were reported to help manage complexity and improve testability.

**Evidence:**

- Völter et al., OOPSLA 2015, DOI 10.1145/2814270.2814276.

**Confidence:** high for the case; do not generalize to all projects.

### Claim E — libraries are still the default competitor

**Say:**

> If a library is enough, use a library. Language extension earns its cost when the new concept needs language-level syntax, static semantics, lowering or tooling, or when independently evolving variants need composition.

**Evidence:**

- JetBrains MPS FAQ explicitly distinguishes extensions from libraries by syntax, constraints/type system and IDE integration.

**Confidence:** high as engineering guidance, not a theorem.

### Claim F — shared compiler infrastructure is a separate win

**Say:**

> Once variants converge on shared semantic/IR contracts, downstream compiler work can be reused across them. That is a different benefit from making the source language extensible.

**Evidence/analogy:**

- Firedrake/UFL: domain-level scientific abstraction plus automatically inherited optimizations.
- MLIR: multiple dialects coexist and share conversion/transformation infrastructure.

**Confidence:** high for the general compiler pattern; medium as analogy to UT.

### Claim G — LanguagePlan addresses global ownership, not arbitrary semantics

**Say:**

> Local modules can declare dependencies, capabilities, conflicts and transformations. The global composition still needs one owner. In UT, `LanguageCompiler` resolves the declared structure into one immutable `LanguagePlan`; runtime executes that selected graph rather than rediscovering it.

**Evidence:** current UniversalToolchain source/documentation at `40117eb68c630f7129c120aaaadc69be8f4ecbfb`.

**Confidence:** high as an implementation statement.

**Do not add:** "therefore the plan proves the modules are semantically compatible."

## Claims to avoid

| Unsafe claim | Why unsafe | Replacement |
| --- | --- | --- |
| "DSLs should be extensible." | Counterexample: many stable DSLs do fine as fixed languages. | "Extensibility is useful under recurring language variability." |
| "Bioinformatics needs extensible DSLs." | We have a scenario, not domain-wide evidence. | "Computational science gives a plausible scenario; UFL/Firedrake proves the nearby shared-abstraction benefit." |
| "Modules eliminate regression risk." | Feature interactions remain. | "Modules isolate ownership; compatibility still requires tests/contracts/oracles." |
| "Tested modules are safe in every combination." | Configuration space and semantic interference remain. | "Known combinations can be tested; explicit plans make configurations observable." |
| "LanguagePlan proves correctness." | Planner checks represented contracts, not arbitrary semantics. | "LanguagePlan proves selected declared structure and reproducibility of that selection." |
| "SSA makes extensible DSLs worthwhile." | A fixed DSL can share an IR too. | "SSA demonstrates the separate downstream reuse benefit of shared IR." |
| "XP tells us to design plugin architecture early." | Conflicts with YAGNI/simple design. | "XP supports extracting extension points after repeated real variability appears." |
| "UT is cheaper than MPS/Xtext/handwritten compilers." | No comparative experiment. | "UT explores a specific composition/ownership model on .NET." |
| "UT solves language migration." | Current planner does not migrate source/models. | "Migration is an independent evolution problem and a useful limitation to acknowledge." |

## Recommended motivation sequence for the deck

### 1. Start with the strongest baseline

> Suppose we need one DSL. We should probably just build one DSL.

This removes the obvious "architecture astronaut" objection before introducing UT.

### 2. Introduce evolution evidence

Show one or two numbers rather than a literature wall:

- **86%** of surveyed DSL authors reported evolution;
- **63%** reported external evolution drivers;
- **17%** had breaking updates.

Source in speaker notes: Borum & Seidl, MODELS 2022.

Speaker line:

> The interesting problem starts after the first language works.

### 3. Turn one DSL into a family

Use the scientific scenario:

```text
BioCore
  ├─ Spatial
  ├─ Stochastic
  └─ Fitting
```

Ask:

> Do we put everything into one compiler, fork three compilers, or share the language pieces that actually overlap?

### 4. Show that fork-and-own is not a straw man

Use the JSS 2023 result on clone-and-own: language evolution can create duplicated components and propagation costs; explicit reuse mechanisms were evaluated precisely against that baseline.

Speaker line:

> Extensibility is not valuable because copying is impossible. It is valuable when copying stops being cheap.

### 5. Introduce the new problem created by modularity

```text
local features
    ↓
??? global dependencies / conflicts / providers / order / routes ???
```

Then:

```text
LanguageCompiler → LanguagePlan
```

Speaker line:

> Once nobody owns the entire language locally, global decisions need an explicit owner.

### 6. Separate language evolution from compiler evolution

```text
features → LanguagePlan → shared IR → optimizers/backends
```

Put two labels on the diagram:

- **above plan:** reuse while the language evolves;
- **below plan:** reuse while the compiler evolves.

This is where SSA/optimization/new backends belong.

### 7. Close the loop with evolutionary design

> We should not predict every module. Start with the smallest DSL; when a second or third real variant appears, refactor the repeated variability into explicit language features.

That makes XP a development discipline rather than a decorative acronym.

## Compact evidence slide candidate

If only one slide is available for external validation, use four cells:

**DSLs evolve**

> 86% of surveyed DSL authors reported evolution.

Borum & Seidl, MODELS 2022.

**Evolution propagates**

> Industrial and workbench studies report co-evolution across models, transformations, editors and language definitions.

Mengerink et al. 2018; Tolvanen et al. 2025.

**Copying has a cost**

> Clone-and-own increased duplication/coupling in a language-evolution study based on ECMAScript features.

Bertolotti et al., JSS 2023.

**Extensions have been used commercially**

> mbeddr's SmartMeter case used domain-specific C extensions and reported complexity/testability benefits.

Völter et al., OOPSLA 2015.

The point of the slide is only:

> This problem family exists.

It must not claim:

> These papers prove UniversalToolchain solves it better.

## Hostile Q&A additions

### Why not a library?

For a feature that is naturally a runtime API, a library is probably better. Language extensions become interesting when the concept needs syntax/static semantics/tooling/lowering or when language configurations themselves vary. Even MPS's own documentation frames the distinction this way.

### Why not one big DSL?

That remains a strong baseline. A monolith wins while one owner and one surface remain coherent. The extensible architecture earns its cost when different users need independently evolving combinations and duplicated compiler changes become material.

### Why not fork the DSL?

Forking is simple and sometimes correct. The problem is change propagation once variants share most linguistic assets. We can cite the Neverlang/ECMAScript reuse study as direct evidence that clone-and-own can increase duplication and coupling.

### Doesn't modularity create a 2^N testing problem?

Yes. UT does not eliminate feature interactions or the configuration state space. Explicit plans make configurations representable and therefore easier to sample, replay and compare. PlanFuzz is experimental work in that direction, not a proof of exhaustive safety.

### Is the scientific example real?

The exact BioCore/Spatial/Fitting example is a scenario. The nearby architecture is real: Firedrake/UFL separates scientific expressions from compiler/numerical optimization work; mbeddr provides real project-specific language extensions; MLIR demonstrates dialect/shared-lowering infrastructure. The talk should state the boundary explicitly.

### Is this XP?

The XP-compatible part is evolutionary extraction: minimal first language, tests/CI, observe repeated variation, refactor. Designing dozens of hypothetical extension points on day one would be YAGNI, not a reason to cite XP.

## Source links

- https://pure.itu.dk/en/publications/survey-of-established-practices-in-the-life-cycle-of-domain-speci/
- https://link.springer.com/article/10.1007/s10664-025-10775-2
- https://link.springer.com/article/10.1007/s10270-024-01218-5
- https://research.tue.nl/en/publications/exploring-dsl-evolutionary-patterns-in-practice-a-study-of-dsl-ev/
- https://www.sciencedirect.com/science/article/pii/S0164121223000997
- https://research.tudelft.nl/en/publications/using-c-language-extensions-for-developing-embedded-software-a-ca
- https://www.jetbrains.com/help/mps/mps-faq.html
- https://www.jetbrains.com/help/mps/migrations.html
- https://www.jetbrains.com/help/mps/using-migrations-with-branching.html
- https://arxiv.org/abs/1501.01809
- https://mlir.llvm.org/docs/LangRef/
- https://martinfowler.com/articles/designDead.html
