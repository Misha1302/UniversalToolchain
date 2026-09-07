---
title: Extensible DSL evidence dossier
description: Claim-to-evidence map for DSL evolution, language families, modular reuse and shared compiler infrastructure.
audience: LangDev 2026 speaker, maintainers
status: research-evidence
evidenceDate: 2026-09-07
sourceRevision: 40117eb68c630f7129c120aaaadc69be8f4ecbfb
---

# Extensible DSL evidence dossier

## Purpose

This file separates external evidence from UniversalToolchain interpretation.

Evidence is graded as:

- **A — direct/strong:** empirical study, industrial case study, large repository study, or official product documentation describing an actual engineering constraint;
- **B — supporting:** peer-reviewed architectural work or mature project documentation showing the same design pressure;
- **C — analogy:** a neighboring system demonstrates a useful mechanism but does not prove the complete UniversalToolchain thesis.

No source below proves that UniversalToolchain is superior to a monolithic DSL or another language workbench. The evidence supports that the underlying problems exist and that modularity/composition/shared infrastructure are established responses.

## Claim 1 — DSLs actually evolve after launch

**Evidence strength: A**

### Practitioner survey: Borum & Seidl, MODELS 2022

Survey of DSL authors covering design/development, launch, evolution and end of life.

Reported results include:

- 86% of respondents reported language evolution;
- 63% were affected by causes outside the maintainer's control, including changes to the application domain, implementation technologies or external technologies;
- 17% of DSL creators had introduced breaking updates;
- 13 of 21 respondents answering the success question considered evolution important or vital to DSL success;
- adding language constructs or syntactic sugar was the most common user-visible form of evolution.

This is the strongest evidence for the basic premise that "the first DSL is not necessarily the final DSL".

Source:

- Holger Stadel Borum, Christoph Seidl. *Survey of Established Practices in the Life Cycle of Domain-Specific Languages*. MODELS 2022.
- DOI: 10.1145/3550355.3552413
- https://pure.itu.dk/en/publications/survey-of-established-practices-in-the-life-cycle-of-domain-speci/
- Open paper: https://pure.itu.dk/files/90366487/main.pdf

**Safe inference for UT:** design for evolution can be a legitimate concern.

**Not supported:** every DSL needs a general-purpose extension ecosystem.

## Claim 2 — evolution is visible in real open-source language repositories

**Evidence strength: A**

### 1002 Xtext repositories: Zhang, Strüber & Hebig

The study systematically identified 1002 GitHub repositories containing Xtext-related projects and manually classified 226 as containing fully developed languages. It studies grammar/front-end evolution and co-evolution of related artifacts. The authors report frequent updates to grammar definitions and example instances and a substantial amount of perfective evolution.

Source:

- Weixing Zhang, Daniel Strüber, Regina Hebig. *Development and evolution of Xtext-based DSLs on GitHub: an empirical investigation*. Empirical Software Engineering 31, 48 (2026).
- DOI: 10.1007/s10664-025-10775-2
- https://link.springer.com/article/10.1007/s10664-025-10775-2
- Preprint: https://arxiv.org/abs/2501.19222

**Safe inference for UT:** language evolution is observable in repository histories, not only recalled in interviews.

**Not supported:** those projects would have been better if rewritten using UT-style composition.

## Claim 3 — language evolution creates a co-evolution problem

**Evidence strength: A**

### Tool-workbench evaluation: Tolvanen et al.

This work states that language refinement/enhancement can require more maintenance work than initial development and that modeling languages have the extra problem of keeping language definitions, tools and existing models synchronized. It explicitly reports that language-workbench users have experienced co-evolution problems.

The evaluated impact spans abstract syntax, concrete syntax, constraints, tools, semantics/generators and existing models. Outcomes in evaluated tools range from substantial automation to manual intervention and cases where editors do not open correctly.

Source:

- Juha-Pekka Tolvanen, Steven Kelly, Juri Di Rocco, Alfonso Pierantonio, Giordano Tinella. *A framework for evaluating tool support for co-evolution of modeling languages, tools and models*. Software and Systems Modeling 24 (2025), 311–338.
- DOI: 10.1007/s10270-024-01218-5
- https://link.springer.com/article/10.1007/s10270-024-01218-5

### Official MPS migration documentation

JetBrains' MPS documentation describes the concrete compatibility problem after a language has users: removing concepts or changing properties/children/references can make existing user models no longer conform to the new language definition. MPS therefore versions languages and supports migration scripts.

Its branching documentation goes further: teams using multiple branches inevitably encounter different language versions across branches, making merges more difficult; their recommended workflow synchronizes language versions and migrations before merging.

Sources:

- https://www.jetbrains.com/help/mps/migrations.html
- https://www.jetbrains.com/help/mps/using-migrations-with-branching.html

**Safe inference for UT:** evolution is broader than "change the parser"; compatibility, tooling and existing programs/models can all become maintenance owners.

**Not supported:** UT's current planner solves source migration. It does not.

## Claim 4 — costly co-evolution occurs in an industrial DSL repository

**Evidence strength: A**

### Mengerink et al., MODELSWARD 2018

This peer-reviewed study examines evolutionary scenarios in a large-scale industrial DSL repository. The abstract states that when domain languages evolve, they may trigger co-evolution in models, model-to-model transformations, graphical/textual editors and other dependent artifacts, and characterizes this co-evolution as tedious and potentially very costly. The work also notes limits to fully automatic co-evolution.

One author, Ramon Schiffelers, was affiliated with ASML in the publication.

Source:

- J. G. M. Mengerink, B. van der Sanden, B. C. M. Cappers, A. Serebrenik, R. R. H. Schiffelers, M. G. J. van den Brand. *Exploring DSL Evolutionary Patterns in Practice: A Study of DSL Evolution in a Large-scale Industrial DSL Repository*. MODELSWARD 2018, pp. 446–453.
- DOI: 10.5220/0006605804460453
- https://research.tue.nl/en/publications/exploring-dsl-evolutionary-patterns-in-practice-a-study-of-dsl-ev/

**Safe inference for UT:** downstream synchronization costs are observed in industrial language engineering.

**Not supported:** the studied repository used or required UniversalToolchain's exact planning model.

## Claim 5 — clone-and-own is a real language-maintenance problem

**Evidence strength: A**

### Bertolotti, Cazzola & Favalli, JSS 2023

The paper compares explicit language-reuse mechanisms against clone-and-own in Neverlang using real ECMAScript evolution scenarios. It reports that clone-and-own negatively affects source design quality. The paper identifies increased duplication and coupling when linguistic reuse is not explicitly supported and explains that duplicated language components become costly to maintain because fixes/changes must propagate across variants.

The experimental case extends an ECMAScript 3 implementation with features introduced by later ECMAScript revisions.

Source:

- Francesco Bertolotti, Walter Cazzola, Ludovico Favalli. *On the granularity of linguistic reuse*. Journal of Systems and Software 202 (2023), 111704.
- DOI: 10.1016/j.jss.2023.111704
- https://www.sciencedirect.com/science/article/pii/S0164121223000997

**Safe inference for UT:** explicit mechanisms for reusing language semantics/features can be preferable to copying implementations as a language evolves.

**Not supported:** a general composition planner is always the best reuse mechanism.

## Claim 6 — reusable components are specifically used to build language variants/families

**Evidence strength: B**

### Butting et al., JOT 2023

The paper argues that modularization matters for reuse of DSMLs/language parts and develops cross-cutting language-component concepts for MagicDraw and MontiCore. It explicitly frames reusable language components as useful for developing variants or families of similar languages and for modular DSML development "in the large".

Source:

- Arvid Butting, Rohit Gupta, Nico Jansen, Nikolaus Regnat, Bernhard Rumpe. *Towards Modular Development of Reusable Language Components for Domain-Specific Modeling Languages in the MagicDraw and MontiCore Ecosystems*. Journal of Object Technology 22 (2023).
- https://www.jot.fm/contents/issue_2023_01/article4.html

### Robotics composition case

Wigand et al. describe robotics as requiring integration of multiple domain-specific software artifacts and identify interoperability, composability and reusability of DSLs/models as challenging. They introduce a modular language-composition approach using a workbench supporting reuse, extensibility and refinement.

Source:

- Dennis Leroy Wigand, Arne Nordmann, Michael Goerlich, Sebastian Wrede. *Modularization of Domain-Specific Languages for Extensible Component-Based Robotic Systems*. IEEE IRC 2017.
- DOI: 10.1109/IRC.2017.34
- https://doi.org/10.1109/IRC.2017.34

**Safe inference for UT:** "a family of related DSLs" is an established language-engineering problem formulation.

## Claim 7 — language extensions have delivered value in a real commercial project

**Evidence strength: A for the case, B for generalization**

### mbeddr smart-meter industrial case study

Völter, van Deursen, Kolb and Eberle report on developing commercial smart-meter embedded software with C plus domain-specific extensions such as components, physical units, state machines, registers and interrupts. The peer-reviewed case study reports that the extensions significantly helped manage complexity and improved testability, with low integration effort and no significant memory/performance overhead in the studied system.

The system used mbeddr, an extensible version of C based on JetBrains MPS. The mbeddr case-study page also states that the SmartMeter project used both built-in extensions and project-specific language extensions.

Sources:

- Markus Völter, Arie van Deursen, Bernd Kolb, Stephan Eberle. *Using C Language Extensions for Developing Embedded Software: A Case Study*. OOPSLA 2015.
- DOI: 10.1145/2814270.2814276
- https://research.tudelft.nl/en/publications/using-c-language-extensions-for-developing-embedded-software-a-ca
- mbeddr case-study index: https://mbeddr.com/learn.html

**Safe inference for UT:** modular/domain-specific language extension is not only a toy or academic mechanism; it has been used in commercial embedded development.

**Not supported:** mbeddr's success proves UT's architecture or performance.

## Claim 8 — extensions are not automatically preferable to libraries

**Evidence strength: A as official engineering guidance**

JetBrains MPS explicitly addresses "Why extend a language? Aren't libraries good enough?" Its answer distinguishes language extensions by capabilities that ordinary libraries do not naturally provide: custom syntax, static constraints/type-system rules, IDE integration and compile-time transformations.

Source:

- https://www.jetbrains.com/help/mps/mps-faq.html

**Safe inference for UT:** the presentation should not use "we need one more function" as justification for a language extension. Extension becomes more defensible when the new concept is genuinely linguistic or when language-level tooling/semantics are required.

This source is especially useful because it strengthens the alternative baseline instead of advertising extensibility unconditionally.

## Claim 9 — shared scientific abstractions can let downstream optimizer work benefit domain users

**Evidence strength: A for shared-abstraction benefit; C for source-language extensibility**

### UFL/Firedrake

Firedrake uses the FEniCS UFL domain-specific language for finite-element formulations and composes abstractions across scientific computing. The authors emphasize separation of concerns between application scientists, numerical analysts and computer scientists; contributions can add functionality or improve performance, and Firedrake automatically benefits from new optimizations.

Source:

- Florian Rathgeber et al. *Firedrake: automating the finite element method by composing abstractions*. ACM Transactions on Mathematical Software 43(3), 2017.
- DOI: 10.1145/2998441
- https://arxiv.org/abs/1501.01809

**Safe inference for UT:** a stable high-level scientific interface plus shared compiler/numerical infrastructure can create cross-user optimization benefits.

**Not supported:** UFL is evidence that scientific DSL syntax itself must be extensible.

## Claim 10 — multiple dialects and progressive lowering are an established compiler architecture

**Evidence strength: B/C**

MLIR's official language reference defines dialects as its extension mechanism. Multiple dialects, including out-of-tree dialects, can coexist in a module and passes can produce/consume them; the framework supports conversion within and between dialects.

Source:

- MLIR Language Reference: https://mlir.llvm.org/docs/LangRef/

This supports the shared-IR/progressive-lowering half of the story: preserve domain-specific structure long enough to transform it, then reuse downstream compiler infrastructure.

**Not supported:** MLIR dialect composition is equivalent to UniversalToolchain source-language feature composition.

## Claim 11 — XP supports evolutionary extraction, not speculative framework building

**Evidence strength: B**

Martin Fowler's discussion of XP/evolutionary design emphasizes refactoring, testing and continuous integration as the mechanisms that let design evolve. The YAGNI/simple-design position argues against building general flexibility solely for hypothetical future requirements.

Sources:

- Martin Fowler, *Is Design Dead?* https://martinfowler.com/articles/designDead.html
- Martin Fowler, *Yagni*. https://martinfowler.com/bliki/Yagni.html
- Agile Alliance, *Simple Design*. https://agilealliance.org/glossary/simple-design/

**Safe inference for UT:** an XP-compatible story is "start with the smallest useful language; extract reusable extension boundaries when repeated real variants appear".

**Not supported:** XP says to build an extensible language workbench in advance.

## Evidence matrix for presentation claims

| Claim | Strength | Best evidence | Presentation-safe wording |
| --- | --- | --- | --- |
| DSLs evolve in practice | A | Borum & Seidl; Zhang et al. | "DSL evolution is common enough to be a first-class engineering concern." |
| Evolution affects more than grammar | A | Tolvanen et al.; MPS migrations; Mengerink et al. | "Changing a language can force tools, models/programs and transformations to co-evolve." |
| Clone-and-own has maintenance costs | A | Bertolotti et al. | "Copying a language feature creates duplication and change-propagation costs." |
| Language families/variants are a recognized problem | B | Butting et al.; Wigand et al. | "Language engineering research explicitly studies reusable components for families of related DSLs." |
| Extensible languages can work commercially | A/B | OOPSLA mbeddr SmartMeter | "A commercial embedded case reported benefits from domain-specific C extensions." |
| Libraries remain the baseline | A guidance | MPS FAQ | "Use a library when a library is enough; language extensions earn their cost when the concept is genuinely linguistic." |
| Shared compiler improvements can benefit domain users | A/C | Firedrake | "High-level scientific abstractions can decouple domain users from optimization work." |
| Shared dialect/IR infrastructure is established | B/C | MLIR | "Compiler ecosystems already use dialects and shared conversion infrastructure to preserve and lower multiple abstraction levels." |
| XP justifies incremental extraction | B | Fowler/Agile Alliance | "Do not predict all extensions; refactor extension points out of observed change." |

## Evidence gaps that remain

The following would require new experiments or user studies before being claimed:

- that UniversalToolchain reduces person-hours compared with Xtext/MPS/Spoofax/handwritten compilers;
- that LanguagePlan reduces defects in real multi-team language development;
- a numeric break-even point for number of features, variants or teams;
- performance overhead of planning at ecosystem scale;
- whether scientists prefer independently composable DSL features over one curated fixed DSL;
- whether UT's wrappers/mediators measurably reduce feature-interaction defects;
- whether PlanFuzz outperforms ordinary fuzzing/pairwise configuration tests at equal budget.

These are research opportunities, not current evidence.
