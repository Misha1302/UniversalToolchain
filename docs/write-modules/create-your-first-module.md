---
title: Create Your First Module
description: Build a small Wist frontend module and register it in the canonical LanguagePlan path.
---

# Create Your First Module

This tutorial uses the repository's `TextualAddition` feature. It adds:

```wist
2 plus 3
```

with result:

```text
5
```

The important S11 rule is that implementing syntax is only half of a built-in Wist feature. The feature must also be registered in the typed Wist LanguagePack so `LanguageCompiler` can place it in the canonical `LanguagePlan`.

## Canonical authoring flow

```text
module implementation
  + typed Wist feature/contribution registration
  + dependency declaration
  -> LanguageDefinition
  -> LanguageCompiler
  -> LanguagePlan
  -> LanguageRuntime
```

Generated runtime-manifest metadata is not the Wist semantic-selection owner.

## Files involved

| Concern | Current owner |
|---|---|
| Module entry point | `UniversalToolchain/ArithmeticModule/Module/TextualAdditionModuleImpl.cs` |
| Parser creator | `UniversalToolchain/ArithmeticModule/Creators/TextualAdditionOperationNodeCreator.cs` |
| AST visitor | `UniversalToolchain/ArithmeticModule/Visitors/TextualAdditionAstVisitor.cs` |
| Feature/contribution ids | `UniversalToolchain.Wist.LanguagePack/WistLanguageFeaturePackage.cs` |
| Alias → implementation factory | `UniversalToolchain.Wist.LanguagePack/WistRuntimeComponentCatalog.cs` |
| Typed feature dependencies | `WistLanguageFeaturePackage.GetRequiredFeatures(...)` |
| Behavior tests | `UniversalToolchain.Dialects.Tests/TextualAdditionModuleTests.cs` |

## Step 1. Implement the source-level feature

`TextualAdditionModuleImpl` owns the `plus` token, parser node creator and AST visitor:

```csharp
[DialectModuleAlias("TextualAddition")]
[DialectRuntimeExport("FrontendModule", "TextualAddition")]
[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class TextualAdditionModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"\bplus\b", "TextualAddition", Priority: 110f)
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-30f, new TextualAdditionOperationNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);
    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);
    public void InitAstTranslator(IAstToBytecodeTranslator translator) =>
        translator.AddVisitors(new TextualAdditionAstVisitor());
}
```

The attributes remain useful compatibility/metadata annotations, but they do not by themselves make the module selectable by the canonical Wist planner.

## Step 2. Give the feature stable typed identities

Built-in Wist features use explicit ids in `WistLanguageFeaturePackage.cs`:

```csharp
public static LanguageFeatureId TextualAddition { get; } = new("wist.textual-addition");
```

and:

```csharp
public static LanguageContributionId TextualAdditionModule { get; } =
    new("wist.module.textual-addition");
```

These ids are the semantic identities captured in `LanguageDefinition` and `LanguagePlan`. The user-facing alias `TextualAddition` is only configuration syntax that maps to them.

## Step 3. Bind the alias to the implementation factory

Add the built-in module to `WistRuntimeComponentCatalog.Modules`:

```csharp
Module(
    WistContributionIds.TextualAdditionModule,
    WistFeatureIds.TextualAddition,
    "TextualAddition",
    190,
    static () => typeof(TextualAdditionModuleImpl),
    static services => ActivatorUtilities.CreateInstance<TextualAdditionModuleImpl>(services))
```

This catalog is the Wist-specific translation/materialization bridge:

- the alias resolves to a typed feature/contribution;
- the implementation factory is used only after the plan has selected that contribution;
- catalog order must not become a second semantic planner.

## Step 4. Expose the feature in the package descriptor

`WistLanguageFeaturePackage.CreateFeatures()` must expose the feature:

```csharp
Feature(WistFeatureIds.TextualAddition, WistContributionIds.TextualAdditionModule)
```

`CreateContributions()` obtains module contribution descriptors from the canonical runtime-component catalog. The resulting package descriptor is what `LanguageCompiler` plans against.

## Step 5. Declare typed dependencies

If the feature requires other Wist features, declare them in `GetRequiredFeatures(...)`.

The current `TextualAddition` contract requires scopes and whitespace handling:

```csharp
if (id == WistFeatureIds.TextualAddition)
    return [WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
```

This is deliberately different from forcing every dialect file to repeat transitive requirements. `LanguageCompiler` closes the feature dependency graph.

The example expression also uses numeric literals, so the demo dialect requests `Numbers` explicitly:

```text
dialect TextualAdditionDemo
use TextualAddition,Numbers
backend cil,interpreter
```

The resulting `LanguagePlan` also contains the required `Scopes` and `Whitespaces` contributions even though the file does not need to repeat them.

## Step 6. Keep exclusion semantics fail-closed

A dialect `exclude` directive is translated to `LanguageDefinition.ExcludedContributions`.

If a new feature declares a dependency whose contribution the dialect excludes, `LanguageCompiler` must fail with the canonical planning diagnostic instead of silently reactivating the module.

Do not implement a Wist-only dependency resolver to work around this. Typed feature dependencies and exclusions already belong to the generic planner.

## Step 7. Test selection and parity

The repository's `TextualAdditionModuleTests` verifies:

- `2 plus 3` executes when `TextualAddition` is selected;
- `2 plus 3 * 4` preserves addition-level precedence;
- the same syntax fails when the module is omitted;
- compiler and interpreter agree.

For a built-in feature, also keep architecture/package tests that prove:

- its feature and contribution ids are present in the package descriptor;
- alias translation maps to the expected typed feature;
- dependency closure is owned by `LanguageCompiler`;
- minimal plans do not materialize unrelated module assemblies.

## Step 8. Do not use runtime manifests as Wist registration

The repository still has generic runtime-manifest emitter/serializer infrastructure. A module project may carry `DialectRuntimeExport` metadata or emit a manifest for compatibility/tooling scenarios.

That is **not** the current Wist registration procedure.

Do not solve a missing Wist alias by:

- hand-writing `.dialect.runtime.json`;
- enabling manifest emission and assuming the canonical planner will discover the module;
- scanning assemblies for module attributes at runtime;
- adding a second selected-runtime plan after `LanguageCompiler`.

Fix the typed Wist LanguagePack/catalog registration instead.

See [Runtime Manifests](/write-modules/runtime-manifests) for the retained manifest subsystem boundary.

## Step 9. Run repository checks

From repository root:

```bash ci-run=false
./build.sh --skip-docs --skip-pack
```

On Windows:

```powershell
./build.ps1 -SkipDocs -SkipPack
```

The canonical gate builds the repository, enforces architecture boundaries and runs the exact test contract. When the test count changes intentionally, do not edit `eng/test-counts.json` first: obtain a semantically clean observed census, then reconcile the manifest and verification docs in a separate reviewed change.

## Definition of done

A built-in Wist module is complete when:

- syntax/parser/AST behavior is implemented;
- stable feature and contribution ids exist;
- alias and implementation factory are registered in `WistRuntimeComponentCatalog`;
- the package descriptor exposes the feature/contribution;
- typed feature dependencies are declared;
- canonical planning selects the expected contribution;
- both supported backend routes agree;
- omitted/excluded behavior fails as intended;
- no manifest-backed or service-container planner is needed to make it work.
