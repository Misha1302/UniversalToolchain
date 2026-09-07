---
title: Scientific DSL evolution case study
subtitle: A presentation scenario for separating domain experimentation, language evolution and compiler evolution.
audience: LangDev 2026 speaker, maintainers
status: research-scenario
researchDate: 2026-09-07
sourceRevision: 40117eb68c630f7129c120aaaadc69be8f4ecbfb
---

# Scientific DSL evolution case study

## Why this file exists

The original motivating idea used a bioinformatics team: programmers implement difficult mathematical/numerical machinery; scientists then need convenient DSLs to experiment with the resulting model; changing research needs create pressure for variants and extensions.

The idea is useful, but it must be presented carefully.

There are two distinct claims:

1. **shared scientific DSL/IR infrastructure is useful** — strongly supported by systems such as UFL/Firedrake;
2. **the scientific DSL itself should be extensible/composable** — plausible in the proposed scenario, but not established merely by the existence of UFL/Firedrake.

The talk should use the scientific story as a concrete thought experiment and use independent DSL-evolution/language-composition evidence to justify extensibility.

## Scenario

Assume a computational-biology group develops a mathematical model and a numerical implementation for it.

The expensive, specialist-owned layer contains things such as:

- numerical solvers;
- automatic differentiation;
- parameter optimization;
- stochastic simulation kernels;
- sparse/tensor operations;
- CPU/GPU execution;
- domain invariants and validity checks.

Scientists should not rewrite this layer for every experiment. They need a domain-facing way to declare models, experiments and fitting tasks.

A minimal first DSL might conceptually look like:

```text
species A
species B

reaction A -> B rate k
parameter k = 0.1
```

At this point **a fixed monolithic DSL is enough**. Building a general extension system for hypothetical future requests would be premature.

## Evolution pressure

Research starts producing real, independent needs.

### Group 1: spatial model

```text
compartment nucleus
compartment cytoplasm

diffuse A from cytoplasm to nucleus
```

This may require more than a library function:

- new syntax/domain concepts;
- static restrictions on allowed entities;
- new semantic objects;
- new lowering to spatial solver operations;
- editor/tooling support.

### Group 2: stochastic kinetics

```text
reaction A -> B
    stochastic poisson
```

This may alter semantic interpretation and require a stochastic backend capability.

### Group 3: parameter fitting / experimental observations

```text
observe B from "experiment.csv"

fit k
    minimize mse
```

This introduces data bindings, objective functions, diagnostics and possibly differentiability requirements.

### Group 4: specialized hardware

The source language may remain unchanged while the desired route changes:

```text
same model
    ↓
GPU/vectorized backend
```

This is primarily a shared-IR/backend problem, not a source-language extension problem.

## Three implementation strategies

### Strategy A — grow one monolithic DSL

All capabilities enter one compiler.

```text
if spatial ...
if stochastic ...
if fitting ...
if gpu ...
```

This can be the best choice while there is one product owner and nearly all users want the same surface.

Risks appear when capabilities are orthogonal or owned independently:

- users receive concepts they do not need;
- conditionals spread across parser/binder/lowering/tooling;
- unrelated changes share one release schedule;
- feature interactions become implicit;
- changing one concern risks the entire compiler.

### Strategy B — fork a compiler for each research group

```text
BioSpatial
BioStochastic
BioFitting
BioSpatialFitting
...
```

This gives each team freedom but duplicates language assets and compiler infrastructure. Fixes and new backend improvements may need to propagate across forks.

This is the point where the clone-and-own evidence from language-engineering research becomes relevant.

### Strategy C — treat variability as language features

```text
BioCore
  + Spatial
  + Stochastic
  + ExperimentData
  + ParameterFitting
```

A concrete dialect is then a selected combination:

```text
BioCore + Spatial
BioCore + Stochastic
BioCore + Spatial + ParameterFitting
```

The goal is not arbitrary syntax macros. Each feature can own the minimum linguistic slice it actually needs:

```text
syntax
+ static constraints
+ semantic contribution
+ lowering
+ tooling metadata where supported
+ required/provided capabilities
```

This is the scenario in which UniversalToolchain-style composition is worth testing.

## Why LanguagePlan appears naturally

Suppose extension packages are independently developed.

`Spatial` might require a `Mesh` capability.

`ParameterFitting` might require a differentiable semantic route.

Two packages might provide different GPU lowering implementations.

`Stochastic` might conflict with a deterministic-only optimization.

No local module can safely choose the complete pipeline because the correct decision depends on the full requested configuration.

Hence:

```text
local declarations
    ↓
LanguageCompiler
    ↓
one global LanguagePlan
    ↓
exact runtime route
```

At the current UniversalToolchain revision, the generic contracts already model independently owned features/contributions, dependencies, conflicts, capabilities, exclusions, ordering and artifact routes. The architectural thesis is therefore not that the planner invents semantics; it resolves the **declared structural composition**.

Semantic compatibility still needs tests, specifications and domain-specific oracles.

## Why wrappers / mediators can make sense

A new feature often does not want to replace all old behavior. It wants to refine or surround an existing semantic operation.

Example:

```text
base reaction semantics
        ↓
stochastic extension adds distribution metadata / checks
        ↓
shared downstream reaction representation
```

or:

```text
base quantity expression
        ↓
units extension validates dimensions
        ↓
shared arithmetic lowering
```

Decorator/delegation-style contributions are valuable if they let an extension reuse future fixes from the underlying behavior instead of copying it.

The Neverlang linguistic-reuse study is relevant here: it compares explicit reuse/delegation-style mechanisms with clone-and-own and finds duplication/coupling disadvantages for the latter.

The hard problem is defining ordering, compatibility and observable effects of multiple wrappers. A planner can make order explicit, but cannot infer arbitrary semantic commutativity.

## What shared IR contributes

After a selected language has been resolved, variants should converge on shared semantic/IR contracts where possible.

```text
BioCore + Spatial ──────┐
BioCore + Stochastic ───┼→ semantic representation → IR → optimization → backend
BioCore + Fitting ──────┘
```

Then compiler engineers can improve:

- SSA conversion;
- common-subexpression elimination;
- vectorization;
- loop transformations;
- memory-layout transformations;
- target-specific lowering;
- selected superoptimization experiments;
- new CPU/GPU targets;

without rewriting each source frontend.

This benefit does not depend on the source language being extensible. It depends on stable shared boundaries. Extensibility adds value when there are *multiple evolving language surfaces* sharing those boundaries.

## Strong real analogies

### UFL / Firedrake — strong analogy for role separation

Firedrake uses UFL as a high-level finite-element DSL and composes several scientific-computing abstractions. Its published architecture emphasizes separation between application scientists, numerical analysts and computer scientists. Contributions can add functionality or improve performance, and the system automatically benefits from new optimizations.

This is very close to the desired story:

```text
scientist expresses domain mathematics
        ↓
shared abstraction
        ↓
specialists improve implementation
        ↓
existing domain programs benefit
```

Source:

- Florian Rathgeber et al. *Firedrake: automating the finite element method by composing abstractions*. ACM TOMS 43(3), 2017. DOI 10.1145/2998441.
- https://arxiv.org/abs/1501.01809

Boundary: Firedrake does not establish that UFL should be decomposed into independently composable source-language features.

### MLIR — strong analogy for multiple abstraction levels and shared lowering

MLIR allows multiple dialects to coexist in one module and supports passes/conversions between them. That validates the architectural value of retaining domain-specific information and sharing transformation infrastructure rather than immediately flattening everything into one low-level representation.

Source: https://mlir.llvm.org/docs/LangRef/

Boundary: MLIR dialects and UT language features solve overlapping but not identical problems.

### mbeddr — direct evidence for project-specific language extensions

The OOPSLA smart-meter case used an extensible C and domain-specific extensions including physical units and state machines. mbeddr's own case-study catalog says the commercial SmartMeter project used built-in extensions plus project-specific language extensions.

Sources:

- DOI 10.1145/2814270.2814276
- https://research.tudelft.nl/en/publications/using-c-language-extensions-for-developing-embedded-software-a-ca
- https://mbeddr.com/learn.html

This is stronger evidence for the "domain needs acquire language-level concepts" part of the scenario than UFL.

## Why a library may still be better

Before turning a scientific feature into a language extension, ask:

1. Does it need new syntax or domain notation?
2. Does it need static constraints/type-system rules?
3. Does it affect semantic lowering rather than just call a function?
4. Does it need special editor/refactoring/diagnostic behavior?
5. Does it introduce a capability/provider choice that affects the global pipeline?
6. Do multiple dialects need different combinations of it?

If the answers are mostly no, a library/API is likely cheaper.

This matches JetBrains MPS's own FAQ distinction: language extensions are justified by language-level syntax, constraints/type systems and IDE integration; ordinary libraries remain a valid alternative.

Source: https://www.jetbrains.com/help/mps/mps-faq.html

## XP-compatible development path

Do not start by implementing all hypothetical modules above.

```text
Iteration 1
  BioCore only

Iteration 2
  real request: Spatial
  implement directly, keep tests

Iteration 3
  independent request: Stochastic
  repeated variability becomes visible

Refactor
  extract stable language-feature contracts
  make composition explicit

Later
  add fitting/GPU only if demanded
```

The point of XP here is the feedback loop: tests/CI/refactoring make it feasible to discover the extension architecture from real change.

The correct message is therefore:

> We do not design every future language feature. We design a system that can factor recurring variation once it becomes real.

## Presentation diagram

A compact slide can use this shape:

```text
                 domain evolves
                      ↓
            ┌──── BioCore ────┐
            │        │        │
         Spatial  Stochastic  Fitting
            │        │        │
            └──── LanguagePlan ┘
                      ↓
                semantic / IR
                      ↓
          SSA / optimizers / GPU
                      ↓
                 execution
```

Two labels are important:

- above `LanguagePlan`: **language evolution reuse**;
- below `LanguagePlan`: **compiler evolution reuse**.

This visually prevents the false inference that SSA is itself the reason DSL syntax must be extensible.

## Claims this scenario must not make

Do not say:

- bioinformatics teams generally need extensible DSLs;
- scientists want to author compiler modules themselves;
- every research group needs a separate dialect;
- adding a module is safe because all old modules were tested;
- LanguagePlan proves feature compatibility;
- SSA/superoptimization proves the value of source-language extensibility;
- XP recommends building an extension framework before requirements appear.

The safe claim is narrower:

> In domains where a stable computational core serves several evolving domain interfaces, independently reusable language features are one plausible way to avoid turning every new variant into a compiler fork. UniversalToolchain explores the structural composition problem that appears once those features are independently owned.
