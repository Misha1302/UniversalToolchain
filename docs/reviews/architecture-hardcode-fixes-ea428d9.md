# Architecture hardcode and extensibility fixes for `ea428d9`

Baseline archive: `Wist2-ea428d9.tar.gz`

This patch applies the first safe implementation pass for the hardcode / anti-extensibility review.
It intentionally fixes the most localized architecture leaks without attempting a full runtime redesign.

## Fixed

### 1. `BasicCore.Binding.Binder` no longer knows concrete variable module node names

Before: `BasicCore.Binding.Binder` contained direct checks for `Variable`, `VariableDefinition`, and `VariableDefinitionWithType`.

After:

- `BasicCore` owns a generic binding lifecycle only.
- `IFrontendCoreModule.GetAstBindingRules()` lets modules register AST binding rules.
- `VariablesModule` owns `VariablesBindingRule` and `VariablesAstContracts`.
- `BindingContext` owns external/local symbol state and exposes controlled declaration/lookup APIs.

Why this is correct: the framework core no longer depends on Wist/Variables syntax. New modules can participate in binding by adding a rule instead of editing `BasicCore`.

### 2. Assignment write-target context no longer uses the cross-module raw AST tag `ExpectingWriteTypeInference`

Before: `EqualityModule` added `AddTag("ExpectingWriteTypeInference")`, and `VariablesModule` read it via `AllTags.Contains(...)`.

After:

- `AstNode` has a typed local semantic tag channel.
- `AssignmentSemanticContractIds.WriteTarget` represents write-position semantics.
- `EqualityModule.ValuesSetNodeCreator` marks the assignment target with `AddSemanticTag(AssignmentSemanticContractIds.WriteTarget)`.
- `VariablesVisitor` reads `HasLocalSemanticTag(AssignmentSemanticContractIds.WriteTarget)`.
- The variables bytecode semantic tag was renamed to `WriteTargetTypeInference`; the old `ExpectingWriteTypeInference` property remains only as an obsolete compatibility alias.

Why this is correct: assignment semantics is now a neutral contract, not a private string agreement between two modules. The tag is local, so parent context cannot accidentally turn unrelated descendants into write targets.

### 3. Wist public facade gained open dialect/backend options

Before: `WistEngineOptions` only exposed closed enums: `WistPreset` and `WistBackend`.

After:

- `WistDialectSource` supports shipped preset ids and explicit dialect files.
- `WistEngineOptions.DialectSource` supports non-enum dialect selection.
- `WistEngineOptions.BackendAlias` supports non-enum backend aliases.
- Legacy enums remain for compatibility.

Why this is correct: convenience aliases remain stable, but external/custom dialect and backend selection no longer requires changing public enums.

### 4. `WistEngine` no longer references CIL/DynamicMethod implementation details

Before: `WistEngine.CompileDynamicMethod` directly called `GetBackendSpecificArtifactCompiler<BasicCilCompiler.Execution.CilCompilationOutput>()` and the public facade source also mentioned `DynamicMethod`.

After:

- `WistEngine` delegates typed delegate/function compilation to `IWistDelegateCompiler`.
- `WistCilDelegateCompiler` is the explicit CIL fast-path adapter and is the only Wist facade file that knows `DynamicMethod` / `CilCompilationOutput`.
- `WistEngine.cs` contains no `BasicCilCompiler`, `CilCompilationOutput`, `GetBackendSpecificArtifactCompiler<...>` or `DynamicMethod` references.

Why this is correct: the public facade stays backend-plan oriented. The remaining CIL fast path is isolated in a named adapter instead of leaking through the main facade. A future backend-neutral callable artifact can replace the adapter without changing `WistEngine` flow.

### 5. Preprocessor define parsing moved out of `VariablesVisitor`

Before: `VariablesVisitor` parsed raw preprocessor text with `Text[3..^1].Split()`.

After:

- `InternalPreprocessorLexemesModule` owns `PreprocessorLexemeContracts.TryReadDefineDirective`.
- `VariablesVisitor` consumes the parsed directive contract.

Why this is correct: raw directive syntax is handled by the preprocessor owner. The variable visitor consumes a semantic directive, not raw source slicing.

### 6. Generic diagnostics no longer use `WistThrower` directly

Before: generic lexer/parser/interpreter/runtime layers used `WistThrower`.

After:

- `ToolchainThrower` is the neutral framework diagnostic thrower.
- `WistThrower` remains as an obsolete compatibility alias.
- Generic call sites were migrated to `ToolchainThrower`.

Why this is correct: UniversalToolchain framework layers no longer expose Wist as their diagnostic identity.

### 7. Runtime provider extraction and default compiler capabilities no longer compare/embed raw `"call C#"` in core call sites

Before: `PreparedExecutionBuilder.ExtractAllowedRuntimeProviderTypes` matched `instruction.Operands[0]` against the literal `"call C#"`.

After:

- At the reviewed commit it used `InstructionIntrinsicReader` plus a legacy string decoder; the current pipeline accepts structured intrinsics only.
- It checks `BuiltinIntrinsicSymbols.Core.CallCSharp`.
- `IAbstractIrCompiler` default supported intrinsics now use `CoreDefaultIntrinsicNames` and `LegacyCapabilityNameEncoder` instead of embedding raw strings directly in the interface.

Why this is correct: legacy textual intrinsic encoding is centralized at the compatibility boundary; runtime planning and core defaults work through typed intrinsic symbols.

### 8. Shipped dialect resolver is less tied to one physical source-tree layout

Before: resolver looked only at `AppContext.BaseDirectory + preset.RelativeDialectFilePath`.

After:

- It probes the app base directory, parent directories, and common NuGet `contentFiles/any/<tfm>/...` layouts.
- Missing preset diagnostics list searched locations.

Why this is correct: runtime resolution is less fragile under test, pack, and publish layouts.


### 9. Architecture guardrails added for the fixed hardcode boundaries

Added `Tests/Architecture/HardcodeBoundaryGuardrailTests.cs` with focused negative checks for:

- concrete Variables AST names in `BasicCore`;
- CIL/DynamicMethod implementation details in `WistEngine.cs`;
- raw cross-module write-target tags in `EqualityModule`;
- raw preprocessor directive parsing in `VariablesVisitor`;
- `WistThrower` usage in guarded framework/runtime layers;
- raw `"call C#"` comparison in `PreparedExecutionBuilder`.

Why this is correct: the patch now protects the architectural boundary, not only the currently edited lines.

## Verified locally

Commands run with sidecar .NET SDK and local NuGet package cache:

```bash ci-run=false
# Full Wist.sln restore was attempted separately, but did not complete in this sandbox.
# Project builds below used the existing restored obj assets from the working tree plus the sidecar SDK/package cache.
dotnet build BasicCore/BasicCore.csproj --no-restore -p:Platform='Any CPU' -p:EmitDialectRuntimeManifest=false -v quiet
dotnet build VariablesModule/VariablesModule.csproj --no-restore -p:Platform='Any CPU' -p:EmitDialectRuntimeManifest=false -v quiet
dotnet build UniversalToolchain.Dialects.Wist/UniversalToolchain.Dialects.Wist.csproj --no-restore -p:Platform='Any CPU' -p:EmitDialectRuntimeManifest=false -v quiet
dotnet build UniversalToolchain.Wist/UniversalToolchain.Wist.csproj --no-restore -p:Platform='Any CPU' -p:EmitDialectRuntimeManifest=false -v quiet
dotnet build Tests/Tests.csproj --no-restore -p:Platform='Any CPU' -p:EmitDialectRuntimeManifest=false -p:BuildProjectReferences=false -v quiet
dotnet build UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj --no-restore -p:Platform='Any CPU' -p:EmitDialectRuntimeManifest=false -p:BuildProjectReferences=false -v quiet
dotnet test Tests/Tests.csproj --no-build --no-restore -p:Platform='Any CPU' -p:EmitDialectRuntimeManifest=false --filter "FullyQualifiedName~Tests.Architecture" -v minimal
dotnet test UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj --no-build --no-restore -p:Platform='Any CPU' -p:EmitDialectRuntimeManifest=false --filter "Name=Variables_WriteTargetTypeInference_Path_IsUsed_ForAssignment|Name=Variables_PreprocessorDefineLexeme_RegistersTypeDeterministically|Name=Variables_ObjectBinding_IsRefinedToConcreteType_OnFirstValidContext" -v minimal
```

Results:

- Full `Wist.sln` restore: attempted but did not complete in this sandbox. Direct project restore through the sidecar-only NuGet config is incomplete because the sidecar feed lacks `Microsoft.Extensions.DependencyInjection`; the builds below therefore used existing restored project assets in the working tree.
- `BasicCore`: build succeeded.
- `VariablesModule`: build succeeded.
- `UniversalToolchain.Dialects.Wist`: build succeeded.
- `UniversalToolchain.Wist`: build succeeded.
- `Tests` project: build succeeded with project references disabled after dependency projects were built.
- `UniversalToolchain.Modules.Tests` project: build succeeded with project references disabled after dependency projects were built.
- Architecture guardrail tests: passed `8/8`.
- Focused VariablesModule direct tests: passed `3/3`.

## Not fully completed in this pass

- Full solution build with manifest emission enabled was not completed: MSBuild child node was killed/timed out during long manifest/test dependency builds in this environment.
- Full test suite was not completed in this environment. Several module parity tests require runtime manifest emission/registration paths; with `EmitDialectRuntimeManifest=false`, those tests fail at test-host composition before exercising Variables semantics.
- Existing legacy intrinsic string comparisons remain in backend/optimizer/test code. Runtime provider extraction and core default compiler capability naming were fixed, but full intrinsic canonicalization is a broader migration.
- `WistCilDelegateCompiler` still uses the explicit CIL backend fast path. This is now isolated, not removed. A fully backend-neutral delegate artifact would require a wider backend contract.
