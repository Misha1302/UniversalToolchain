---
title: Semantic Fact & Proof Layer — research note
navigation: hidden
status: Research proposal; not part of published user navigation.
---

# Semantic Fact & Proof Layer — research note

Status: research proposal
Date: 2026-09-20
Basis: the 2026-09-20 voice-note transcript, current UniversalToolchain architecture, current ContractExperiments, and a bounded prior-art review.

> This note does not claim research novelty. It records the design problem, maps it to existing mechanisms, and proposes the smallest useful experiment.

## 1. Problem

UniversalToolchain already has a deterministic planning problem:

```text
language packages
  -> LanguageDefinition
  -> LanguageCompiler
  -> immutable LanguagePlan
  -> typed route
  -> exact backend/runtime
```

That solves "which component/stage comes next?" reasonably well.

The harder problem is semantic cooperation between independently-developed modules.

A new feature such as a delegate/closure can add syntax without much difficulty, but code generation, serialization, capture analysis, optimization and backend lowering need semantic information from type systems, runtimes, backends and other modules. The delegate feature should not need to know their concrete identities or APIs.

The target property is:

> Modules communicate through typed semantic facts/capabilities and proof obligations, not through concrete knowledge of one another.

This is deliberately a semantic layer above concrete implementation APIs. A type system, interpreter, JIT or native backend may implement the same high-level feature in radically different ways; the delegate/closure feature should depend on facts such as serializability, purity, capture shape or materializability rather than on one backend's internal representation. The layer does not attempt to reconstruct all high-level semantics from generated CIL, native code or interpreter internals.

### Semantic structural compatibility

Consumers should quantify over proven semantic properties, not nominal implementation types. Two operands participating in one operation need not have the same concrete CLR type or originate from the same module if they satisfy the required semantic contract.

For example:

```text
Combine(left, right)
  requires CallableLike(left)
  requires CallableLike(right)
  requires CompatibleSignature(left, right)
```

This is closer to structural/capability typing over compile-time semantic evidence than to runtime dynamic typing. It is important for independently-developed modules: compatibility is established by shared predicates rather than concrete cross-package references.

Operations themselves also have contracts. Preconditions, postconditions and preservation/invalidation behavior belong next to algebraic laws:

```text
Combine(left, right)
  requires  CallableLike(left), CallableLike(right)
  produces  CallableLike(result)
  preserves Deterministic(result) when both inputs are deterministic
  invalidates CachedCaptureLayout(result) when capture structure changes
```

Example questions:

- Can entity X be serialized to target T?
- Does X require context C?
- Is operation O pure/deterministic in scope S?
- Can two delegates share one serialized capture context?
- Can the selected backend materialize the required representation?

If the required semantic fact cannot be established, correctness-sensitive transformations must fail closed.

## 2. Separate four concepts

The original discussion mixed "ontology" and "algebra". They should be distinct.

### Vocabulary / ontology

Typed predicates and relations:

```text
DependsOn(x, y, kind)
Captures(delegate, context)
Serializable(x, target)
StableIdentity(x, scope)
Pure(operation, scope)
BackendSupports(backend, capability)
```

### Algebraic laws

Properties of operations over domains, not generic object flags:

```text
Commutative(op, domain)
Associative(op, domain)
Idempotent(op, domain)
Distributive(opA, opB, domain)
Transitive(relation, domain)
```

### Inference rules

Rules that derive facts:

```text
CanShareSerializedContext(d1, d2, c, target)
  :- Captures(d1, c),
     Captures(d2, c),
     Serializable(c, target),
     StableIdentity(c, SnapshotScope),
     SnapshotStable(c, SnapshotScope).
```

### Proof obligations

Consumers declare what must be proven before an action is legal:

```text
requires Proven(CanShareSerializedContext(...))
```

### Primitive versus derived properties

The vocabulary should prefer small, stable, reasonably orthogonal primitive facts and derive convenience facts where possible. The goal is not to pretend that every semantic property can be mathematically independent; real compiler semantics overlap. The practical rule is to avoid duplicate synonyms and manually repeated consequences. If `CanPersist(x)` is always derivable from more fundamental facts, it should normally be a rule result rather than another independently asserted flag.

## 3. Open-world, fail-closed semantics

Do not equate missing metadata with false or safe.

Public query state should be at least:

```text
Proven
Disproven
Unknown
Contradiction
```

For correctness-sensitive actions:

```text
Proven     -> may proceed
Disproven  -> must not proceed
Unknown    -> must not proceed; safe fallback only
Contradiction -> diagnostic/failure
```

Internally, absence of a positive or explicit-negative proof should mean Unknown.

`Disproven` must require explicit negative evidence; it is never negation-by-absence. A useful internal model is proposition polarity:

```text
positive evidence only -> Proven
negative evidence only -> Disproven
neither                 -> Unknown
both                     -> Contradiction
```

The MVP should therefore avoid negation-as-failure. Explicit negative facts can be represented by proposition polarity or dedicated negative evidence records while rule evaluation remains monotone.

This is especially important for independently-developed extensions: a module cannot assume that an unknown production/object/dependency does not exist.

## 4. Existing UniversalToolchain foundation

Do not build a parallel architecture.

The current repository already has `UniversalToolchain.ContractExperiments`. Its policy specification models semantic evidence through:

```text
requires
produces
preserves
invalidates
```

and already has:

- selective semantic verifier scheduling;
- invalidation-created obligations;
- unresolved-obligation failure;
- unknown/missing/conflicting verifier routes failing closed.

This is evidence from the current experimental surface, not a claim that the proposed semantic layer is already a production feature.

The proposed layer should generalize the *shape* of facts and add derivation/provenance.

Useful split:

```text
ContractExperiments
  = lifecycle of facts through passes/boundaries

Semantic Fact & Proof Layer
  = typed predicates + relations + derivation + provenance
```

Existing pass semantics remain valid:

```text
requires  AirVerified
preserves NoUndefinedControlFlow
invalidates ValueRangeKnown(v)
produces  Canonicalized(region)
```

but a fact can now be a typed proposition with arguments and scope.

## 5. Minimal architecture

```text
Predicate schema
   ↓
Evidence providers
   ↓
Fact store
   ↓
Rule/fixpoint engine
   ↓
Proof provenance
   ↓
Obligation queries
   ↓
planner / optimizer / serializer / backend
```

### Predicate schema

Predicates have stable typed identities.

```csharp
SemanticPredicate Serializable(Entity, SerializationTarget);
SemanticPredicate DependsOn(Entity, Entity, DependencyKind);
SemanticPredicate Captures(Entity, Entity);
SemanticPredicate StableIdentity(Entity, SemanticScope);
SemanticPredicate BackendSupports(BackendId, CapabilityId);
```

### Evidence providers

Concrete components publish only primitive facts they actually own.

Example:

```text
delegate analysis:
  Captures(d1, c)
  Captures(d2, c)
  DependencySetComplete(d1, Capture)
  DependencySetComplete(d2, Capture)

runtime/backend provider:
  Serializable(c, PersistentSnapshot)
  StableIdentity(c, SnapshotScope)
  SnapshotStable(c, SnapshotScope)
```

Providers do not reference consumers.

### Evidence authority and what "proof" means

The word *proof* in this note means a machine-checkable derivation from registered semantic evidence under known rules. It does **not** by itself mean formal verification that a provider's implementation satisfies the fact it publishes.

Evidence should retain its origin, for example:

```text
DeclaredByOwner
EstablishedByVerifier
DerivedByRule
```

A verifier-backed fact is stronger evidence than an unchecked author declaration, but both can participate only according to explicit policy. A buggy or dishonest provider can otherwise publish a false premise and make a derivation internally valid but semantically wrong. The fact layer composes evidence; it does not magically prove arbitrary implementation semantics.

### Fact store

Facts must be scoped by relevant environment:

- frozen LanguagePlan identity;
- backend/runtime/platform;
- compiler phase/artifact boundary;
- serialization target;
- semantic assumptions where necessary.

Do not publish a backend-sensitive fact as globally true.

### Rule engine

Do not define inference as BFS/DFS to an arbitrary depth.

Start with a deliberately restricted Datalog/Horn-clause-like model:

- finite typed predicates;
- range-restricted variables;
- monotone rules by default;
- deterministic fixed-point/worklist evaluation;
- recursive rules allowed only with fixed-point semantics;
- no unrestricted arbitrary code in declarative rules;
- explicit negative evidence rather than negation-as-failure in the MVP;
- stratified negation/aggregates/custom solvers only later.

An operational budget is still useful. Exhausting it yields Unknown, never a positive result.

### Provenance

Every derived fact should have an explanation:

```text
CanShareSerializedContext(d1,d2,c,target)
  by rule semantic.serialization.shared-context.v1
  from:
    Captures(d1,c)                         [delegate-analysis]
    Captures(d2,c)                         [delegate-analysis]
    Serializable(c,target)                [runtime-provider]
    StableIdentity(c,SnapshotScope)        [runtime-provider]
    SnapshotStable(c,SnapshotScope)         [runtime-provider]
```

Include owner/provider/version/plan identity so stale proofs cannot survive a package/backend change silently.

## 6. Delegate serialization example

The original "two delegates + one program instead of two programs" idea is best modeled as graph serialization with identity preservation/shared subobjects, not as a linked list.

```text
Delegate d1 ─┐
             ├──captures──> Context c
Delegate d2 ─┘
```

A naive tree serializer duplicates c. A graph serializer can emit one serialized context node and two references.

But deduplication is legal only when facts prove that sharing preserves semantics.

Possible premises:

```text
Captures(d1,c)
Captures(d2,c)
Serializable(c,target)
StableIdentity(c,SnapshotScope)
SnapshotStable(c, SnapshotScope)
```

Derived:

```text
CanShareSerializedContext(d1,d2,c,target)
```

If an interpreter/runtime contains opaque native state, its provider can explicitly disprove serialization or simply fail to provide the proof. Either way the persistent-serialization optimization does not run.

This expresses backend/runtime differences without hard-coding them into the delegate module.

## 7. Completeness is a semantic fact too

A subtle open-world bug:

```text
Serializable(x,target)
  if every dependency of x is Serializable(_,target)
```

is unsound unless the engine knows that it has enumerated *all* dependencies.

Therefore dependency discovery must be able to prove:

```text
DependencySetComplete(x, kind)
```

Only then may an aggregate/verifier derive something like:

```text
AllDependenciesSerializable(x, kind, target)
```

This "completeness of evidence" concept is mandatory for sound open-world reasoning.

## 8. Prevent property explosion

Do not ask every feature author to describe hundreds of axioms.

Use three authoring tiers.

### Tier A — reusable semantic traits/profiles

Normal authors pick from common semantics:

```text
Pure
Deterministic
NoMemoryEffect
ReadsCaptureGraph
DoesNotMutateCaptureGraph
PreservesIdentity
ContextFree
```

A profile expands into primitive facts and standard rules.

### Tier B — evidence provider/interface

Advanced components implement a typed semantic provider for custom behavior.

### Tier C — new predicate/rule family

Rare framework-level work. New fundamental predicates/rules require review for:

- scope;
- ownership;
- monotonicity;
- conflict semantics;
- invalidation;
- proof explainability.

### Who authors what

The intended responsibility split is:

- a normal feature author reuses existing traits and publishes only facts owned by that feature;
- an operation author declares operation-specific preconditions/effects and algebraic laws only when they are semantically guaranteed;
- a backend/runtime author publishes backend- or platform-scoped capability/evidence;
- a verifier author establishes facts that can be checked from an artifact boundary;
- framework maintainers own new fundamental predicate families, conflict semantics and reusable inference rules.

This keeps ordinary module work away from theorem-engine design.

The ergonomics principle should be:

> Missing semantic metadata normally costs an optimization or optional feature, not correctness.

## 9. Prior art

### MLIR

MLIR provides the closest match for generic semantic interfaces:

- operation traits/properties;
- Op/Type/Attribute Interfaces;
- external interface models;
- ODS/TableGen as single-source semantic metadata with generated boilerplate;
- DataFlow lattices and conservative unknown/top states;
- fail-closed behavior in transformations such as bufferization unless unknown handling is explicitly enabled.

It is a strong model for typed semantic interfaces and conservative analyses. It is not one universal cross-dialect theorem engine.

Primary references:
- https://mlir.llvm.org/docs/Interfaces/
- https://mlir.llvm.org/docs/Traits/
- https://mlir.llvm.org/docs/DefiningDialects/Operations/
- https://mlir.llvm.org/docs/Tutorials/DataFlowAnalysis/
- https://mlir.llvm.org/docs/Bufferization/

### Silver / AbleC

This is the strongest examined match for independently-developed language extensions that need not know each other.

Relevant ideas:

- forwarding;
- extensible attribute grammars;
- modular well-definedness analysis;
- explicit research goal that one extension need not be aware of another and that composition should not require glue code.

Silver also explicitly warns against unsafe closed-world "default" semantics for unknown future productions.

References:
- https://melt.cs.umn.edu/silver/
- https://melt.cs.umn.edu/
- https://melt.cs.umn.edu/ableC/
- https://melt.cs.umn.edu/silver/ref/stmt/forwarding/
- https://melt.cs.umn.edu/silver/ref/decl/productions/default/

### Datalog / Soufflé

Best conceptual match for the inference kernel:

- typed relations;
- facts and Horn-clause rules;
- recursive fixed-point derivation;
- semi-naive evaluation;
- proof provenance/explanation trees.

This is a better semantic basis than bounded BFS/DFS.

References:
- https://souffle-lang.github.io/program
- https://souffle-lang.github.io/rules
- https://souffle-lang.github.io/provenance

### K Framework

Useful reference for explicit semantic/algebraic laws such as associative/commutative/idempotent/unit and rules with requires/ensures. Likely too heavy for the MVP of a compiler-plugin semantic contract layer.

Reference:
- https://kframework.org/exports/K.html

### egg

Useful for equality saturation and algebraic equivalence/rewrite search. It is a future optimizer reference, not a substitute for general facts such as Serializable or BackendSupports.

Reference:
- https://docs.rs/egg/latest/egg/tutorials/_01_background/index.html

## 10. Recommended synthesis

```text
Silver/AbleC
  independent extension composition

+ MLIR
  typed semantic traits/interfaces + generated metadata

+ current UT ContractExperiments
  requires/produces/preserves/invalidates + verifier obligations

+ Datalog semantics
  facts + monotone rules + fixed point + provenance

= UniversalToolchain Semantic Fact & Proof Layer
```

This layer must not become a second planner.

`LanguagePlan` continues to choose packages, routes and backends exactly once. The semantic fact layer reasons only inside that already-selected world.

## 11. MVP

Start with one vertical slice: delegate capture + serialization.

Initial predicates (roughly 8–12):

```text
Captures(delegate, context)
DependsOn(entity, dependency, kind)
DependencySetComplete(entity, kind)
Serializable(entity, target)
StableIdentity(entity, scope)
SnapshotStable(entity, scope)
BackendSupports(backend, capability)
Materializable(entity, backend)
CanShareSerializedContext(d1,d2,c,target)   // derived
```

Infrastructure:

1. typed entity/predicate IDs;
2. typed proposition arguments;
3. Proven/Disproven/Unknown/Contradiction with explicit proposition polarity;
4. provider, evidence-authority and provenance identities;
5. monotone rule registration;
6. deterministic worklist/fixpoint evaluation;
7. explanation/proof-tree API;
8. adapter to existing requires/produces/preserves/invalidates;
9. invalidation propagation for derived facts;
10. deterministic hashing/order compatible with LanguagePlan reproducibility.

## 12. Rules to enforce immediately

1. No optimistic defaults.
2. No implicit closed-world reasoning.
3. No unscoped backend-sensitive facts.
4. No semantic re-planning after LanguagePlan freeze.
5. No unbounded arbitrary theorem search.
6. Every derived fact is explainable.
7. Invalidating a premise invalidates dependent derived facts.
8. Conflicting evidence is visible, never silently resolved by provider order.
9. Algebraic laws are scoped to operations/domains/preconditions.
10. Missing optional facts may disable optimization; required semantics need explicit implementation/verifier contracts.
11. Absence of evidence is Unknown, never Disproven.
12. A successful derivation proves only what follows from its registered premises; provider truth requires its own authority/verifier contract.

## 13. Strongest simpler alternative

The serious alternative is to stop at MLIR-style typed interfaces/capabilities and have consumers query providers directly.

Pros:

- smaller implementation;
- easier debugging;
- more ordinary C# typing;
- fewer inference/termination/conflict problems.

Cons:

- derived cross-module semantics becomes hand-written glue;
- N producer × M consumer coupling grows;
- provenance fragments;
- transitive dependency reasoning becomes bespoke in each consumer.

Recommendation: do not build a general theorem prover. Build a *small* fact/rule kernel whose first evidence providers are typed interfaces. Expand it only when real cross-module derivations justify the extra machinery.

## 14. Concrete experiment

Build a separate experiment beside current ContractExperiments before touching production architecture.

### A. Semantic kernel

Implement:

- 6–10 predicates;
- 3–5 inference rules;
- open-world four-state queries;
- explicit positive/negative evidence;
- provenance;
- deterministic fixpoint;
- invalidation propagation.

### B. Delegate profiles

Test three runtime profiles:

1. fully serializable context;
2. partially opaque interpreter context;
3. serializable context with unstable/non-shareable identity.

Expected:

- profile 1: prove serialization and shared-context deduplication;
- profile 2: Unknown/Disproven persistent serialization, skip;
- profile 3: serialize but refuse shared-context deduplication.

### C. Independence test

Put providers in packages that do not reference the delegate optimizer. The optimizer imports only the semantic contract package. Swap providers without changing optimizer code.

### D. Boilerplate measurement

Compare:

1. direct module-to-module APIs;
2. typed interfaces only;
3. traits + fact rules.

Measure:

- handwritten LOC;
- cross-package references;
- declarations per operation;
- glue changes after adding a provider;
- failure behavior when a provider is absent.

This makes the "will property declarations make developer life unbearable?" question empirical.

### E. Heterogeneous composition test

Define two nominally unrelated delegate-like entities from independent packages. Give them only the shared semantic facts required by `Combine`/serialization. The consumer must accept or reject them solely from those facts, without adding package-specific branches. This directly tests the structural semantic compatibility goal from the original idea.

## 15. Voice-note traceability

The proposal intentionally preserves the main ideas from the source discussion:

- fixed toolchain-stage ordering is not the hard part; cooperation with feature modules above/beside the pipeline is;
- component APIs vary across type systems/runtimes/backends, so consumers should not bind to concrete implementations;
- objects and operations expose high-level semantic properties rather than requiring one universal low-level implementation model;
- operations may combine nominally different entities when required properties are proven;
- algebraic laws, purity, determinism, context dependence and preservation/invalidation are distinct semantic dimensions;
- property explosion is controlled through reusable traits, providers and derived facts;
- bounded ad-hoc BFS/DFS theorem search is replaced with deterministic restricted inference;
- delegate serialization follows transitive dependencies and must account for opaque interpreter/native state;
- multiple delegates may share one serialized context only when identity/snapshot semantics permit graph deduplication;
- MLIR and Silver/AbleC are comparison points for semantic interfaces and independent extension composition;
- missing knowledge is fail-closed for correctness-sensitive actions;
- platform/backend/runtime differences are facts in scope, not hard-coded delegate logic.

## 16. Research conclusion

The idea is technically coherent, but the useful version is narrower than "an algebra containing hundreds of manually declared properties for every object".

A practical formulation is:

> UniversalToolchain modules expose typed semantic evidence about entities they own. A small open-world rule engine derives additional facts with provenance. Compiler actions declare proof obligations. Unknown evidence fails closed for correctness-sensitive transformations. Existing requires/produces/preserves/invalidates contracts remain the lifecycle mechanism as artifacts move through the pipeline.

The examined systems contain close prior art for the individual parts. The research question is therefore the value and soundness of their combination inside UniversalToolchain, not a claim that traits, inference, forwarding, or proof search are individually new.
