---
title: Wist phase ownership
navigation: hidden
status: Internal maintainer architecture evidence for phase-boundary and split-readiness review.
---

# Wist phase ownership

The canonical Wist execution architecture is:

```text
LanguageDefinition
    -> LanguageCompiler
    -> immutable LanguagePlan
    -> LanguageRuntime
    -> exact planned implementations
```

`LanguageCompiler` is the only semantic/planning authority. `LanguageRuntime`, `WistEngine`, artifact transformers and diagnostic listeners do not discover or select language features at runtime.

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

The phase split is behavioral, not just nominal. The frontend stage performs source preprocessing, lexer/parser configuration and syntax-tree processing only. It does not invoke `Binder` or bytecode visitors. The semantic stage materializes only the semantic contributions selected by `LanguagePlan`, owns `Binder`, and snapshots the bound result into `WistSemanticProgram`. The bytecode stage materializes only the lowering contributions selected by the same plan and never derives its modules from syntax contribution IDs.

Phase artifacts are data boundaries. `WistSyntaxArtifact`, `WistSemanticArtifact`, `WistBytecodeArtifact`, and `WistAirArtifact` do not carry `IFrontendCoreModule` or `IAirOptimizer` instances. Syntax, semantic and lowering module instances are created independently for their owning stage and disposed after that stage execution. When an external planned AIR pass supplies module-contract metadata, `WistAirArtifact` retains only a copied `WistOptimizerContractSnapshot` of its contribution identity, namespace owners and facets; the executable optimizer/provider object itself is not retained.

The historical combined `IFrontendCoreModule` remains an implementation shape used by existing Wist module assemblies, but its methods no longer determine phase ownership. `WistModulePhaseOwnership` explicitly maps actual module duties to separate plan contributions:

- syntax contribution: concrete syntax, preprocessing, lexer/parser and syntax-tree processing;
- semantic contribution: binding rules;
- lowering contribution: AST-projection-to-bytecode visitors or bytecode post-processing;
- optimizer contribution: AIR pass;
- backend/runtime contribution: route endpoint and execution.

Syntax-only modules do not receive fake semantic/lowering contributions. In particular, TextualAddition and Comments are syntax-only. Variables has syntax, semantic-binding and lowering ownership. Modules with bytecode visitors receive explicit lowering contributions.

## Semantic representation

The semantic boundary does not retain a live mutable parser AST. `WistSemanticProgram` recursively snapshots the bound tree. Bound local/external symbol identity is retained as semantic data, while frontend module/plugin instances never cross the boundary.

Legacy Wist node kinds that do not yet have a dedicated canonical semantic node are represented by immutable `WistLegacySemanticNode` data. Bytecode lowering can reconstruct a fresh compatibility AST projection from that snapshot for the existing Wist visitors. This is representation migration debt, not cross-phase module ownership: the lowerer set is independently selected from explicit lowering contributions and no syntax-stage module instance or syntax contribution identity is reused to choose lowering behavior.

Mutation of the syntax AST after semantic binding cannot change an already-produced semantic artifact.

## Arithmetic/TextualAddition canonical semantics

Arithmetic `+` and textual `plus` are separate concrete syntax forms but share one semantic identity:

```text
Addition syntax ---------\
                          -> wist.semantic.arithmetic.add -> canonical Add lowering
TextualAddition syntax --/
```

The canonical Add lowerer is enabled only when `wist.lowering.arithmetic.add` is present in the exact `LanguagePlan`. It consumes the semantic operation and its children. It does not inspect `+`, `plus`, `AdditionOperationNodeCreator`, `TextualAdditionOperationNodeCreator`, or a frontend plugin identity.

Program-root lowering is explicit inside the lowering stage. The former hidden `WistProgramStructureFrontendModule` and `WistLegacyFrontendModuleCompatibility` are removed.

All built-in frontend/syntax contributions are now marked:

```text
wist.phase = syntax
wist.compatibility = none
wist.owner = language-plan
```

Semantic and lowering ownership is represented by separate contribution IDs in `wist.semantics.features` and `wist.lowering.features`.

## Runtime extension boundary

Wist does not rely on `InternalsVisibleTo` from UNIVERSAL assemblies. In particular, `UniversalToolchain.Runtime` does not friend the Wist-owned mixed `UniversalToolchain.LanguageSdk.Tests` assembly.

The small UT-owned surface required by the Wist implementation is language-neutral:

- `CanonicalArtifactStages` exposes low-level stage mechanics only; it performs no planning or discovery.
- `LanguagePackageRegistrationIdentity` exposes exact registered-instance provenance checks/materialization; the identity itself is registry-issued and cannot be manufactured by Wist.
- `ILanguageArtifactRouteListener` receives an immutable projection of an already-selected route after a transformation. Its public observation contains the plan, backend, route steps, current step and resulting artifact, but no selection/discovery API.
- `RuntimeLifetimeGate` is lifecycle coordination only and carries no language or planning state.

The Runtime dispatcher behind route observation remains internal. `WistModuleContractRouteObserver` consumes the public listener contract and cannot change the plan, route or backend.

## Module-contract observation

Module-contract verification is derived from the exact selected plan/route. The observer does not instantiate executable frontend modules or AIR optimizers merely to diagnose them. Built-in Wist contracts are projected from the exact selected plan; contract metadata emitted by external planned AIR passes is carried forward as metadata-only snapshots. The observer merges those two sources in already-executed route order and fails on duplicate snapshot identities rather than selecting an alternative implementation.

This keeps diagnostics observational: verification can reject an invalid execution, but it does not become a second feature registry or planner. In particular, a third-party AIR pass can invalidate compiler facts and trigger P2/P3 reverification without forcing Wist to retain the pass object beyond its transformation.

## UT/Wist ownership

`eng/project-ownership.json` is the repository ownership source used by CI. The enforced direction is:

```text
WIST_PRODUCT -> UNIVERSAL      allowed
UNIVERSAL    -> WIST_PRODUCT  forbidden
```

`Tools/check-project-ownership.py` validates total project ownership and rejects reverse `ProjectReference` edges. It also derives assembly/package identities from every WIST_PRODUCT project and rejects hidden reverse edges from UNIVERSAL projects through `InternalsVisibleTo`, `PackageReference`, assembly `Reference`, analyzer and build-import paths. UNIVERSAL C# sources are additionally scanned for explicitly forbidden Wist semantic/assembly tokens.

The architecture workflow contains a negative mutant that temporarily adds a friend edge from `UniversalToolchain.Runtime` to the Wist-owned `UniversalToolchain.LanguageSdk.Tests`; the validator must reject it and then pass again after restoration.

`Tools/verify-universal-only.py` runs the same ownership checks, classifies the complete UNIVERSAL project set, then source-builds the repository source graph and executes its generic test projects without Wist-owned projects. Generated template consumer projects under `UniversalToolchain.Templates/content` remain classified but are not source-added to this solution because they intentionally test the public NuGet surface through `PackageReference`; they are package consumers, not source-graph dependencies.

`UniversalToolchain.LanguageSdk.Generic.Tests` is a Wist-free test owner for deterministic planning, exact planned route materialization/fail-closed behavior and the public plan-owned route-listener boundary. Wist-specific LanguageSdk tests remain in the Wist-owned mixed test project and therefore cannot manufacture a false UT-only PASS.

## Package boundary

`UniversalToolchain.Wist` remains a single facade package for this migration. The reviewed package-surface baseline contains 63 `lib/net10.0` runtime DLL identities. `Tools/verify-wist-package.py` compares each packed candidate against that explicit identity set; CI then builds and runs a fresh external `net10.0` consumer using only the produced `.nupkg` plus normal NuGet dependencies.

The package boundary and repository ownership boundary answer different questions. The phase repair does not add a correctness reason to split the facade package, so no cosmetic package decomposition is performed merely to reduce the DLL count.

## Physical split readiness criteria

This migration does not perform a repository split. Readiness is evaluated separately against these observable conditions:

- no `UNIVERSAL -> WIST_PRODUCT` project/package/assembly/friend/build dependency;
- meaningful Wist-free UT-only build/test proof;
- Wist consumes only explicit language-neutral UT boundaries;
- Wist feature phase ownership is plan-owned rather than inherited from the combined module interface;
- packed Wist clean consumer succeeds from `.nupkg` only;
- current package closure matches the reviewed identity contract.

A final READY/NOT READY decision is made only from the corresponding CI/package evidence for the exact candidate HEAD; documentation alone is not evidence.
