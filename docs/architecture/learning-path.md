---
title: Architecture Learning Path
description: Read UniversalToolchain at three depths without traversing every subsystem at once.
audience: learner-evaluator
status: current
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# Architecture learning path

Use the smallest depth that answers your question. The Wist compiler pipeline and the generic Language Authoring route are related but not identical.

## 30-minute orientation

1. [Mental Model](/start/mental-model)
2. [Physical Project Map](/architecture/project-map)
3. [Lowering and Route Walkthrough](/architecture/lowering-walkthrough)
4. [Current Architecture Status](/CURRENT_ARCHITECTURE_STATUS)
5. [Limitations](/limitations)

Outcome: you should be able to explain the difference between Wist, Wist dialect composition and the generic Language Authoring SDK.

## Two-hour compiler/runtime path

1. [Wist Pipeline](/internals/pipeline)
2. [Bytecode and AIR](/architecture/bytecode-and-air)
3. [Runtime Composition](/current-canonical-runtime-pipeline)
4. [Backends and Semantic Parity](/architecture/backends-and-parity)
5. [Composition Explainability](/architecture/composition-explain-plan)
6. [Debug Trace v2](/architecture/debug-trace-v2)

Outcome: you should be able to follow one Wist program from source through frontend artifacts, runtime selection and execution.

## Generic SDK path

1. [External Language Authoring Quickstart](/language-authoring/quickstart)
2. [Packages and Contributions](/language-authoring/package-model)
3. [Typed Artifact Routing](/language-authoring/artifact-routing)
4. [Runtime Lifecycle and Policy](/language-authoring/runtime-lifecycle)
5. [Deep SDK Architecture](/architecture/external-language-authoring-sdk)

Outcome: you should be able to describe how a non-Wist package contributes transformations and exact backend executors without passing through Wist AST, Bytecode or AIR.

## Optimizer and SSA path

1. [AIR Reference](/reference/air-reference)
2. [Callable-first SSA](/architecture/callable-first-ssa)
3. [SSA Coverage Matrix](/architecture/ssa-coverage-matrix)
4. [SSA Route Tests](/testing/ssa-route-tests)
5. [SSA Route Correctness Release](/releases/ssa-route-correctness-2026-07-04)

Outcome: you should understand the verifier-gated `AIR -> SSA -> passes -> AIR` route, its supported subset and its failure/fallback policies.

## Code-reading anchors

Use [Physical Project Map](/architecture/project-map) to map each concept to projects and tests. Read implementation in this order:

```text
public API or sample
-> orchestration/builder
-> immutable plan or artifact contract
-> runtime implementation
-> focused tests
-> evidence/release record
```

Do not begin with the largest solution-wide service-registration file; it hides ownership boundaries that are clearer in package builders, contracts and focused tests.
