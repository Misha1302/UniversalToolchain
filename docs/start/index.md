---
title: Start Here
description: Choose the Wist, external language-authoring or framework-internals route.
audience: wist-application-developer
status: current
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# Start here

UniversalToolchain is a modular .NET compiler/runtime framework. **Wist** is its reference language and packaged formula API. The repository also exposes a separate **External Language Authoring SDK** for independent non-Wist languages.

## Choose the correct surface

| You need to... | Use |
|---|---|
| validate restricted numeric formulas and compile typed delegates | `UniversalToolchain.Wist`; start with [First Program](/start/first-program) |
| create an independent language package with your own syntax artifacts and backends | `UniversalToolchain.LanguageAuthoring`; start with [Language Authoring Quickstart](/language-authoring/quickstart) |
| configure a shipped Wist language/runtime surface | Wist `.wistdialect` profiles; start with [Dialect Files](/build-dsls/dialect-files) |
| add syntax to the existing Wist frontend pipeline | supported Wist compiler-module contracts; start with [Choose an Extension Type](/write-modules/choose-extension-type) |
| understand implementation boundaries | [Physical Project Map](/architecture/project-map) and [Lowering Walkthrough](/architecture/lowering-walkthrough) |

## Core model

```text
Wist application path
source -> Wist frontend -> Bytecode -> AIR -> optimizer/backend -> typed delegate or execution result

Generic language-authoring path
package descriptors + runtime registrations -> LanguageDefinition -> LanguagePlan -> typed artifact route -> exact backend executor
```

The generic SDK does not force every language through Wist AST, Bytecode or AIR. Those are Wist/framework artifact protocols, not universal mandatory stages.

## Current maturity

- Wist `0.1.0-alpha.1` is a controlled-evaluation/prototype package, not a stable 1.0 contract.
- Generic language authoring is a low-level alpha with typed routing, deterministic planning and runtime lifecycle contracts.
- Restricted composition is not a hardened sandbox.
- Public evidence and current gaps are tracked under [Evidence and Release Status](/evidence/).

## Recommended next step

- Application developer: [Install Wist](/start/installation), then follow [Production Integration](/start/production-integration) before implementing hot updates.
- Language author: [Build the Acme sample](/language-authoring/quickstart).
- Compiler/runtime contributor: [Read the project map](/architecture/project-map).
