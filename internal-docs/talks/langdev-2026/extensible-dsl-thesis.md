---
title: Extensible DSL motivation thesis
description: Research-backed motivation for extensible language families, shared compiler infrastructure and evolutionary design.
audience: LangDev 2026 speaker, maintainers
status: research-note
researchDate: 2026-09-07
sourceRevision: 40117eb68c630f7129c120aaaadc69be8f4ecbfb
---

# Extensible DSL motivation thesis

## Executive conclusion

The defensible motivation for UniversalToolchain is **not** that every DSL should be extensible.

For one stable language, with one team, a known feature set and a fixed execution pipeline, a monolithic compiler or an ordinary library/plugin architecture may be simpler, cheaper and safer.

Extensible language architecture becomes economically and technically interesting when the object being maintained is no longer one language but an **evolving family of related languages** that share most syntax/semantics/compiler infrastructure while differing in domain concepts, constraints, tooling, lowerings, optimizations or backends.

A concise thesis for the talk is:

> UniversalToolchain explores how to reduce the marginal cost of evolving a family of programming languages without forking their compiler infrastructure.

An even shorter stage formulation is:

> We are not only trying to make the first DSL cheaper. We are trying to make the next version, the next dialect and the next backend cheaper without cloning the compiler.

This is a research/architecture thesis, not a measured superiority claim.

## Why the problem is real

Empirical evidence says DSL evolution is normal rather than exceptional.

Borum and Seidl's practitioner survey of DSL authors reports that 86% of respondents experienced language evolution. The paper also reports that 63% were affected by causes outside the maintainer's control, including changes in the application domain, implementation technologies or external technologies; 17% had introduced breaking updates; and 13 of 21 respondents answering the corresponding question considered evolution important or vital to DSL success. The most common user-visible form of evolution was adding language constructs or syntactic sugar.

A later large-scale empirical study by Zhang, Strüber and Hebig identified 1002 GitHub repositories containing Xtext-related projects and manually classified 226 as fully developed languages. The authors report frequent updates to grammar definitions and example instances and explicitly study evolution and co-evolution of related artifacts.

The issue is therefore not merely "we can imagine that a DSL may change". Language evolution and its downstream effects are observed in both practitioner surveys and repository data.

## The causal chain

The motivation should be presented as a conditional chain rather than a universal claim.

```text
A domain or its users change
        ↓
The DSL acquires new concepts / constraints / workflows
        ↓
Different users need different subsets or variants
        ↓
One language starts behaving like a language family
        ↓
Fork-and-own or a growing monolith duplicates / couples language assets
        ↓
Reusable language features become valuable
        ↓
Independently authored features create global composition decisions
        ↓
A planner/LanguagePlan can own those decisions explicitly
        ↓
All selected variants lower into shared semantic/IR contracts
        ↓
Optimizer/backend improvements can benefit many variants
```

Every arrow must be kept conditional. A fixed DSL can stop before the "language family" step and remain a perfectly reasonable design.

## What extensibility buys

### 1. Reuse across language variants

The strongest reuse target is not source files but linguistic assets: syntax, semantics, static constraints, lowering rules, tooling and domain capabilities.

Research on linguistic reuse explicitly compares reusable composition mechanisms against clone-and-own. Bertolotti, Cazzola and Favalli show that clone-and-own negatively affects source design quality and that lack of explicit reuse increases duplication and coupling. Their evaluation uses the evolution of an ECMAScript implementation as a concrete case.

This supports the claim that a language workbench can have value when a new language feature substantially overlaps an existing one and should inherit future fixes rather than clone them.

### 2. Controlled language families instead of compiler forks

The useful abstraction is not "a dialect because dialects are cool". A dialect is one selected configuration of a language family for a particular audience or problem.

Examples:

```text
BioCore + Spatial
BioCore + Stochastic
BioCore + Spatial + ParameterFitting
BioCore + ExperimentData + GPU
```

If those variants share most compiler infrastructure, rebuilding each one as an independent compiler creates repeated ownership and change-propagation work.

### 3. Explicit ownership of global composition

Once features are independently owned, local declarations are no longer enough to determine a complete executable language.

A feature may declare dependencies, conflicts, provided capabilities, exclusions, ordering constraints or artifact transformations. The global language still needs a single authority to decide which providers and routes form the selected composition.

At the current UniversalToolchain source revision, `LanguageCompiler` owns feature closure, contribution/capability-provider resolution, ordering and artifact routes, producing one immutable `LanguagePlan`; `LanguageRuntime` materializes that plan instead of performing a second global selection.

This gives a more substantive answer to "why LanguagePlan?":

> When extension authors own local facts, somebody still has to own the global decision.

The planner makes that decision explicit and inspectable. It does not prove semantic compatibility.

### 4. Shared IR makes downstream improvements reusable

Shared IR is a distinct benefit from source-language extensibility.

A fixed DSL can also lower to a shared IR and benefit from optimizer/backend improvements. Therefore SSA, vectorization, superoptimization or a new backend are **not proof that source DSLs need to be extensible**.

The combined architecture is valuable because it separates two reuse axes:

```text
language-feature reuse  → cheaper language evolution/variants
shared IR reuse         → cheaper optimizer/backend evolution
```

Together:

```text
modules → LanguagePlan → shared semantic/IR pipeline → optimizers → backends
   ↑                                                        ↓
cheap variants                                      shared performance wins
```

MLIR is a useful analogy for the second half: its dialect mechanism allows multiple dialects to coexist and its infrastructure converts between dialects. It is evidence that preserving multiple abstraction levels and sharing transformation infrastructure is useful, not evidence that every source DSL should be a UniversalToolchain-style module graph.

## The scientific-computing scenario

The scientific example is plausible when framed as a **scenario**, not as evidence that bioinformatics specifically requires extensible DSLs.

Assume a team has a computational core for a mathematical model: solvers, differentiation, optimization and CPU/GPU execution. Scientists should manipulate the model through high-level domain concepts rather than rewrite solver code.

The first DSL may be small. Later different research groups request spatial behavior, stochastic behavior, experimental-data fitting, uncertainty models or different notations. At that point the project must choose among:

1. keep adding everything to one monolithic language;
2. fork separate compilers;
3. keep one core and represent variable language slices as composable features.

The third option is where extensible programming is worth evaluating.

Firedrake/UFL is strong evidence for a nearby but narrower proposition: scientific users can work at a high mathematical abstraction while compiler/numerical-specialist contributions add functionality or improve performance, and existing applications can automatically benefit from new optimizations. Firedrake does **not** by itself prove the need for extensible source-language syntax.

## Relationship to Extreme Programming / evolutionary design

XP should not be used as "proof" that an extensible framework should be designed up front. That would conflict with simple design and YAGNI.

The compatible relationship is the reverse:

```text
iteration 1: build the smallest useful DSL
iteration 2: add the first real domain change
iteration 3: a second independent variant appears
             ↓
refactor recurring variability into reusable language features
             ↓
make composition explicit only when distributed ownership creates real pressure
```

Testing, continuous integration and refactoring provide the safety net for extracting stable extension boundaries from repeated change.

So the XP-compatible claim is:

> Extensibility should be earned by observed variability, not guessed in advance.

## Strongest baseline and break-even criterion

The strongest alternative is:

> one stable DSL + ordinary libraries/plugins + a shared IR/backend.

UniversalToolchain earns its additional complexity only when ordinary extension mechanisms stop being sufficient.

Signals that the break-even point may have been reached:

- several independently evolving language variants share substantial assets;
- a feature needs its own syntax, static constraints/type rules, lowering and IDE/tool behavior rather than only callable library code;
- independent packages provide alternative capabilities or routes requiring global resolution;
- fork-and-own or conditional-heavy compiler code begins duplicating language behavior;
- reproducibility requires an explicit record of what composition was selected;
- testing needs to reason about configurations, not only one canonical pipeline.

Signals that extensibility is probably unnecessary:

- one team owns one stable language;
- changes are primarily new library functions or runtime data;
- there is no meaningful variant/configuration pressure;
- a handwritten pipeline remains clearer than a planner;
- the cost of maintaining extension contracts exceeds observed reuse.

## Important limitations

UniversalToolchain must not claim that modularity eliminates the hard parts of language evolution.

- Composition can create feature interactions.
- Independently correct modules are not automatically correct in every combination.
- Structural compatibility and deterministic planning do not prove semantic compatibility.
- The configuration space may grow combinatorially.
- Language migrations and old source/model compatibility remain separate problems.
- Shared IR can erase useful high-level information if lowering happens too early.
- Extension points add API and compatibility costs of their own.

The present PlanFuzz `extension-noninterference` and plan-determinism oracles are relevant research mechanisms for these risks, but they are experimental and are not a proof of general safety or superiority.

## Resulting positioning for LangDev

Avoid:

> DSLs should be extensible.

Prefer:

> A fixed DSL is often the right answer. The difficult case starts when the DSL itself becomes a product line: several groups need related but different language capabilities, while we want fixes, tooling and compiler improvements to remain shared.

Then UniversalToolchain can be introduced as one attempt to make this evolution explicit:

> Keep local knowledge local. Make global language-composition decisions explicit. Reuse the compiler pipeline after composition instead of cloning it for each variant.

## Core sources

- Holger Stadel Borum, Christoph Seidl. *Survey of Established Practices in the Life Cycle of Domain-Specific Languages*. MODELS 2022. DOI: 10.1145/3550355.3552413. https://pure.itu.dk/en/publications/survey-of-established-practices-in-the-life-cycle-of-domain-speci/
- Weixing Zhang, Daniel Strüber, Regina Hebig. *Development and evolution of Xtext-based DSLs on GitHub: an empirical investigation*. Empirical Software Engineering 31, 48 (2026). DOI: 10.1007/s10664-025-10775-2. https://link.springer.com/article/10.1007/s10664-025-10775-2
- Francesco Bertolotti, Walter Cazzola, Ludovico Favalli. *On the granularity of linguistic reuse*. Journal of Systems and Software 202 (2023), 111704. DOI: 10.1016/j.jss.2023.111704. https://www.sciencedirect.com/science/article/pii/S0164121223000997
- Juha-Pekka Tolvanen et al. *A framework for evaluating tool support for co-evolution of modeling languages, tools and models*. Software and Systems Modeling 24 (2025), 311–338. DOI: 10.1007/s10270-024-01218-5. https://link.springer.com/article/10.1007/s10270-024-01218-5
- Markus Völter, Arie van Deursen, Bernd Kolb, Stephan Eberle. *Using C language extensions for developing embedded software: A case study*. OOPSLA 2015. DOI: 10.1145/2814270.2814276. https://research.tudelft.nl/en/publications/using-c-language-extensions-for-developing-embedded-software-a-ca
- JetBrains MPS documentation: language extensions vs libraries. https://www.jetbrains.com/help/mps/mps-faq.html
- JetBrains MPS documentation: language migrations. https://www.jetbrains.com/help/mps/migrations.html
- MLIR Language Reference: dialects. https://mlir.llvm.org/docs/LangRef/
- Florian Rathgeber et al. *Firedrake: automating the finite element method by composing abstractions*. ACM TOMS 43(3), 2017. DOI: 10.1145/2998441. https://arxiv.org/abs/1501.01809
- Martin Fowler. *Is Design Dead?* https://martinfowler.com/articles/designDead.html
