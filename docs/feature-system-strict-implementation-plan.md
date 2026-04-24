# Strict Implementation Plan: Feature System + Rule UX Layer for UniversalToolchain

## 0. Purpose of this document

This document describes a strict, linear implementation plan for the strong MVP described in `docs/strong-mvp-feature-system-plan.md`.

The goal is not to brainstorm features. The goal is to define a concrete sequence of changes that can be implemented one PR at a time, without parallel development branches and without architectural shortcuts.

The plan must satisfy the project constraints:

```text
- UniversalToolchain remains the primary product.
- Wist remains a reference language/proving ground.
- No hardcoded dialect assumptions.
- No hardcoded shipped profile branching.
- No hidden source of framework truth.
- No runtime activation rewrite in this stage.
- No reflection cleanup in this stage.
- No direct module/backend activation from the feature system.
- Existing manifest-backed dialect runtime remains canonical.
- New convenience APIs stay optional.
- Every behavior change gets tests.
- Every meaningful architecture change gets docs.
```

The plan is intentionally sequential:

```text
PR 1 must be completed before PR 2.
PR 2 must be completed before PR 3.
No feature branch should depend on another unfinished feature branch.
Each PR must leave master in a coherent and testable state.
```

---

## 1. Global implementation strategy

### 1.1. What we are adding

We are adding a high-level authoring and UX layer over the existing dialect runtime.

This layer consists of:

```text
Feature metadata
Feature explanation
Function descriptors
Safe function pack
Rule diagnostics
Rule declarations
RuleSet API
Rule schema
Product profiles
CLI/docs support
```

### 1.2. What we are not adding

We are not doing this in the MVP:

```text
- reflection cleanup;
- runtime activation rewrite;
- new general-purpose type system;
- user-defined functions;
- closures;
- imports;
- packages;
- classes;
- arrays/lists;
- real sandbox;
- language server;
- IDE tooling;
- macro system.
```

### 1.3. Source of truth rule

The most important architectural rule:

```text
Feature System is a projection/explanation layer.
It is not a runtime activation layer.
```

The source of truth remains:

```text
dialect definition
→ compiled dialect slice
→ build plan
→ selected runtime plan
→ runtime configuration
→ host
```

The feature system may inspect selected runtime components and produce UX metadata. It must not choose runtime components itself.

### 1.4. Linear dependency chain

The implementation must follow this dependency chain:

```text
1. Feature metadata contracts
2. Feature projection from selected runtime plan
3. Diagnostics contracts
4. Function descriptor contracts
5. Function availability diagnostics
6. SafeMathFunctions descriptors
7. SafeMathFunctions runtime behavior
8. Function call authoring support if missing
9. IfExpression support
10. LetBindings polish
11. Rule model contracts
12. Rule declaration parser/extractor
13. RuleSet compile API
14. RuleSet runtime API
15. Rule schema/introspection
16. Product profiles
17. CLI commands
18. README/docs polishing
```

This is intentionally not parallelized.

---

## 2. Target user-facing MVP

At the end of the plan, this scenario must work:

```csharp
using var runtime = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithShippedDialectPreset(WistShippedDialectPresets.PricingRules)
    .Build();

var compileResult = runtime.CompileRuleSet("""
rule FinalPrice(price: number, quantity: number, discount: number, maxDiscount: number) -> number {
    let base = price * quantity
    let discountValue = clamp(base * discount, 0.0, maxDiscount)
    let result = base - discountValue

    if result < 0.0 then 0.0 else result
}
""");

if (!compileResult.IsSuccess)
{
    Console.WriteLine(compileResult.FormatDiagnostics());
    return;
}

var result = compileResult.RuleSet.Run(
    "FinalPrice",
    new Dictionary<string, object?>
    {
        ["price"] = 100.0,
        ["quantity"] = 3.0,
        ["discount"] = 0.15,
        ["maxDiscount"] = 50.0
    });
```

Expected result:

```text
255.0
```

The same rule must be executable through:

```text
compiler / cil
interpreter
```

when the selected dialect enables both backends.

---

## 3. Project layout proposal

The exact project layout may be adjusted to current solution conventions, but the conceptual separation should remain.

### 3.1. Feature metadata layer

Preferred namespaces:

```text
UniversalToolchain.Features.Abstractions
UniversalToolchain.Features.Core
UniversalToolchain.Dialects.Wist.Features
```

Possible folders/projects:

```text
UniversalToolchain/UniversalToolchain.Features.Abstractions/
UniversalToolchain/UniversalToolchain.Features.Core/
UniversalToolchain/UniversalToolchain.Dialects.Wist/Features/
UniversalToolchain/UniversalToolchain.Dialects.Tests/Features/
```

If adding new projects is too much for the first PR, start inside existing dialect/core projects, but keep namespaces clean and avoid coupling to concrete Wist profiles.

### 3.2. Function descriptor layer

Preferred namespaces:

```text
UniversalToolchain.Functions.Abstractions
UniversalToolchain.Functions.Core
UniversalToolchain.Dialects.Wist.Functions
```

Possible folders/projects:

```text
UniversalToolchain/UniversalToolchain.Functions.Abstractions/
UniversalToolchain/UniversalToolchain.Functions.Core/
UniversalToolchain/UniversalToolchain.Dialects.Wist/Functions/
```

### 3.3. Rule layer

Preferred namespaces:

```text
UniversalToolchain.Rules.Abstractions
UniversalToolchain.Rules.Core
UniversalToolchain.Dialects.Wist.Rules
```

Possible folders/projects:

```text
UniversalToolchain/UniversalToolchain.Rules.Abstractions/
UniversalToolchain/UniversalToolchain.Rules.Core/
UniversalToolchain/UniversalToolchain.Dialects.Wist/Rules/
```

### 3.4. Test folders

Use existing test organization principles:

```text
Tests/Core
Tests/Backends
Tests/Internal
Tests/Infrastructure
Tests/Stress
UniversalToolchain.Dialects.Tests/Features
UniversalToolchain.Dialects.Tests/Functions
UniversalToolchain.Dialects.Tests/Rules
UniversalToolchain.Dialects.Tests/ProductProfiles
```

Do not add new tests to legacy base classes.

---

## 4. Core abstractions

This section defines the intended abstractions before the PR sequence.

---

## 4.1. Feature metadata abstractions

### 4.1.1. LanguageFeatureId

Purpose:

```text
Stable identifier for a user-facing language capability.
```

Shape:

```csharp
public readonly record struct LanguageFeatureId(string Value)
{
    public override string ToString()
    {
        return Value;
    }
}
```

Validation rule:

```text
Value must not be null/empty/whitespace.
Use Thrower helpers if constructor validation is added.
```

Suggested ids:

```text
NativeNumbers
StandardNumbers
ExternalParameters
LetBindings
IfExpression
RuleDeclarations
TypedRuleParameters
SafeMathFunctions
ValidationFunctions
CSharpInterop
Loops
Labels
Comments
BooleanLogic
ComparisonLogic
```

### 4.1.2. LanguageFeatureKind

Purpose:

```text
Classifies features for reports and tooling.
```

Shape:

```csharp
public enum LanguageFeatureKind
{
    Syntax,
    FunctionSet,
    TypeSystem,
    RuleModel,
    HostIntegration,
    Diagnostic,
    Optimization,
    Interop
}
```

### 4.1.3. LanguageFeatureSymbolDescriptor

Purpose:

```text
Describes user-facing symbols provided by a feature.
```

Examples:

```text
function clamp(number, number, number) -> number
syntax if condition then expr else expr
syntax rule Name(param: type) -> type { ... }
```

Shape:

```csharp
public sealed record LanguageFeatureSymbolDescriptor(
    string Name,
    LanguageFeatureSymbolKind Kind,
    string Signature,
    string Description);
```

### 4.1.4. LanguageFeatureDescriptor

Purpose:

```text
Describes a user-facing feature and how it maps to selected runtime components.
```

Shape:

```csharp
public sealed record LanguageFeatureDescriptor(
    LanguageFeatureId FeatureId,
    string DisplayName,
    LanguageFeatureKind Kind,
    IReadOnlyList<string> RequiredRuntimeComponentAliases,
    IReadOnlyList<LanguageFeatureId> RequiredFeatures,
    IReadOnlyList<LanguageFeatureSymbolDescriptor> ProvidedSymbols,
    IReadOnlyList<string> SupportedBackendAliases,
    string ShortDescription);
```

Rules:

```text
- RequiredRuntimeComponentAliases are aliases expected in selected runtime plan.
- RequiredFeatures are other features that must be available.
- SupportedBackendAliases describes user-facing backend support.
- Descriptors must be immutable.
- Ordering must be deterministic.
- No descriptor may branch on dialect names.
```

### 4.1.5. ILanguageFeatureCatalog

Purpose:

```text
Provides descriptors known to the current product/runtime package.
```

Shape:

```csharp
public interface ILanguageFeatureCatalog
{
    IReadOnlyList<LanguageFeatureDescriptor> GetFeatures();

    bool TryGetFeature(
        LanguageFeatureId featureId,
        out LanguageFeatureDescriptor? descriptor);
}
```

Implementation:

```text
WistLanguageFeatureCatalog
```

Important:

```text
The catalog describes known features.
It does not activate modules.
It does not read files.
It does not build service providers.
```

### 4.1.6. DialectFeatureExplanation

Purpose:

```text
Projection of selected runtime surface into user-facing capability information.
```

Shape:

```csharp
public sealed record DialectFeatureExplanation(
    string DialectName,
    IReadOnlyList<AvailableLanguageFeature> AvailableFeatures,
    IReadOnlyList<UnavailableLanguageFeature> UnavailableFeatures,
    IReadOnlyList<LanguageFeatureSymbolDescriptor> AvailableSymbols,
    IReadOnlyList<DialectFeatureBackendSupport> BackendSupport);
```

### 4.1.7. DialectFeatureExplanationProjector

Purpose:

```text
Converts composition/runtime selection into feature explanation.
```

Inputs:

```text
DialectFrameworkCompositionResult
ILanguageFeatureCatalog
```

Output:

```text
DialectFeatureExplanation
```

Hard rules:

```text
- Must not create WistDialectExecutionHost.
- Must not call backend registrars.
- Must not instantiate runtime modules.
- Must only inspect selected aliases/types already available through composition/selection.
```

---

## 4.2. Diagnostics abstractions

Diagnostics should be created before the RuleSet API because functions, features and rules all need common reporting.

### 4.2.1. RuleDiagnostic

Purpose:

```text
Stable user-facing diagnostic for high-level rule authoring.
```

Shape:

```csharp
public sealed record RuleDiagnostic(
    string Code,
    RuleDiagnosticSeverity Severity,
    string Message,
    SourceSpan? Span,
    IReadOnlyList<RuleDiagnosticHint> Hints);
```

### 4.2.2. RuleDiagnosticSeverity

```csharp
public enum RuleDiagnosticSeverity
{
    Info,
    Warning,
    Error
}
```

### 4.2.3. SourceSpan

```csharp
public sealed record SourceSpan(
    string SourceName,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
```

MVP rule:

```text
SourceSpan can be null.
Do not block implementation on perfect span support.
Add spans incrementally.
```

### 4.2.4. RuleDiagnosticFormatter

Purpose:

```text
Formats diagnostics deterministically for CLI/docs/tests.
```

Shape:

```csharp
public sealed class RuleDiagnosticFormatter
{
    public string Format(IReadOnlyList<RuleDiagnostic> diagnostics)
    {
        // Deterministic ordering and formatting.
    }
}
```

Suggested diagnostic code groups:

```text
WST-FEAT-*   feature availability errors
WST-FUNC-*   function resolution errors
WST-TYPE-*   type errors
WST-BIND-*   binding/name errors
WST-RULE-*   rule declaration errors
WST-BACK-*   backend support errors
```

---

## 4.3. Function descriptor abstractions

### 4.3.1. FunctionTypeDescriptor

Purpose:

```text
Represents the high-level type known to function descriptors.
```

Shape:

```csharp
public sealed record FunctionTypeDescriptor(string Name);
```

MVP types:

```text
number
bool
```

Mapping:

```text
number -> double
bool -> bool
```

### 4.3.2. FunctionParameterDescriptor

```csharp
public sealed record FunctionParameterDescriptor(
    string Name,
    FunctionTypeDescriptor Type);
```

### 4.3.3. FunctionPurity

```csharp
public enum FunctionPurity
{
    Pure,
    ReadsHostState,
    HasSideEffects
}
```

MVP functions should be:

```text
Pure
```

### 4.3.4. BuiltinFunctionDescriptor

```csharp
public sealed record BuiltinFunctionDescriptor(
    string Name,
    LanguageFeatureId FeatureId,
    IReadOnlyList<FunctionParameterDescriptor> Parameters,
    FunctionTypeDescriptor ReturnType,
    FunctionPurity Purity,
    IReadOnlyList<string> SupportedBackendAliases);
```

### 4.3.5. BuiltinFunctionResolution

```csharp
public sealed record BuiltinFunctionResolution(
    bool IsSuccess,
    BuiltinFunctionDescriptor? Descriptor,
    FunctionTypeDescriptor? ReturnType,
    IReadOnlyList<RuleDiagnostic> Diagnostics);
```

### 4.3.6. IBuiltinFunctionCatalog

```csharp
public interface IBuiltinFunctionCatalog
{
    IReadOnlyList<BuiltinFunctionDescriptor> GetFunctions();

    BuiltinFunctionResolution Resolve(
        string name,
        IReadOnlyList<FunctionTypeDescriptor> argumentTypes,
        DialectFeatureExplanation featureExplanation,
        string backendAlias);
}
```

Rules:

```text
- Unknown function returns WST-FUNC-001.
- Function descriptor exists but feature unavailable returns WST-FUNC-002.
- Unsupported backend returns WST-FUNC-003.
- Wrong argument count returns WST-FUNC-004.
- Wrong argument type returns WST-FUNC-005.
```

---

## 4.4. Rule model abstractions

### 4.4.1. RuleTypeDescriptor

```csharp
public sealed record RuleTypeDescriptor(string Name, Type RuntimeType);
```

MVP:

```text
number -> typeof(double)
bool -> typeof(bool)
```

### 4.4.2. RuleParameterDescriptor

```csharp
public sealed record RuleParameterDescriptor(
    string Name,
    RuleTypeDescriptor Type,
    bool IsRequired);
```

### 4.4.3. CompiledRuleDescriptor

```csharp
public sealed record CompiledRuleDescriptor(
    string Name,
    IReadOnlyList<RuleParameterDescriptor> Parameters,
    RuleTypeDescriptor ReturnType);
```

### 4.4.4. ICompiledRule

```csharp
public interface ICompiledRule
{
    CompiledRuleDescriptor Descriptor { get; }

    object? Run(IReadOnlyDictionary<string, object?> arguments);
}
```

### 4.4.5. ICompiledRuleSet

```csharp
public interface ICompiledRuleSet
{
    IReadOnlyList<CompiledRuleDescriptor> Rules { get; }

    bool TryGetRule(
        string name,
        out ICompiledRule? rule);

    object? Run(
        string ruleName,
        IReadOnlyDictionary<string, object?> arguments);
}
```

### 4.4.6. RuleSetCompileResult

```csharp
public sealed class RuleSetCompileResult
{
    public bool IsSuccess { get; }

    public ICompiledRuleSet? RuleSet { get; }

    public IReadOnlyList<RuleDiagnostic> Diagnostics { get; }
}
```

### 4.4.7. RuleSetSchema

```csharp
public sealed record RuleSetSchema(
    IReadOnlyList<RuleSchema> Rules);

public sealed record RuleSchema(
    string Name,
    IReadOnlyList<RuleParameterSchema> Parameters,
    string ReturnType,
    IReadOnlyList<LanguageFeatureId> UsedFeatures);

public sealed record RuleParameterSchema(
    string Name,
    string Type,
    bool IsRequired);
```

---

# 5. Strict PR sequence

The rest of this document defines the exact linear PR plan.

Every PR must:

```text
- build independently;
- pass tests independently;
- leave master usable;
- not depend on unfinished future PRs;
- avoid broad rewrites;
- include docs if behavior/architecture changes;
- include tests for every introduced contract.
```

---

## PR 1 — Add high-level feature metadata contracts

### Goal

Introduce immutable feature metadata abstractions with no behavior changes.

### Why first

Every later layer needs a way to describe user-facing capabilities. This must exist before function packs, feature explanations or product profiles.

### Scope

Add contracts only:

```text
LanguageFeatureId
LanguageFeatureKind
LanguageFeatureSymbolKind
LanguageFeatureSymbolDescriptor
LanguageFeatureDescriptor
ILanguageFeatureCatalog
```

### Suggested files

```text
UniversalToolchain/UniversalToolchain.Features.Abstractions/LanguageFeatureId.cs
UniversalToolchain/UniversalToolchain.Features.Abstractions/LanguageFeatureKind.cs
UniversalToolchain/UniversalToolchain.Features.Abstractions/LanguageFeatureSymbolKind.cs
UniversalToolchain/UniversalToolchain.Features.Abstractions/LanguageFeatureSymbolDescriptor.cs
UniversalToolchain/UniversalToolchain.Features.Abstractions/LanguageFeatureDescriptor.cs
UniversalToolchain/UniversalToolchain.Features.Abstractions/ILanguageFeatureCatalog.cs
```

If creating a new project is too expensive, place under an existing abstractions/core project but keep namespace names clear.

### Implementation details

1. Add immutable records/enums.
2. Add minimal validation only where easy and consistent with project rules.
3. Do not reference Wist concrete modules from this project.
4. Do not add runtime behavior.
5. Do not add service registration yet unless needed by tests.

### Tests

```text
LanguageFeatureId_ToString_ReturnsValue
LanguageFeatureDescriptor_CanRepresentSyntaxFeature
LanguageFeatureDescriptor_CanRepresentFunctionSetFeature
FeatureDescriptor_CollectionsAreStableSnapshots_IfConstructorNormalizes
```

### Acceptance criteria

```text
- New abstractions compile.
- No runtime behavior changes.
- No dependency on Wist module implementations.
```

### Explicitly forbidden in PR 1

```text
- No SafeMathFunctions.
- No Wist feature catalog.
- No runtime projection.
- No service provider changes.
- No CLI changes.
```

---

## PR 2 — Add Wist feature catalog for existing capabilities

### Goal

Add a Wist-specific feature catalog that describes existing known language capabilities without changing behavior.

### Why after PR 1

Now that contracts exist, Wist can expose descriptors for already existing concepts.

### Scope

Add:

```text
WistLanguageFeatureIds
WistLanguageFeatureCatalog
Wist feature descriptors for existing modules/capabilities
service registration helper if needed
```

Initial feature descriptors should cover existing concepts only, for example:

```text
StandardNumbers
NativeNumbers
ArithmeticExpressions
BooleanLogic
ComparisonLogic
EqualityLogic
Variables
Scopes
Loops
Labels
Comments
SemicolonAsNewLine
CSharpInterop
CompilerBackend
InterpreterBackend
```

### Suggested files

```text
UniversalToolchain/UniversalToolchain.Dialects.Wist/Features/WistLanguageFeatureIds.cs
UniversalToolchain/UniversalToolchain.Dialects.Wist/Features/WistLanguageFeatureCatalog.cs
UniversalToolchain/UniversalToolchain.Dialects.Wist/Features/WistFeatureServiceCollectionExtensions.cs
UniversalToolchain/UniversalToolchain.Dialects.Tests/Features/WistLanguageFeatureCatalogTests.cs
```

### Implementation details

1. Implement descriptors with required runtime component aliases.
2. Keep descriptors data-only.
3. Deterministically order descriptors by `FeatureId.Value`.
4. Do not activate anything.
5. Do not branch by dialect name.

Example descriptor:

```csharp
new LanguageFeatureDescriptor(
    WistLanguageFeatureIds.CSharpInterop,
    "C# interop",
    LanguageFeatureKind.Interop,
    ["CSharpInterop"],
    [],
    [],
    ["interpreter", "cil"],
    "Allows trusted runtime access to selected C# interop forms.");
```

### Tests

```text
WistLanguageFeatureCatalog_GetFeatures_ReturnsDeterministicOrder
WistLanguageFeatureCatalog_TryGetFeature_KnownFeature_ReturnsDescriptor
WistLanguageFeatureCatalog_TryGetFeature_UnknownFeature_ReturnsFalse
WistLanguageFeatureCatalog_Descriptors_DoNotUseDialectNames
```

### Acceptance criteria

```text
- Existing features are described as metadata.
- No runtime behavior changes.
- No feature report yet.
```

### Explicitly forbidden in PR 2

```text
- No projection from selected runtime plan yet.
- No SafeMathFunctions.
- No RuleSet API.
```

---

## PR 3 — Add feature explanation projection

### Goal

Project selected runtime surface into available/unavailable user-facing features.

### Why after PR 2

Feature descriptors exist. Now they can be projected from actual dialect composition results.

### Scope

Add:

```text
AvailableLanguageFeature
UnavailableLanguageFeature
DialectFeatureBackendSupport
DialectFeatureExplanation
DialectFeatureExplanationProjector
DialectFeatureExplanationFormatter
```

### Suggested files

```text
UniversalToolchain/UniversalToolchain.Features.Core/AvailableLanguageFeature.cs
UniversalToolchain/UniversalToolchain.Features.Core/UnavailableLanguageFeature.cs
UniversalToolchain/UniversalToolchain.Features.Core/DialectFeatureBackendSupport.cs
UniversalToolchain/UniversalToolchain.Features.Core/DialectFeatureExplanation.cs
UniversalToolchain/UniversalToolchain.Features.Core/DialectFeatureExplanationProjector.cs
UniversalToolchain/UniversalToolchain.Features.Core/DialectFeatureExplanationFormatter.cs
UniversalToolchain/UniversalToolchain.Dialects.Tests/Features/DialectFeatureExplanationProjectorTests.cs
UniversalToolchain/UniversalToolchain.Dialects.Tests/Features/DialectFeatureExplanationFormatterTests.cs
```

### Implementation details

1. Projector input:

```text
DialectFrameworkCompositionResult
ILanguageFeatureCatalog
```

2. Extract selected component aliases from selected runtime plan.
3. Mark feature available if all required aliases/features are available.
4. Mark feature unavailable with reasons otherwise.
5. Available symbols are concatenated from available descriptors.
6. Formatter sorts everything deterministically.
7. Projector must not create host or service provider.

### Important design decision

For PR 3, use simple dependency model:

```text
all required aliases must be present
all required feature ids must be available
```

No complex OR requirements.

### Tests

```text
Project_MinimalArithmetic_ReturnsOnlySelectedFeatures
Project_FullDefault_ReturnsCSharpInteropWhenSelected
Project_RestrictedSandbox_DoesNotReturnCSharpInterop
Project_WhenRequiredAliasMissing_ReturnsUnavailableReason
Project_RepeatedCalls_ReturnEquivalentExplanation
Format_RepeatedCalls_ReturnSameText
Project_DoesNotCreateBackendRegistrationSideEffects
```

### Acceptance criteria

```text
- Existing dialects can be explained as user-facing feature sets.
- Projection is deterministic.
- Projection has no runtime activation side effects.
```

### Explicitly forbidden in PR 3

```text
- No new language features.
- No new parser syntax.
- No CLI unless tiny and separately approved.
```

---

## PR 4 — Add rule diagnostics contracts and formatter

### Goal

Introduce high-level diagnostics used by functions, rules and schemas later.

### Why before function descriptors

Function resolution must return diagnostics instead of raw exceptions. Rule parsing/compilation will also need diagnostics.

### Scope

Add:

```text
RuleDiagnostic
RuleDiagnosticSeverity
SourceSpan
RuleDiagnosticHint
RuleDiagnosticFormatter
RuleDiagnosticCodes static constants or grouped classes
```

### Suggested files

```text
UniversalToolchain/UniversalToolchain.Rules.Abstractions/RuleDiagnostic.cs
UniversalToolchain/UniversalToolchain.Rules.Abstractions/RuleDiagnosticSeverity.cs
UniversalToolchain/UniversalToolchain.Rules.Abstractions/SourceSpan.cs
UniversalToolchain/UniversalToolchain.Rules.Abstractions/RuleDiagnosticHint.cs
UniversalToolchain/UniversalToolchain.Rules.Core/RuleDiagnosticFormatter.cs
UniversalToolchain/UniversalToolchain.Rules.Core/RuleDiagnosticCodes.cs
UniversalToolchain/UniversalToolchain.Dialects.Tests/Rules/RuleDiagnosticFormatterTests.cs
```

### Implementation details

1. Use immutable records.
2. `SourceSpan` may be null in diagnostics.
3. Formatter must be deterministic.
4. Formatter output should be human-readable.
5. Keep this independent from Wist parser internals.

### Suggested diagnostic code groups

```text
WST-FEAT-001 Unknown feature.
WST-FEAT-002 Feature is not available in the current dialect.
WST-FUNC-001 Unknown function.
WST-FUNC-002 Function is not available in the current dialect.
WST-FUNC-003 Function is not supported by backend.
WST-FUNC-004 Wrong function argument count.
WST-FUNC-005 Wrong function argument type.
WST-RULE-001 Duplicate rule name.
WST-RULE-002 Unknown rule type.
WST-RULE-003 Rule return type mismatch.
WST-RULE-004 Duplicate rule parameter name.
WST-BIND-001 Unknown binding.
WST-BIND-002 Binding name conflict.
WST-TYPE-001 Type mismatch.
```

### Tests

```text
Format_NoDiagnostics_ReturnsEmptyOrSuccessText
Format_SingleDiagnostic_ContainsCodeAndMessage
Format_WithSourceSpan_ContainsLineAndColumn
Format_WithHints_ContainsHints
Format_RepeatedCalls_AreDeterministic
```

### Acceptance criteria

```text
- Diagnostics contracts exist.
- No behavior changes.
- Later PRs can return diagnostics consistently.
```

### Explicitly forbidden in PR 4

```text
- No rule parser.
- No function resolver.
- No language behavior changes.
```

---

## PR 5 — Add builtin function descriptor contracts

### Goal

Introduce function descriptor and resolution contracts without implementing runtime functions yet.

### Why after diagnostics

Resolution needs diagnostics.

### Scope

Add:

```text
FunctionTypeDescriptor
FunctionParameterDescriptor
FunctionPurity
BuiltinFunctionDescriptor
BuiltinFunctionResolution
IBuiltinFunctionCatalog
```

### Suggested files

```text
UniversalToolchain/UniversalToolchain.Functions.Abstractions/FunctionTypeDescriptor.cs
UniversalToolchain/UniversalToolchain.Functions.Abstractions/FunctionParameterDescriptor.cs
UniversalToolchain/UniversalToolchain.Functions.Abstractions/FunctionPurity.cs
UniversalToolchain/UniversalToolchain.Functions.Abstractions/BuiltinFunctionDescriptor.cs
UniversalToolchain/UniversalToolchain.Functions.Abstractions/BuiltinFunctionResolution.cs
UniversalToolchain/UniversalToolchain.Functions.Abstractions/IBuiltinFunctionCatalog.cs
```

### Implementation details

1. Keep contracts generic and Wist-independent where possible.
2. Function descriptors reference `LanguageFeatureId`.
3. Resolution returns diagnostics, not exceptions for normal authoring errors.
4. Do not implement SafeMathFunctions yet.

### Tests

```text
BuiltinFunctionDescriptor_CanRepresentClampSignature
BuiltinFunctionResolution_CanRepresentSuccess
BuiltinFunctionResolution_CanRepresentFailureDiagnostics
```

### Acceptance criteria

```text
- Function descriptor contracts compile.
- No runtime behavior changes.
```

### Explicitly forbidden in PR 5

```text
- No function parser.
- No SafeMathFunctions implementation.
- No CIL/interpreter changes.
```

---

## PR 6 — Add Wist builtin function catalog and resolver

### Goal

Add Wist-specific function catalog/resolver infrastructure, still without adding runtime SafeMath behavior.

### Why after PR 5

Contracts exist. Now Wist can provide catalog/resolution logic.

### Scope

Add:

```text
WistFunctionTypeDescriptors
WistBuiltinFunctionCatalog
BuiltinFunctionResolver implementation
empty or existing-function descriptors if any are already safe to expose
```

For PR 6, it is acceptable for the catalog to be empty or contain only descriptors that are already supported.

### Suggested files

```text
UniversalToolchain/UniversalToolchain.Dialects.Wist/Functions/WistFunctionTypeDescriptors.cs
UniversalToolchain/UniversalToolchain.Dialects.Wist/Functions/WistBuiltinFunctionCatalog.cs
UniversalToolchain/UniversalToolchain.Dialects.Wist/Functions/WistBuiltinFunctionResolver.cs
UniversalToolchain/UniversalToolchain.Dialects.Tests/Functions/WistBuiltinFunctionCatalogTests.cs
UniversalToolchain/UniversalToolchain.Dialects.Tests/Functions/WistBuiltinFunctionResolverTests.cs
```

### Implementation details

Resolution algorithm:

```text
1. Filter descriptors by name using Ordinal comparison.
2. If no descriptors, return WST-FUNC-001.
3. Filter by feature availability.
4. If none available, return WST-FUNC-002.
5. Filter by backend alias.
6. If none supported, return WST-FUNC-003.
7. Match argument count.
8. If no count match, return WST-FUNC-004.
9. Match argument types.
10. If no type match, return WST-FUNC-005.
11. Return success.
```

Ordering:

```text
name
arity
parameter type names
return type
```

### Tests

```text
Resolve_UnknownFunction_ReturnsUnknownFunctionDiagnostic
Resolve_FunctionWithUnavailableFeature_ReturnsUnavailableDiagnostic
Resolve_FunctionWithUnsupportedBackend_ReturnsBackendDiagnostic
Resolve_WrongArgumentCount_ReturnsArgumentCountDiagnostic
Resolve_WrongArgumentType_ReturnsArgumentTypeDiagnostic
Resolve_ValidDescriptor_ReturnsSuccess
Resolve_RepeatedCalls_AreDeterministic
```

### Acceptance criteria

```text
- Wist function resolution infrastructure exists.
- No new runtime functions are required yet.
- No parser/runtime changes.
```

### Explicitly forbidden in PR 6

```text
- No SafeMath runtime implementation.
- No new syntax required.
```

---

## PR 7 — Add SafeMathFunctions descriptors and feature projection only

### Goal

Introduce SafeMathFunctions as metadata/descriptors before runtime execution.

### Why separate from runtime behavior

This keeps the step small and validates feature/function metadata first.

### Scope

Add:

```text
SafeMathFunctions feature id
SafeMathFunctions feature descriptor
function descriptors for min/max/abs/clamp/round
feature projection coverage
function resolver coverage
```

### Suggested descriptors

```text
min(number left, number right) -> number
max(number left, number right) -> number
abs(number value) -> number
clamp(number value, number min, number max) -> number
round(number value, number digits) -> number
```

### Required runtime alias decision

Pick one MVP path:

```text
Option A: SafeMathFunctions requires NativeTypes.
Option B: SafeMathFunctions requires Numbers.
```

Recommended MVP:

```text
Option A: require NativeTypes.
```

Reason:

```text
Native numeric path is more suitable for typed rule MVP and CIL performance story.
```

### Tests

```text
WistLanguageFeatureCatalog_ContainsSafeMathFunctions
WistBuiltinFunctionCatalog_ContainsSafeMathFunctionDescriptors
Resolve_Clamp_WhenSafeMathFeatureAvailable_ReturnsSuccess
Resolve_Clamp_WhenSafeMathFeatureUnavailable_ReturnsUnavailableDiagnostic
FeatureProjection_WithSafeMathAlias_ReturnsSafeMathFunctions
FeatureProjection_WithoutSafeMathAlias_ReportsSafeMathUnavailable
```

### Acceptance criteria

```text
- Feature reports can mention SafeMathFunctions.
- Function resolver can resolve SafeMath descriptors if feature is selected.
- Runtime execution is not required yet.
```

### Explicitly forbidden in PR 7

```text
- No parser changes.
- No CIL/interpreter implementation yet.
```

---

## PR 8 — Implement SafeMathFunctions runtime support

### Goal

Make SafeMathFunctions actually executable by interpreter and compiler.

### Why after descriptors

Now metadata and diagnostics already exist. Runtime behavior can be plugged into the described feature.

### Scope

Add:

```text
SafeMathFunctions module
runtime manifest entry
intrinsic descriptor provider if needed
interpreter implementation
CIL implementation
backend support declarations
parity tests
negative tests
```

### Suggested files

```text
UniversalToolchain/Modules/SafeMathFunctionsModule/... or matching existing module layout
UniversalToolchain/Intrinsics/SafeMath/... if intrinsic structure is used
UniversalToolchain/UniversalToolchain.Dialects.Tests/Functions/SafeMathFunctionsExecutionTests.cs
UniversalToolchain/UniversalToolchain.Dialects.Tests/Functions/SafeMathFunctionsAvailabilityTests.cs
```

### Implementation details

1. Add module alias `SafeMathFunctions`.
2. Add manifest entry.
3. Add descriptor provider for intrinsics.
4. Implement pure static methods or intrinsic handlers:

```csharp
public static double Min(double left, double right)
public static double Max(double left, double right)
public static double Abs(double value)
public static double Clamp(double value, double min, double max)
public static double Round(double value, double digits)
```

5. For `round`, convert `digits` carefully. MVP can require `digits` to be an integer-valued number and document behavior, or postpone `round` if this creates complexity.
6. Prefer adding only functions that can be implemented cleanly for both backends.

### Tests

```text
SafeMath_Min_InterpreterAndCompiler_ReturnSameResult
SafeMath_Max_InterpreterAndCompiler_ReturnSameResult
SafeMath_Abs_InterpreterAndCompiler_ReturnSameResult
SafeMath_Clamp_InterpreterAndCompiler_ReturnSameResult
SafeMath_Round_InterpreterAndCompiler_ReturnSameResult_IfIncluded
SafeMath_Clamp_NotAvailableWithoutModule_ReturnsDiagnostic
SafeMath_ModuleSelection_DoesNotEnableCSharpInterop
```

### Acceptance criteria

```text
- SafeMath functions execute in both backends where enabled.
- Results match between interpreter and compiler.
- Unselected dialects do not expose SafeMath functions.
```

### Explicitly forbidden in PR 8

```text
- No pricing-rules profile yet unless needed as test fixture.
- No RuleSet API yet.
```

---

## PR 9 — Add function call authoring support if missing

### Goal

Ensure user-facing function call syntax works through the Wist pipeline.

### Why after SafeMath runtime support

If existing method-call/intrinsic syntax already supports calls, this PR may be small. If not, this PR adds the minimal frontend path.

### Scope

Add or polish:

```text
function call parsing
function call AST node if missing
function call semantic resolution through IBuiltinFunctionCatalog
function call lowering to intrinsic/method call
function call diagnostics
```

### Syntax

```wist
clamp(price * discount, 0.0, maxDiscount)
```

### Implementation strategy

1. Search existing call/method-call nodes and reuse them if possible.
2. Do not create a duplicate call mechanism if CSharpInterop/method-call infrastructure already exists.
3. Add a Wist-safe function call path that is independent from unsafe C# interop.
4. Function availability must come from selected feature/function descriptors.
5. CSharpInterop must not be required for SafeMath function calls.

### Tests

```text
FunctionCall_ClampExpression_ParsesAndExecutes
FunctionCall_NestedArguments_ParsesAndExecutes
FunctionCall_UnknownFunction_ReturnsDiagnostic
FunctionCall_WrongArity_ReturnsDiagnostic
FunctionCall_WrongType_ReturnsDiagnostic
FunctionCall_DoesNotRequireCSharpInterop
FunctionCall_CompilerAndInterpreterParity
```

### Acceptance criteria

```text
- SafeMath function calls work in source code.
- Diagnostics are high-level and deterministic.
- Function calls do not imply CSharpInterop.
```

### Explicitly forbidden in PR 9

```text
- No general user-defined functions.
- No namespaces.
- No overload complexity beyond exact MVP descriptors.
```

---

## PR 10 — Add IfExpression feature metadata and parser skeleton

### Goal

Introduce IfExpression as a feature and parse it, without necessarily lowering all runtime behavior in this PR if that is too large.

### Why separate skeleton/runtime if needed

IfExpression can touch parser, AST, bytecode/AIR and backends. Split only if necessary, but still sequential.

### Scope

Add:

```text
IfExpression feature descriptor
syntax recognition
AST node
basic parser tests
semantic placeholder diagnostics if execution not implemented yet
```

### Syntax

```wist
if condition then thenExpression else elseExpression
```

### Tests

```text
IfExpression_Parse_SimpleConditional_CreatesIfExpressionNode
IfExpression_Parse_NestedInArithmetic_CreatesExpectedShape
IfExpression_Parse_MissingThen_ReturnsDiagnostic
IfExpression_Parse_MissingElse_ReturnsDiagnostic
```

### Acceptance criteria

```text
- Parser recognizes if-expression.
- Feature metadata reports IfExpression when selected.
- Execution may be deferred to PR 11 if needed.
```

### Explicitly forbidden in PR 10

```text
- No statement-level if.
- No blocks.
- No elif.
- No pattern matching.
```

---

## PR 11 — Implement IfExpression semantics and backend parity

### Goal

Make if-expression executable and type-checked.

### Scope

Add:

```text
semantic validation
condition bool check
then/else type compatibility check
bytecode/AIR lowering
interpreter execution
CIL branch emission
parity tests
negative tests
```

### Semantic rules

```text
condition: bool
then: T
else: T
result: T
```

MVP type compatibility:

```text
number with number -> number
bool with bool -> bool
anything else -> diagnostic
```

### Tests

```text
IfExpression_TrueCondition_ReturnsThenBranch
IfExpression_FalseCondition_ReturnsElseBranch
IfExpression_NestedInArithmetic_ReturnsExpectedResult
IfExpression_ConditionNumber_ReturnsTypeDiagnostic
IfExpression_BranchTypeMismatch_ReturnsTypeDiagnostic
IfExpression_NotSelected_ReturnsFeatureDiagnostic
IfExpression_CompilerAndInterpreterParity
```

### Acceptance criteria

```text
- If-expression works in both backends.
- Type errors are readable.
- Feature is unavailable unless selected.
```

### Explicitly forbidden in PR 11

```text
- No statement-level control flow expansion.
- No multi-statement branches.
```

---

## PR 12 — Add LetBindings feature descriptor and diagnostics polish

### Goal

Formalize let-bindings as a user-facing feature and add diagnostics around local binding behavior.

### Why after SafeMath and IfExpression

Let bindings make examples readable. Existing Variables/Scopes may already support much of this; this PR is about making it stable, feature-described and diagnostic-friendly.

### Scope

Add:

```text
LetBindings feature descriptor
local binding diagnostics
binding shadowing rules
duplicate binding rules
unknown local binding diagnostics
parity tests
```

### MVP syntax

Use current project syntax if it already exists. If not, define:

```wist
let name = expression
```

### Binding rules

Recommended:

```text
- let binding is local to current body/rule.
- later expressions can reference previous let names.
- let cannot shadow rule parameter in MVP.
- duplicate let names are rejected.
```

### Tests

```text
LetBinding_CanReferencePreviousBinding
LetBinding_CanChainBindings
LetBinding_CannotReferenceFutureBinding
LetBinding_CannotShadowParameter
LetBinding_DuplicateLocalName_ReturnsDiagnostic
LetBinding_CompilerAndInterpreterParity
```

### Acceptance criteria

```text
- LetBindings appears in feature reports.
- Readable multi-step formulas work.
- Common binding mistakes produce diagnostics.
```

### Explicitly forbidden in PR 12

```text
- No closures.
- No mutable assignment unless already supported and stable.
- No block scoping redesign.
```

---

## PR 13 — Add rule model contracts

### Goal

Add RuleSet API contracts without parser/runtime implementation yet.

### Why now

Expression layer is ready. Now define high-level rule contracts.

### Scope

Add:

```text
RuleTypeDescriptor
RuleParameterDescriptor
CompiledRuleDescriptor
ICompiledRule
ICompiledRuleSet
RuleSetCompileResult
```

### Suggested files

```text
UniversalToolchain/UniversalToolchain.Rules.Abstractions/RuleTypeDescriptor.cs
UniversalToolchain/UniversalToolchain.Rules.Abstractions/RuleParameterDescriptor.cs
UniversalToolchain/UniversalToolchain.Rules.Abstractions/CompiledRuleDescriptor.cs
UniversalToolchain/UniversalToolchain.Rules.Abstractions/ICompiledRule.cs
UniversalToolchain/UniversalToolchain.Rules.Abstractions/ICompiledRuleSet.cs
UniversalToolchain/UniversalToolchain.Rules.Abstractions/RuleSetCompileResult.cs
```

### Tests

```text
RuleTypeDescriptor_CanRepresentNumber
CompiledRuleDescriptor_CanRepresentRuleSignature
RuleSetCompileResult_CanRepresentSuccess
RuleSetCompileResult_CanRepresentFailure
```

### Acceptance criteria

```text
- Contracts compile.
- No rule parsing yet.
- No runtime behavior changes.
```

### Explicitly forbidden in PR 13

```text
- No facade CompileRuleSet yet.
- No rule syntax yet.
```

---

## PR 14 — Add rule declaration parser/extractor MVP

### Goal

Parse/extract top-level rule declarations into an intermediate model.

### Why before compile API

Need rule syntax model before compilation.

### Scope

Add:

```text
RuleDeclarationSyntaxModel or RuleDeclarationModel
RuleParameterSyntaxModel
RuleSetParser or RuleDeclarationExtractor
basic diagnostics
```

### Syntax

```wist
rule RuleName(param1: number, param2: bool) -> number {
    expression
}
```

### Implementation shortcut

For MVP, rule extraction may be implemented as a focused top-level parser that extracts:

```text
rule name
parameter list
return type
body text
source spans where possible
```

Then each body can be compiled through the existing expression pipeline.

This avoids full multi-function compiler rewrite.

### Important constraints

```text
- Only top-level rule declarations.
- Rule body is expression-oriented.
- No nested rule declarations.
- No user-defined functions.
- No imports.
```

### Diagnostics

```text
WST-RULE-001 Duplicate rule name.
WST-RULE-002 Unknown rule type.
WST-RULE-004 Duplicate rule parameter name.
WST-RULE-005 Rule name must not be empty.
WST-RULE-006 Rule body must contain an expression.
```

### Tests

```text
RuleDeclarationExtractor_ParsesSingleRule
RuleDeclarationExtractor_ParsesMultipleRules
RuleDeclarationExtractor_ParsesParameterTypes
RuleDeclarationExtractor_ParsesReturnType
RuleDeclarationExtractor_DuplicateRuleName_ReturnsDiagnostic
RuleDeclarationExtractor_DuplicateParameterName_ReturnsDiagnostic
RuleDeclarationExtractor_UnknownType_ReturnsDiagnostic
RuleDeclarationExtractor_MissingBody_ReturnsDiagnostic
RuleDeclarationExtractor_RepeatedCalls_AreDeterministic
```

### Acceptance criteria

```text
- Rule declarations can be extracted into a typed model.
- Common syntax/semantic authoring errors return diagnostics.
- No rule execution yet.
```

### Explicitly forbidden in PR 14

```text
- No full parser rewrite.
- No multi-function compiler.
- No rule invocation from Wist source.
```

---

## PR 15 — Implement RuleSet compiler using existing artifact compiler

### Goal

Compile each extracted rule body into an existing compiled artifact.

### Why after rule extractor

Rule declarations can now be parsed. We can compile rule bodies.

### Scope

Add:

```text
WistRuleSetCompiler
CompiledRule
CompiledRuleSet
rule parameter to declared binding mapping
return type validation if possible
compiler/interpreter mode support
```

### Implementation algorithm

```text
1. Extract rule declarations.
2. If extraction diagnostics contain errors, return RuleSetCompileResult failure.
3. For each rule declaration:
   a. Convert rule parameters to OrderedDictionary<string, Type>.
   b. Compile rule body through WistDialectExecutionHost.GetArtifactCompiler<T>().Compile(...).
   c. Store artifact in CompiledRule.
4. Create CompiledRuleSet.
5. Return RuleSetCompileResult success.
```

### Backend handling

Use existing host backend resolution:

```text
mode = compiler -> CIL artifact compiler
mode = interpreter -> AIR/interpreter artifact compiler
```

Do not manually wire modules.

### Return type validation

MVP options:

```text
Option A: validate return type using semantic type info if available.
Option B: validate by executing? Not acceptable as general compile-time validation.
Option C: initially validate only where type metadata is available and otherwise document limitation.
```

Recommended:

```text
Add minimal type inference for rule bodies only if already supported by existing AST/semantics.
If not available, defer strict return type validation to a later PR, but keep descriptor field.
```

However, diagnostics should still catch obvious mismatches when possible.

### Tests

```text
RuleSetCompiler_CompileSingleNumericRule_Succeeds
RuleSetCompiler_CompileSingleBooleanRule_Succeeds
RuleSetCompiler_CompileMultipleRules_Succeeds
RuleSetCompiler_WhenRuleBodyInvalid_ReturnsDiagnostic
RuleSetCompiler_UsesExistingDialectHost_NotManualComposition
RuleSetCompiler_CompilerMode_CreatesExecutableRule
RuleSetCompiler_InterpreterMode_CreatesExecutableRule
```

### Acceptance criteria

```text
- Rule bodies compile through existing pipeline.
- No manual module/backend composition.
- CompileResult exposes success/failure.
```

### Explicitly forbidden in PR 15

```text
- No Wist source-level calls between rules.
- No recursion.
- No shared rule-local global state.
```

---

## PR 16 — Implement RuleSet runtime execution

### Goal

Make compiled rules executable by name with argument dictionaries.

### Scope

Add:

```text
CompiledRule.Run(arguments)
CompiledRuleSet.Run(ruleName, arguments)
argument validation
unknown rule diagnostics/Thrower behavior
missing argument behavior
wrong argument type behavior
```

### Argument validation rules

```text
- Unknown argument name: diagnostic or Thrower.Argument depending API shape.
- Missing required argument: diagnostic or Thrower.Argument.
- Null for non-nullable value type: error.
- Wrong runtime type: error.
```

For MVP, if `Run` throws for invalid runtime invocation, it must use project-approved Thrower style. Compile-time authoring errors should remain diagnostics.

### Tests

```text
CompiledRule_Run_WithValidArguments_ReturnsExpectedResult
CompiledRuleSet_Run_WithRuleName_ReturnsExpectedResult
CompiledRuleSet_TryGetRule_KnownRule_ReturnsTrue
CompiledRuleSet_TryGetRule_UnknownRule_ReturnsFalse
CompiledRule_Run_MissingArgument_FailsPredictably
CompiledRule_Run_WrongArgumentType_FailsPredictably
CompiledRuleSet_Run_UnknownRule_FailsPredictably
CompiledRule_Run_RepeatedCalls_AreStable
```

### Acceptance criteria

```text
- .NET host can run named rules.
- Runtime invocation failures are predictable.
- No direct exception throwing outside project rules.
```

### Explicitly forbidden in PR 16

```text
- No stateful rule sessions beyond existing compiled artifact session model.
- No async execution.
- No dynamic parameter discovery outside rule schema.
```

---

## PR 17 — Add WistRuntimeFacade.CompileRuleSet

### Goal

Expose RuleSet compilation through the convenient Wist facade.

### Why after RuleSet compiler/runtime

Lower-level rule infrastructure works. Now add user-facing API.

### Scope

Add to facade or extension:

```csharp
public RuleSetCompileResult CompileRuleSet(
    string source,
    string mode = "compiler");
```

Possible overload:

```csharp
public RuleSetCompileResult CompileRuleSet(
    WistRuleSetCompileRequest request);
```

Request type:

```csharp
public sealed record WistRuleSetCompileRequest(
    string Source,
    string Mode = "compiler",
    string SourceName = "rules.wist");
```

### Implementation details

1. Use existing facade host.
2. Resolve backend mode through existing configuration.
3. Create `WistRuleSetCompiler` with host/artifact compiler dependencies.
4. Return diagnostics rather than throwing for authoring errors.
5. Thrower only for invalid API usage such as null source/mode.

### Tests

```text
WistRuntimeFacade_CompileRuleSet_ValidRule_ReturnsSuccess
WistRuntimeFacade_CompileRuleSet_InvalidRule_ReturnsDiagnostics
WistRuntimeFacade_CompileRuleSet_CompilerMode_RunsRule
WistRuntimeFacade_CompileRuleSet_InterpreterMode_RunsRule
WistRuntimeFacade_CompileRuleSet_UnknownMode_FailsPredictably
```

### Acceptance criteria

```text
- Main user-facing API exists.
- Demo scenario can be written in C#.
```

### Explicitly forbidden in PR 17

```text
- No new service provider composition inside facade beyond existing pattern.
- No bypassing WistDialectExecutionWorkflow.
```

---

## PR 18 — Add RuleSet schema/introspection

### Goal

Expose schema for compiled rule sets.

### Scope

Add:

```text
RuleSetSchema
RuleSchema
RuleParameterSchema
schema projection from ICompiledRuleSet
optional deterministic JSON formatter
```

### Implementation details

1. Schema comes from compiled rule descriptors.
2. UsedFeatures can initially be conservative:

```text
- include RuleDeclarations and TypedRuleParameters for all rules;
- include SafeMathFunctions/IfExpression/LetBindings if detected or selected.
```

3. Do not make schema extraction execute code.

### Tests

```text
RuleSetSchema_FromSingleRule_ReturnsParametersAndReturnType
RuleSetSchema_FromMultipleRules_ReturnsDeterministicOrder
RuleSetSchemaJsonFormatter_RepeatedCalls_AreDeterministic
RuleSetSchema_DoesNotExecuteRules
```

### Acceptance criteria

```text
- Host can inspect rule inputs/outputs.
- Schema is deterministic.
```

### Explicitly forbidden in PR 18

```text
- No UI generation.
- No external JSON schema standard unless trivial.
```

---

## PR 19 — Add `pricing-rules` shipped dialect profile

### Goal

Create the strongest product demo profile.

### Why after RuleSet API

Now the profile can demonstrate actual high-level usage.

### Scope

Add:

```text
UniversalToolchain/Dialects/examples/wist/pricing-rules/dialect.wistdialect
UniversalToolchain/Dialects/examples/wist/pricing-rules/program.wist
UniversalToolchain/Dialects/examples/wist/pricing-rules/README.md
optional preset id WistShippedDialectPresets.PricingRules
tests
```

### Dialect

```wist
dialect PricingRules
use NativeTypes,Identifier,Variables,Scopes,LetBindings,IfExpression,RuleDeclarations,SafeMathFunctions
backend cil,interpreter
security restricted
```

If actual module aliases differ, use the real aliases from manifests. Do not invent aliases without adding manifest entries.

### Program

```wist
rule FinalPrice(price: number, quantity: number, discount: number, maxDiscount: number) -> number {
    let base = price * quantity
    let discountValue = clamp(base * discount, 0.0, maxDiscount)
    let result = base - discountValue

    if result < 0.0 then 0.0 else result
}
```

### Tests

```text
PricingRules_ComposesSuccessfully
PricingRules_FeatureReport_ContainsRuleDeclarationsIfExpressionSafeMath
PricingRules_RunFinalPrice_Compiler_Returns255
PricingRules_RunFinalPrice_Interpreter_Returns255
PricingRules_DoesNotExposeCSharpInterop
PricingRules_DoesNotExposeLoopsUnlessExplicitlySelected
```

### Acceptance criteria

```text
- PricingRules is runnable from repository root.
- PricingRules demonstrates the MVP.
- README includes exact commands and expected result.
```

### Explicitly forbidden in PR 19

```text
- No special-case code for PricingRules in framework.
- No sandbox claims.
```

---

## PR 20 — Add `validation-rules` shipped dialect profile

### Goal

Show that the framework can build a different DSL surface, not only pricing.

### Scope

Add:

```text
validation-rules dialect
validation-rules program
validation-rules README
minimal ValidationFunctions descriptors/runtime if included
```

Important sequencing decision:

If ValidationFunctions are not implemented yet, use only existing boolean/comparison logic:

```wist
rule CanApplyDiscount(price: number, discount: number, customerLevel: number) -> bool {
    price > 0.0
        and discount >= 0.0
        and discount <= price
        and customerLevel >= 2.0
}
```

Do not block validation profile on a new ValidationFunctions pack unless SafeMath path is already stable.

### Tests

```text
ValidationRules_ComposesSuccessfully
ValidationRules_RunCanApplyDiscount_ReturnsTrueForValidInput
ValidationRules_RunCanApplyDiscount_ReturnsFalseForInvalidInput
ValidationRules_DoesNotExposeCSharpInterop
ValidationRules_FeatureReport_IsDeterministic
```

### Acceptance criteria

```text
- ValidationRules demonstrates boolean rule use case.
- No extra function pack is required unless already implemented.
```

### Explicitly forbidden in PR 20

```text
- No string validation functions yet.
- No regex.
- No custom validation framework.
```

---

## PR 21 — Add `policy-rules` shipped dialect profile

### Goal

Show a decision/policy use case.

### Scope

Add:

```text
policy-rules dialect
policy-rules program
policy-rules README
tests
```

Program:

```wist
rule ShouldManualReview(amount: number, riskScore: number, isNewCustomer: bool) -> bool {
    amount > 10000.0 or riskScore > 0.8 or isNewCustomer
}
```

### Tests

```text
PolicyRules_ComposesSuccessfully
PolicyRules_ShouldManualReview_HighAmount_ReturnsTrue
PolicyRules_ShouldManualReview_LowRiskExistingCustomer_ReturnsFalse
PolicyRules_FeatureReport_ContainsBooleanComparisonFeatures
```

### Acceptance criteria

```text
- Third profile proves reusable framework story.
- No profile-specific framework logic.
```

---

## PR 22 — Add CLI feature report command

### Goal

Expose feature explanation to users from CLI.

### Scope

Add CLI verb:

```text
features --dialect-file <path>
```

Optional:

```text
features --preset <preset-id>
```

But if preset support requires extra complexity, only implement dialect-file first.

### Implementation details

1. Use existing dialect workflow provider.
2. Compose dialect file.
3. Project feature explanation.
4. Format deterministic report.
5. Exit code 0 on success, 1 on composition failure.

### Example

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- features --dialect-file UniversalToolchain/Dialects/examples/wist/pricing-rules/dialect.wistdialect
```

### Tests

```text
WistCli_Features_PricingRules_PrintsSafeMathFunctions
WistCli_Features_MinimalArithmetic_DoesNotPrintCSharpInterop
WistCli_Features_InvalidDialect_ReturnsNonZero
```

### Acceptance criteria

```text
- User can inspect feature surface without running code.
- Command does not create runtime host.
```

### Explicitly forbidden in PR 22

```text
- No runtime execution.
- No backend registrar activation.
```

---

## PR 23 — Add CLI rule schema command

### Goal

Expose rule schema from CLI.

### Scope

Add CLI verb:

```text
rule-schema --dialect-file <path> --file <rules.wist> --mode compiler
```

Output:

```json
{
  "rules": [
    {
      "name": "FinalPrice",
      "parameters": [
        { "name": "price", "type": "number", "isRequired": true }
      ],
      "returnType": "number"
    }
  ]
}
```

### Tests

```text
WistCli_RuleSchema_PricingRules_PrintsFinalPriceSchema
WistCli_RuleSchema_InvalidRules_ReturnsDiagnosticsAndNonZero
WistCli_RuleSchema_RepeatedRuns_OutputDeterministic
```

### Acceptance criteria

```text
- Schema can be inspected without custom host code.
- CLI output is deterministic.
```

---

## PR 24 — Add CLI rule-run command

### Goal

Allow running a named rule from CLI for demos.

### Scope

Add CLI verb:

```text
rule-run --dialect-file <path> --file <rules.wist> --rule FinalPrice --arg price=100 --arg quantity=3 --arg discount=0.15 --arg maxDiscount=50 --mode compiler
```

### Argument parsing MVP

Support only:

```text
number -> double
bool -> true/false
```

Type should be inferred from rule schema.

### Tests

```text
WistCli_RuleRun_PricingRules_Compiler_Returns255
WistCli_RuleRun_PricingRules_Interpreter_Returns255
WistCli_RuleRun_MissingArgument_ReturnsNonZero
WistCli_RuleRun_UnknownRule_ReturnsNonZero
```

### Acceptance criteria

```text
- Demos can be run entirely through CLI.
- Errors are readable.
```

---

## PR 25 — Documentation integration

### Goal

Make the MVP discoverable.

### Scope

Add/update:

```text
docs/features.md
docs/rules.md
docs/feature-authoring.md
docs/product-profiles.md
readme.md
per-profile READMEs
```

### Documentation requirements

Docs must explain:

```text
- what a feature is;
- how feature differs from module;
- feature system is projection, not activation;
- how to inspect feature report;
- how to write rules;
- how to compile/run rules from .NET;
- how to inspect schema;
- how to create product profiles;
- restricted profiles are not sandboxes.
```

### README strong example

Add a section:

```text
Build and run a restricted pricing rules DSL
```

Include:

```text
PricingRules dialect
rule source
C# host usage
feature report
expected result
```

### Tests/checks

```text
Markdown command fences run if CI supports them.
Commands are valid from repository root.
No docs claim hardened sandboxing.
No docs describe feature system as runtime activation source.
```

### Acceptance criteria

```text
- New user can understand the MVP without reading source code.
- README communicates framework value clearly.
```

---

# 6. Final validation path

After every PR:

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Wist.sln -c Release --no-build
```

If a PR changes docs with bash fences, also run the repository's markdown command validation if available.

For feature/runtime PRs, also run targeted tests first:

```bash
dotnet test UniversalToolchain/Wist.sln -c Release --filter "FullyQualifiedName~Features"
dotnet test UniversalToolchain/Wist.sln -c Release --filter "FullyQualifiedName~Functions"
dotnet test UniversalToolchain/Wist.sln -c Release --filter "FullyQualifiedName~Rules"
```

Exact filters may need adjustment to actual test namespaces.

---

# 7. Final milestone definition

The strong MVP is complete when the following works on master:

```text
1. Feature reports can be generated for existing and new dialects.
2. SafeMathFunctions are selectable by dialect.
3. SafeMathFunctions execute on compiler and interpreter backends.
4. IfExpression works on compiler and interpreter backends.
5. LetBindings are documented and have deterministic diagnostics.
6. Rule declarations can be compiled into a RuleSet.
7. RuleSet can run named rules from .NET.
8. RuleSet schema can be inspected.
9. pricing-rules profile demonstrates a strong business DSL scenario.
10. validation-rules profile demonstrates boolean rule scenario.
11. policy-rules profile demonstrates decision/policy scenario.
12. CLI can inspect features.
13. CLI can inspect rule schema.
14. CLI can run a named rule.
15. README explains why this is not just expression evaluation.
```

---

# 8. Non-negotiable guardrails

These guardrails must be preserved throughout the implementation.

```text
Feature system must not activate runtime components.
Feature system must not replace manifests.
Feature system must not branch on shipped preset names.
RuleSet API must not manually compose modules/backends.
RuleSet API must use existing dialect host/artifact compiler path.
Product profiles must be regular dialect files.
Restricted profiles must not claim sandbox security.
Every executable feature must have compiler/interpreter parity tests.
Every restricted surface must have negative tests.
Diagnostics and reports must be deterministic.
New code must follow project style and Thrower rules.
```

---

# 9. Suggested commit/PR naming

```text
Add feature metadata contracts
Add Wist feature catalog
Add dialect feature explanation projection
Add rule diagnostics contracts
Add builtin function descriptor contracts
Add Wist builtin function resolver
Describe SafeMathFunctions feature
Implement SafeMathFunctions runtime support
Add Wist function call authoring support
Add IfExpression parser support
Implement IfExpression execution parity
Polish LetBindings diagnostics
Add rule model contracts
Add rule declaration extraction
Compile rules through existing artifact pipeline
Run compiled rules by name
Expose RuleSet compilation in Wist facade
Add RuleSet schema projection
Add pricing rules dialect profile
Add validation rules dialect profile
Add policy rules dialect profile
Add CLI feature report command
Add CLI rule schema command
Add CLI rule run command
Document feature and rule authoring MVP
```

Each PR title should describe exactly one step.

---

# 10. Final note

This plan intentionally avoids parallel feature branches.

The order is strict because each layer creates the stable contract for the next layer:

```text
feature metadata
→ feature projection
→ diagnostics
→ function descriptors
→ safe functions
→ expression features
→ rule contracts
→ rule compilation
→ rule execution
→ schema
→ product profiles
→ CLI/docs
```

This keeps the project reviewable, prevents hidden architecture drift, and makes every PR valuable even before the full MVP is complete.
