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

`LanguageCompiler` is the only semantic/planning authority. `LanguageRuntime`, `WistEngine`, artifact transformers, and compatibility adapters do not discover or select language features at runtime.

## Compilation phases

The Wist artifact route is now explicit:

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

Canonical features other than the Arithmetic/TextualAddition pilot still use `WistLegacyFrontendModuleCompatibility` for their existing AST-to-bytecode visitors. This is a bounded migration adapter, not a second planner: module factories are derived from already-resolved `LanguagePlan.Contributions`, and bound AST payload is preserved as data to retain symbol identity.

Every non-pilot frontend contribution is mechanically marked with:

```text
wist.compatibility = legacy-cross-phase-lowering-adapter
```

Arithmetic and TextualAddition are marked `none`. The compatibility boundary is tested and tracked by the migration issue `Migrate remaining Wist frontend modules off the legacy cross-phase lowering adapter`.

## UT/Wist ownership

`eng/project-ownership.json` is the repository ownership source used by CI. The enforced direction is:

```text
WIST_PRODUCT -> UNIVERSAL      allowed
UNIVERSAL    -> WIST_PRODUCT  forbidden
```

`Tools/check-project-ownership.py` validates all project ownership and `ProjectReference` edges. `Tools/verify-universal-only.py` constructs a solution only from `UNIVERSAL` projects, builds it, and executes its test projects. This is the mechanical proof that the UT-owned source closure does not require Wist-owned project references.

## Package boundary

`UniversalToolchain.Wist` remains a single facade package for this migration. Its current runtime implementation closure is still the reviewed 63-DLL monolithic closure; this change does not pretend that the physical repository/package split is ready.

`Tools/verify-wist-package.py` compares the packed `lib/net10.0` DLL set against `docs/evidence/wist-package-closure.md`, and CI builds/runs a clean external consumer using only the produced `.nupkg` plus normal NuGet dependencies.

A physical Wist/UT repository split is **not ready** while the non-pilot compatibility adapter remains and while the facade package still vendors the broad UT runtime closure. Repository separation must follow those boundary migrations, not precede them.
