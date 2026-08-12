---
title: Wist package runtime closure evidence
description: Physical UniversalToolchain.Wist runtime closure classification for architecture hardening.
navigation: hidden
status: Internal maintainer evidence for architecture and package-boundary review.
---

# Wist package runtime closure evidence

> Scope: physical `lib/net10.0` runtime closure of `UniversalToolchain.Wist` after migration #332. This is evidence for architecture/production hardening; it is not a proposal to split packages.

## Evidence source

The measured baseline used for classification is the package produced by the canonical LanguagePlan migration candidate that became #332:

- Package: `UniversalToolchain.Wist.0.1.0-alpha.6.nupkg`.
- Package SHA-256: `0c7fdc2d9a42259f4e3c63d76b32d2be204ff5284bbb61157d64001521f83aeb`.
- Package size: `829087` bytes.
- Observed runtime DLL count: **63**.
- Evidence artifact: successful baseline-bearing `Package Compatibility Review` for that candidate.

The architecture/production-hardening branch advances package versions because package payloads changed. It does **not** introduce a new package split or a second runtime-closure owner. The `GetWistLanguagePackRuntimeClosure` target and the facade/LanguagePack ProjectReference topology remain the physical-closure mechanism. The branch adds an explicit `VariablesModule -> CommonExceptions` project reference; `CommonExceptions.dll` was already present in the measured 63-DLL closure, so that edge does not add a new assembly identity.

Therefore the table below is a measured classification baseline, not a claim that the final `0.1.0-alpha.7` bytes are identical to `alpha.6`. The final candidate must reproduce the expected package-surface set through the full baseline-bearing package gate before integration readiness is claimed.

## Classification model

The requested labels describe the **semantic owner** of an assembly. Physical inclusion is stricter: the single `UniversalToolchain.Wist.LanguagePack` advertises all supported Wist features and its runtime component catalog has compile-time references to their implementation types. The public Wist facade also references SSA route types. Therefore semantic-only assemblies can still be physically unconditional in the monolithic package.

- `always required` — shared facade/compiler/runtime/backend/module dependency, or an explicitly advertised LanguagePack component that cannot be removed from the current monolithic closure without changing supported package surface.
- `restricted-only` — implementation selected only by the restricted function-call/SafeMath shipped preset.
- `native/full-only` — implementation owned by full-language or native feature families.
- `SSA-only` — semantic implementation owned by SSA; still physically present today because the facade/LanguagePack directly reference SSA route types.
- `tooling-only` — build-time tooling, not a runtime DLL. `UniversalToolchain.FeatureManifestEmitter.dll` is in this class and is intentionally absent from the measured package runtime surface.
- `accidental/legacy transitive` — no live package/runtime owner found. **None identified in the measured 63-DLL alpha.6 surface.**

## Runtime assemblies

| Assembly | Classification | Bytes | Evidence / note |
|---|---|---:|---|
| `AbstractIrConverters.dll` | `always required` | 7680 |  |
| `AbstractIrExtensions.dll` | `always required` | 13312 |  |
| `ArithmeticModule.dll` | `always required` | 19968 |  |
| `AssemblyFinder.dll` | `always required` | 18432 |  |
| `BasicCilCompiler.dll` | `always required` | 16896 |  |
| `BasicCodeTranslator.dll` | `always required` | 5632 |  |
| `BasicCore.dll` | `always required` | 122368 |  |
| `BasicInterpreter.dll` | `always required` | 23552 |  |
| `BasicLexer.dll` | `always required` | 8704 |  |
| `BasicParser.dll` | `always required` | 8192 |  |
| `BasicStdLib.dll` | `always required` | 8192 | Explicitly exposed by the direct frontend type catalog. |
| `BasicTypesExtensions.dll` | `always required` | 9216 |  |
| `BytecodeDynamicMethodsCompiler.dll` | `always required` | 36352 |  |
| `CSharpInteropModule.dll` | `native/full-only` | 16896 | Selected only by the two full presets. |
| `CommentsModule.dll` | `always required` | 10240 |  |
| `CommonExceptions.dll` | `always required` | 7168 | Already present before the explicit VariablesModule edge. |
| `ConditionsModule.dll` | `always required` | 42496 |  |
| `DotnetHelper.dll` | `always required` | 9216 |  |
| `DynamicMethodWrapper.dll` | `always required` | 6656 |  |
| `EqualityModule.dll` | `always required` | 16384 |  |
| `ExceptionsManager.dll` | `always required` | 7168 |  |
| `FunctionCallsModule.dll` | `restricted-only` | 28160 | Selected only by `function-calls-safe-math` among shipped presets. |
| `GenericMath.dll` | `always required` | 5120 |  |
| `IdentifierModule.dll` | `always required` | 14848 |  |
| `IntermediateRepresentationAbstractions.dll` | `always required` | 31744 |  |
| `InternalPreprocessorLexemesModule.dll` | `always required` | 9216 | Advertised LanguagePack feature and dependency of VariablesModule implementation; not accidental. |
| `LabelsModule.dll` | `native/full-only` | 21504 | Selected only by the two full presets. |
| `ListExtensions.dll` | `always required` | 5120 |  |
| `LoopsModule.dll` | `native/full-only` | 18432 | Selected only by the two full presets. |
| `NativeMathModule.dll` | `native/full-only` | 45568 | Owns native-types/native optimizer implementations used by native/full, pricing and SSA presets. |
| `NumbersModule.dll` | `always required` | 19968 |  |
| `ObjectExtensions.dll` | `always required` | 4608 |  |
| `ParametersSetterModule.dll` | `always required` | 10752 | Not selected by a shipped preset, but remains an advertised LanguagePack feature; not accidental. |
| `SafeMathFunctionsModule.dll` | `restricted-only` | 14848 | Selected only by `function-calls-safe-math`; requires function calls. |
| `ScopesModule.dll` | `always required` | 17408 |  |
| `SemicolonAsNewLineModule.dll` | `always required` | 13312 |  |
| `SettableGettableModule.dll` | `always required` | 4608 |  |
| `UniversalIntermediateRepresentation.dll` | `always required` | 12288 |  |
| `UniversalToolchain.Air.Analysis.dll` | `always required` | 49664 |  |
| `UniversalToolchain.Capabilities.Abstractions.dll` | `always required` | 12800 |  |
| `UniversalToolchain.Capabilities.Core.dll` | `always required` | 32256 |  |
| `UniversalToolchain.Diagnostics.Abstractions.dll` | `always required` | 11776 |  |
| `UniversalToolchain.Dialects.Abstractions.dll` | `always required` | 24576 |  |
| `UniversalToolchain.Dialects.Frontend.dll` | `always required` | 94208 |  |
| `UniversalToolchain.ExpressionTyping.Abstractions.dll` | `always required` | 8704 |  |
| `UniversalToolchain.FeatureSdk.dll` | `always required` | 41984 |  |
| `UniversalToolchain.Functions.Abstractions.dll` | `always required` | 18432 |  |
| `UniversalToolchain.Ir.Abstractions.dll` | `always required` | 16384 |  |
| `UniversalToolchain.Language.Abstractions.dll` | `always required` | 41472 |  |
| `UniversalToolchain.LanguageSdk.dll` | `always required` | 85504 | Canonical planner/plan SDK dependency; runtime does not re-plan. |
| `UniversalToolchain.ModuleContracts.dll` | `always required` | 237056 |  |
| `UniversalToolchain.Runtime.dll` | `always required` | 81408 | Canonical plan execution/runtime owner. |
| `UniversalToolchain.Semantics.Abstractions.dll` | `SSA-only` | 16896 | Semantic dependency of the SSA implementation family. |
| `UniversalToolchain.Ssa.Abstractions.dll` | `SSA-only` | 60416 | SSA contracts. |
| `UniversalToolchain.Ssa.Core.dll` | `SSA-only` | 36352 | SSA core implementation. |
| `UniversalToolchain.Ssa.Emission.dll` | `SSA-only` | 65536 | SSA-to-AIR emission. |
| `UniversalToolchain.Ssa.Lowering.dll` | `SSA-only` | 35328 | AIR-to-SSA lowering. |
| `UniversalToolchain.Ssa.Optimization.dll` | `SSA-only` | 94208 | SSA route/optimization owner; facade and LanguagePack also reference its public route types. |
| `UniversalToolchain.Wist.LanguagePack.dll` | `always required` | 132096 | Canonical Wist package/runtime-provider owner; statically references shipped module implementations. |
| `UniversalToolchain.Wist.dll` | `always required` | 55808 | Public facade. |
| `VariablesModule.dll` | `always required` | 30208 |  |
| `VariablesRuntime.dll` | `always required` | 7680 |  |
| `WhitespacesModule.dll` | `always required` | 10240 |  |

## Decision

**No package graph split in this hardening change.** Package versions are advanced because payloads changed, but the physical dependency topology is not split into preset-specific packages.

The strongest alternative was preset-specific package splitting or removing semantic-only DLLs from the facade package. That would reduce some consumer payload, but the current `Wist.LanguagePack` is a single public feature package whose catalog statically owns all supported implementation types, while the facade has direct SSA route dependencies. Removing DLLs without first changing those ownership/reference boundaries would create load/runtime holes or a second packaging/planning topology. That is a larger architecture change than this hardening work and would conflict with the goal of narrowing existing contracts rather than adding orchestration layers.

Before any future package reduction, first move a proven category boundary behind an existing typed owner, then re-run the full baseline-bearing package gate, package-surface check and clean external consumer. Do not infer trim safety only from a shipped preset not selecting a feature.
