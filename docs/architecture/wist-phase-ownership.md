---
title: Wist phase ownership
status: current
---

# Wist phase ownership

The canonical Wist execution architecture remains:

```text
LanguageDefinition
    -> LanguageCompiler
    -> immutable LanguagePlan
    -> LanguageRuntime
    -> exact planned implementations
```

`LanguageCompiler` is the only semantic/planning authority. `LanguageRuntime`, `WistEngine`, artifact transformers, compatibility adapters and diagnostic listeners do not discover or select language features at runtime.

## Compilation phases

The Wist artifact route is explicit:

```text
source text
    -> frontend/syntax
    -> semantic binding
    -> bytecode lowering
    -> AIR lowering
    -> planned AIR optimizers
    -> planned backend
```

Phase artifacts are data boundaries. `WistSyntaxArtifact`, `WistSemanticArtifact`, `WistBytecodeArtifact`, and `WistAirArtifact` do not carry `IFrontendCoreModule` or `IAirOptimizer` instances. Frontend modules and optimizers are materialized from exact `LanguagePlan` contribution provenance inside the stage that owns their execution and do not survive that stage.

The legacy compatibility semantic payload is a snapshot, not a live `AstNode` reference. A separate AST is reconstructed only when the bounded legacy lowering adapter runs, so mutation of the parser-owned tree cannot mutate an already-produced semantic artifact.

## Arithmetic/TextualAddition pilot

Arithmetic `+` and textual `plus` are separate concrete syntax forms but share one semantic identity:

```text
Addition syntax ---------\
                          -> wist.semantic.arithmetic.add -> canonical Add lowering
TextualAddition syntax --/
```

The canonical Add lowerer consumes the semantic operation and its children. It does not inspect `+`, `plus`, `AdditionOperationNodeCreator`, `TextualAdditionOperationNodeCreator`, or a frontend plugin identity.

Program-root lowering is explicit inside the lowering stage. The former hidden `WistProgramStructureFrontendModule` is removed.

## Transitional legacy adapter

Canonical features other than the Arithmetic/TextualAddition pilot still use `WistLegacyFrontendModuleCompatibility` for their existing AST-to-bytecode visitors. This is a bounded migration adapter, not a second planner: module factories are derived from already-resolved `LanguagePlan.Contributions`, package identity is checked against the exact registry-issued implementation provenance, and the compatibility semantic artifact contains only snapshotted data.

Every non-pilot frontend contribution is mechanically marked with:

```text
wist.compatibility = legacy-cross-phase-lowering-adapter
```

Arithmetic and TextualAddition are marked `none`. The compatibility boundary is tested and remains explicit migration debt; it is not the target architecture for new features.

## Runtime extension boundary

Wist no longer relies on `InternalsVisibleTo` from UniversalToolchain assemblies.

The small UT-owned surface required by the Wist implementation is language-neutral:

- `CanonicalArtifactStages` exposes stage mechanics only; callers supply already-selected lexer/parser/modules/translators and it performs no planning or discovery.
- `LanguagePackageRegistrationIdentity` exposes exact registered-instance provenance checks/materialization; the identity itself is registry-issued and cannot be manufactured by Wist.
- `ILanguageArtifactRouteListener` receives an immutable projection of an already-selected route after a transformation. Its public observation contains the plan, backend, route steps, current step and resulting artifact, but no selection/discovery API.
- `RuntimeLifetimeGate` is lifecycle coordination only and carries no language or planning state.

The Runtime dispatcher behind route observation remains internal. `WistModuleContractRouteObserver` consumes the public listener contract and cannot change the plan, route or backend.

## Module-contract observation

Module-contract verification is derived from the exact selected plan/route. The observer does not instantiate executable frontend modules or AIR optimizers merely to diagnose them. It builds contract-only projections from the selected runtime-component descriptors, preserving namespace owners/facets and deriving applied optimizer contracts only from route steps already executed.

This keeps diagnostics observational: verification can reject an invalid execution, but it does not become a second feature registry or planner.

## UT/Wist ownership

`eng/project-ownership.json` is the repository ownership source used by CI. The enforced direction is:

```text
WIST_PRODUCT -> UNIVERSAL      allowed
UNIVERSAL    -> WIST_PRODUCT  forbidden
```

`Tools/check-project-ownership.py` validates total project ownership, `ProjectReference` direction and Wist-owned source/assembly identities in every UNIVERSAL C# source file, including `AssemblyInternals.cs`. The previous friend-metadata scanner exemption is gone, so a UNIVERSAL assembly cannot silently regain a Wist-specific `InternalsVisibleTo` edge while the ownership gate remains green.

`Tools/verify-universal-only.py` classifies the complete UNIVERSAL project set, then source-builds the repository source graph and executes its generic test projects without Wist-owned projects. Generated template consumer projects under `UniversalToolchain.Templates/content` remain classified but are not source-added to this solution because they intentionally test the public NuGet surface through `PackageReference`; they are package consumers, not source-graph dependencies.

`UniversalToolchain.LanguageSdk.Generic.Tests` is a Wist-free test owner for deterministic planning, exact planned route materialization/fail-closed behavior and the public plan-owned route-listener boundary. Wist-specific LanguageSdk tests remain in the Wist-owned mixed test project and therefore cannot manufacture a false UT-only PASS.

## Package boundary

`UniversalToolchain.Wist` remains a single facade package for this migration. The reviewed package-surface baseline contains 63 `lib/net10.0` runtime DLL identities. `Tools/verify-wist-package.py` compares each packed candidate against that explicit identity set; CI then builds and runs a fresh external `net10.0` consumer using only the produced `.nupkg` plus normal NuGet dependencies.

The package boundary and the repository ownership boundary answer different questions: a broad facade closure can still be a valid package surface, while a repository split additionally requires the source dependency direction and extension APIs above to remain coherent without hidden friend coupling.

## Physical split readiness criteria

This migration does not perform a repository split. Readiness is evaluated separately against these observable conditions:

- no `UNIVERSAL -> WIST_PRODUCT` project/source/friend dependency;
- meaningful Wist-free UT-only build/test proof;
- Wist consumes only explicit language-neutral UT boundaries;
- packed Wist clean consumer succeeds from `.nupkg` only;
- current package closure matches the reviewed identity contract;
- remaining compatibility adapter debt is explicit, mechanically marked and bounded.

A final READY/NOT READY decision is made only from the corresponding CI/package evidence for the exact candidate HEAD; documentation alone is not evidence.
