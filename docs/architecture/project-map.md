---
title: Physical Project Map
description: Map architectural roles to projects, key types and tests.
audience: framework-contributor
status: current
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# Physical project map

This page maps conceptual architecture to the repository. It is a navigation aid, not an API-stability promise.

## Generic language-authoring stack

| Role | Project | Key types | Main verification |
|---|---|---|---|
| stable IDs, policies and artifact contracts | `UniversalToolchain.Language.Abstractions` | `LanguageArtifactKind<T>`, `LanguageDefinition`, `LanguageRuntimePolicy`, IDs and slots | `UniversalToolchain.LanguageSdk.Tests` |
| feature/contribution descriptors | `UniversalToolchain.FeatureSdk` | feature and contribution descriptors, merge policy, transformation metadata | typed-authoring and planning tests |
| package registry and deterministic planning | `UniversalToolchain.LanguageSdk` | `LanguagePackageRegistry`, `LanguageDefinitionBuilder`, `LanguageCompiler`, `LanguagePlan`, `LanguageLockFile` | external authoring, architecture and canonicalization tests |
| coupled descriptor/runtime authoring | `UniversalToolchain.LanguageAuthoring` | `LanguagePackageBuilder`, feature/contribution builders | typed-authoring tests and package smoke |
| route assembly and execution | `UniversalToolchain.Runtime` | `LanguageRuntime`, `LanguageRouteRuntimeAssembler`, component registrations, providers and sessions | lifecycle and external execution tests |
| reusable contract tests | `UniversalToolchain.Testing` | `LanguageContractSuite` | sample/package consumers |
| package template | `UniversalToolchain.Templates` | `ut-language` template | clean template consumer smoke |
| Wist compatibility pack | `UniversalToolchain.Wist.LanguagePack` | Wist feature/runtime pack and legacy adapter | Language SDK and Wist dialect tests |
| independent reference consumer | `samples/Acme.PricingLanguage` | custom syntax, parser, interpreter and compiled backend | solution build and sample run |

## Experimental research tooling

| Role | Project | Key types | Main verification |
|---|---|---|---|
| deterministic PlanFuzz contracts | `UniversalToolchain.PlanFuzz.Core` | `PlanFuzzTestCase`, `PlanFuzzObservation`, adapter and oracle contracts, replay records | `UniversalToolchain.PlanFuzz.Tests` |
| independent Acme adapter | `UniversalToolchain.PlanFuzz.Adapter.Acme` | structured generator, plan variants and typed decimal executor | unit and strict-process replay tests |
| Wist Level 0 adapter | `UniversalToolchain.PlanFuzz.Adapter.Wist` | restricted `Int32` model, interpreter/compiler matrix, SSA policy mapping and route evidence | direct oracle tests and clean fresh-process parameter replay |
| coordinator and worker CLI | `UniversalToolchain.PlanFuzz.Cli` | generation, isolated workers, replay, campaign and artifact manifest | `UniversalToolchain.PlanFuzz.IntegrationTests` |

These projects are non-packable experimental research tooling. They do not extend the public Wist package surface.

They are grouped in `UniversalToolchain/PlanFuzz.sln`; `build.sh` and `build.ps1` build this configuration-complete research solution alongside `Wist.sln` before executing `eng/test-projects.txt`.

## Wist compiler/runtime stack

| Role | Representative projects |
|---|---|
| lexer and parser | `BasicLexer`, `BasicParser` |
| frontend orchestration | `BasicCore` and feature modules such as `ArithmeticModule`, `NumbersModule`, `ConditionsModule` |
| AST/Bytecode contracts | `BasicCore`, `UniversalToolchain` feature modules |
| Bytecode to AIR | `AbstractIrConverters` |
| AIR contracts and operations | `UniversalToolchain.Ir.Abstractions`, `UniversalIntermediateRepresentation` |
| capabilities and selected runtime | `UniversalToolchain.Capabilities.*`, `UniversalToolchain.Dialects.*` |
| reference execution | `BasicInterpreter` |
| CIL execution | `BasicCilCompiler`, `BytecodeDynamicMethodsCompiler` |
| SSA experiment | `UniversalToolchain.Ssa.*`, Wist SSA adapter/projects |
| Wist dialect integration | `UniversalToolchain.Dialects.Wist` |
| public formula facade | `UniversalToolchain.Wist` |
| CLI | `Wistc` |
| product sample | `samples/Wist.RolloutScoring` |

## Test projects

The canonical test list is declared in `eng/test-projects.txt`. On the PlanFuzz research branch it contains:

- `Tests`;
- `UniversalToolchain.Modules.Tests`;
- `UniversalToolchain.Dialects.Tests`;
- `UniversalToolchain.LanguageSdk.Tests`;
- `UniversalToolchain.PlanFuzz.Tests`;
- `UniversalToolchain.PlanFuzz.IntegrationTests`.

The historical verification page remains tied to the pre-PlanFuzz artifact until the full canonical build and test run is repeated. Do not infer coverage from project count alone. Use [current verification](/evidence/current-verification) for the tied evidence record.
