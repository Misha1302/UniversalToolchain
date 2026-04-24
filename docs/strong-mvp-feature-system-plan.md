# План реализации сильного MVP для UniversalToolchain: Feature System + Rule UX Layer

## 0. Краткое резюме

Цель MVP — превратить UniversalToolchain из проекта, который выглядит как “язык Wist + compiler/interpreter”, в понятный и полезный embeddable DSL framework для .NET-приложений.

Сильный MVP должен показать не просто новые операторы языка, а полноценный сценарий:

```text
.NET developer embeds a restricted rule DSL.
DSL designer chooses available capabilities through dialect profiles.
Business rule author writes readable rules.
Runtime compiles/interprets the same rule semantics.
Host application receives diagnostics, schema, and can execute named rules.
```

Главная идея:

```text
Do not rewrite the canonical runtime path.
Do not fix reflection/activation in this stage.
Do not create a second source of truth.
Add a high-level feature/rule authoring layer on top of the existing manifest-backed dialect runtime.
```

Текущий canonical path остаётся основой:

```text
dialect source
→ dialect compilation
→ build plan
→ manifest-backed runtime selection
→ host creation
→ execution
```

Новый MVP добавляет поверх него:

```text
Feature metadata
→ feature explanation
→ function descriptors
→ safe function packs
→ rule declarations
→ typed parameters
→ rule set API
→ diagnostics
→ schema/introspection
→ product profiles
```

Итоговый пользовательский пример должен выглядеть так:

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

Это уже не выглядит как простой evaluator. Это выглядит как restricted rule DSL framework.

---

## 1. Зачем нужен этот MVP

### 1.1. Текущая проблема

UniversalToolchain уже имеет сильную внутреннюю архитектурную идею:

- dialect composition;
- module-based language construction;
- compiler/interpreter execution;
- manifest-backed runtime selection;
- restricted dialect profiles;
- canonical runtime pipeline.

Но внешний пользователь может не увидеть ценность сразу. Если demo сводится к:

```wist
price * 0.9 + fee
```

то возникает естественный вопрос:

```text
Why not NCalc?
Why not Dynamic Expresso?
Why not a small hand-written evaluator?
```

Чтобы ответ стал сильным, нужно показать не просто expression evaluation, а более высокий сценарий:

```text
Build a restricted rule DSL for a concrete domain.
Expose only selected capabilities.
Compile named rules.
Validate input schema.
Get diagnostics.
Run through compiler or interpreter.
Explain available features.
```

### 1.2. Какой профит должен дать MVP

MVP должен дать 5 видов профита.

#### 1.2.1. Product/demo profit

Проект можно будет демонстрировать как framework для DSL, а не как очередной expression evaluator.

До:

```wist
price * 0.9 + fee
```

После:

```wist
rule FinalPrice(price: number, quantity: number, discount: number, maxDiscount: number) -> number {
    let base = price * quantity
    let discountValue = clamp(base * discount, 0.0, maxDiscount)
    let result = base - discountValue

    if result < 0.0 then 0.0 else result
}
```

#### 1.2.2. Framework profit

Фичи становятся selectable capabilities, а не hardcoded Wist behavior.

Например:

```wist
dialect PricingRules
use NativeTypes,Identifier,Variables,IfExpression,SafeMathFunctions,PricingFunctions
backend cil,interpreter
security restricted
```

И другой dialect:

```wist
dialect ValidationRules
use NativeTypes,BooleanConditions,ComparisonConditions,Identifier,Variables,ValidationFunctions
backend interpreter
security restricted
```

Один framework — разные DSL.

#### 1.2.3. Developer UX profit

.NET-разработчик получает не низкоуровневый compiler pipeline, а понятный API:

```csharp
var compileResult = runtime.CompileRuleSet(source);
var result = compileResult.RuleSet.Run("FinalPrice", args);
```

#### 1.2.4. Business author UX profit

Автор правил получает читаемый DSL:

```wist
let base = price * quantity
let discountValue = clamp(base * discount, 0.0, maxDiscount)
if discountValue > 50.0 then 50.0 else discountValue
```

А не набор cryptic expression hacks.

#### 1.2.5. Architecture proof profit

Архитектор или жюри видит:

```text
This project is not only parsing.
This project is not only evaluation.
This project is not only C# interop.
This project builds controlled runtime language surfaces.
```

---

## 2. Для каких пользователей делается MVP

MVP должен покрыть несколько типов пользователей, но главным должен быть один.

### 2.1. Главный пользователь: .NET application developer

Это разработчик, который хочет встроить в приложение configurable business rules.

Типичные задачи:

```text
pricing
validation
routing
policy checks
workflow conditions
risk scoring
```

Ему нужны:

```text
CompileRuleSet
RunRule
input schema
diagnostics
compiled artifact caching
backend selection
restricted dialect profiles
```

Ему не нужны в MVP:

```text
closures
classes
advanced generics
imports
macros
IDE tooling
full language workbench
```

### 2.2. Business rule author / power user

Это пользователь, который пишет правила, но не хочет понимать compiler internals.

Ему нужны:

```text
readable rule declarations
let-bindings
if-expression
safe built-in functions
comments
human-readable diagnostics
```

Пример:

```wist
rule CanApplyDiscount(price: number, discount: number, customerLevel: number) -> bool {
    price > 0.0
        and discount >= 0.0
        and discount <= price
        and customerLevel >= 2.0
}
```

### 2.3. DSL designer / platform engineer

Это пользователь, который собирает конкретный DSL из framework capabilities.

Ему нужны:

```text
feature packs
capability-based dialect profiles
feature explanation
backend support matrix
negative restricted tests
```

Пример:

```text
PricingRules includes SafeMathFunctions and IfExpression.
ValidationRules includes ValidationFunctions but no CSharpInterop.
FormulaSafe includes arithmetic and safe functions but no loops.
```

### 2.4. Module author

Это разработчик, который хочет добавить новый function pack или language feature.

Ему нужны:

```text
feature descriptor contracts
function descriptor contracts
manifest integration pattern
parity test templates
diagnostics conventions
```

### 2.5. Architect / evaluator

Это человек, который оценивает проект против NCalc, Dynamic Expresso, RulesEngine, ANTLR, etc.

Ему нужны доказательства:

```text
multiple DSLs from one framework
restricted runtime surfaces
compiler/interpreter parity
schema/introspection
diagnostics
explainability
```

---

## 3. Основные non-goals

Важно заранее ограничить scope.

### 3.1. Не исправлять reflection/activation в этом этапе

В этом MVP не нужно:

```text
переписывать runtime assembly loading
убирать reflection
делать feature packs полностью lazy-loaded
перерабатывать manifest activation model
менять canonical runtime path
```

MVP должен работать поверх текущего canonical path.

### 3.2. Не делать полноценный язык общего назначения

Не делать сейчас:

```text
user-defined functions
closures
classes
interfaces
arrays/lists
imports/namespaces
modules/packages
macros
pattern matching
full type inference
advanced generics
async language features
```

### 3.3. Не заявлять sandbox

Restricted dialects — это controlled language surface, а не hardened sandbox.

Документация и demo должны писать:

```text
This is a restricted composition profile, not a hardened sandbox guarantee.
```

### 3.4. Не делать Feature System источником runtime activation

Feature System не должен решать, какие runtime components активировать.

Источник истины остаётся:

```text
dialect definition
build plan
runtime manifests
selected runtime plan
```

Feature System только объясняет и улучшает UX.

---

## 4. Главные архитектурные правила

### 4.1. Feature System is projection, not authority

Плохо:

```csharp
if (featureId == "SafeMathFunctions")
{
    services.AddSingleton<SafeMathFunctionsModule>();
}
```

Правильно:

```text
Runtime selection chooses modules through dialect/manifests.
Feature projection explains that selected modules provide SafeMathFunctions.
```

Feature System может:

```text
explain features
list available functions
format feature report
help semantic validation
help diagnostics
help schema generation
```

Feature System не должен:

```text
replace runtime manifests
activate modules directly
branch on shipped preset ids
hide module/backend selection rules
```

### 4.2. No hardcoded profile branching

Плохо:

```csharp
if (dialectName == "PricingRules")
{
    EnableClamp();
}
```

Правильно:

```text
If selected runtime surface contains SafeMathFunctions provider, clamp is available.
```

### 4.3. Convenience layers must stay optional

`WistRuntimeFacade.CompileRuleSet` — удобный слой, но он не должен становиться обязательным framework path.

Низкоуровневый path должен остаться работоспособным:

```text
WistDialectExecutionWorkflow
WistDialectExecutionHost
ICompiledArtifact
ICompiledArtifactSession
```

### 4.4. Every feature must have parity strategy

Каждая non-trivial language feature должна иметь:

```text
interpreter behavior
compiler behavior
parity tests
negative availability tests
semantic diagnostics tests
```

### 4.5. Respect project coding rules

Реализация должна соблюдать:

```text
English-only comments/docs in code
Thrower-only exceptions
Allman braces
PascalCase public types/methods
_camelCase private fields
no direct throw outside Thrower helpers
no hidden mutable static state
no profile-specific framework branching
one main type per file
public API XML docs where useful
```

---

## 5. Сильный MVP: состав

MVP состоит из 7 блоков.

```text
1. Feature Metadata Layer
2. Function Descriptor Layer
3. SafeMathFunctions feature pack
4. IfExpression + LetBindings polish
5. RuleSet API
6. Diagnostics + Schema
7. Product profiles and demos
```

---

## 6. Блок 1: Feature Metadata Layer

### 6.1. Зачем нужен

Feature Metadata Layer отвечает на вопросы:

```text
Какие пользовательские возможности доступны в этом dialect?
Какие syntax forms доступны?
Какие function packs доступны?
Какие функции доступны?
Какие backend'ы поддерживают эти features?
Почему feature недоступна?
```

Без этого DSL designer и architect видят только modules/backends. Но modules — это implementation detail. Пользователь мыслит features:

```text
RuleDeclarations
IfExpression
SafeMathFunctions
ValidationFunctions
CSharpInterop
Loops
NativeNumbers
```

### 6.2. Основные типы

Рекомендуемые namespaces:

```text
UniversalToolchain.Features.Abstractions
UniversalToolchain.Features.Core
UniversalToolchain.Dialects.Wist.Features
```

Типы:

```csharp
public readonly record struct LanguageFeatureId(string Value);

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

public enum LanguageFeatureSymbolKind
{
    SyntaxForm,
    Function,
    Type,
    RuleForm,
    Operator,
    HostBinding
}

public sealed record LanguageFeatureDescriptor(
    LanguageFeatureId FeatureId,
    string DisplayName,
    LanguageFeatureKind Kind,
    IReadOnlyList<string> RequiredRuntimeComponentAliases,
    IReadOnlyList<LanguageFeatureId> RequiredFeatures,
    IReadOnlyList<LanguageFeatureSymbolDescriptor> ProvidedSymbols,
    IReadOnlyList<string> SupportedBackendAliases,
    string ShortDescription);

public sealed record LanguageFeatureSymbolDescriptor(
    string Name,
    LanguageFeatureSymbolKind Kind,
    string Signature,
    string Description);
```

Catalog:

```csharp
public interface ILanguageFeatureCatalog
{
    IReadOnlyList<LanguageFeatureDescriptor> GetFeatures();

    bool TryGetFeature(
        LanguageFeatureId featureId,
        out LanguageFeatureDescriptor? descriptor);
}
```

Projection result:

```csharp
public sealed record DialectFeatureExplanation(
    string DialectName,
    IReadOnlyList<AvailableLanguageFeature> AvailableFeatures,
    IReadOnlyList<UnavailableLanguageFeature> UnavailableFeatures,
    IReadOnlyList<LanguageFeatureSymbolDescriptor> AvailableSymbols,
    IReadOnlyList<DialectFeatureBackendSupport> BackendSupport);
```

Projector:

```csharp
public sealed class DialectFeatureExplanationProjector
{
    public DialectFeatureExplanation Project(
        DialectFrameworkCompositionResult composition,
        ILanguageFeatureCatalog featureCatalog)
    {
        // Implementation must only inspect composition/runtime selection.
        // It must not activate runtime components.
    }
}
```

Formatter:

```csharp
public sealed class DialectFeatureExplanationFormatter
{
    public string FormatDeterministic(DialectFeatureExplanation explanation)
    {
        // Stable ordering by feature id, symbol name, backend id.
    }
}
```

### 6.3. Как определить, feature доступна или нет

MVP-правило:

```text
Feature is available when all RequiredRuntimeComponentAliases are present in selected runtime plan and all RequiredFeatures are available.
```

Например:

```text
SafeMathFunctions requires:
- SafeMathFunctions module
- NativeTypes or Numbers, depending on implementation choice
```

Для MVP лучше не делать сложный boolean expression language для requirements. Можно начать с простого:

```text
All required module aliases must be selected.
All required feature ids must be available.
```

Если нужно OR-requirement, например `NativeTypes OR Numbers`, лучше для MVP сделать 2 отдельных descriptors:

```text
SafeMathFunctions.Native
SafeMathFunctions.Standard
```

или временно выбрать только NativeTypes-based implementation.

### 6.4. Пример feature report

```text
Dialect: PricingRules

Available features:
- ExternalParameters
- NativeNumbers
- LetBindings
- IfExpression
- RuleDeclarations
- SafeMathFunctions

Available symbols:
- syntax: rule Name(param: type) -> type { ... }
- syntax: if condition then expr else expr
- syntax: let name = expr
- function: clamp(number value, number min, number max) -> number
- function: max(number left, number right) -> number
- function: min(number left, number right) -> number

Backends:
- cil: RuleDeclarations, IfExpression, SafeMathFunctions
- interpreter: RuleDeclarations, IfExpression, SafeMathFunctions

Unavailable features:
- CSharpInterop: required runtime component CSharpInterop is not selected
- Loops: required runtime component Loops is not selected
- Labels: required runtime component Labels is not selected
```

### 6.5. Tests

```text
DialectFeatureExplanationProjector_Project_MinimalArithmetic_ReturnsArithmeticFeaturesOnly
DialectFeatureExplanationProjector_Project_PricingRules_ReturnsSafeMathFunctions
DialectFeatureExplanationProjector_Project_WhenRequiredModuleMissing_ReportsUnavailableFeature
DialectFeatureExplanationFormatter_FormatDeterministic_RepeatedCallsReturnSameText
FeatureCatalog_GetFeatures_ReturnsStableOrder
FeatureProjection_DoesNotCreateBackendRuntimeSideEffects
```

---

## 7. Блок 2: Function Descriptor Layer

### 7.1. Зачем нужен

Function Descriptor Layer нужен, чтобы функции были не hardcoded special cases, а normal language capabilities.

Без него добавление функций быстро превратится в:

```text
if name == "clamp"
if name == "round"
if name == "between"
if name == "moneyMax"
```

С ним каждая функция описана структурно:

```text
name
feature id
parameters
return type
purity
backend support
interpreter implementation
CIL lowering
semantic diagnostics
```

### 7.2. Основные типы

Namespaces:

```text
UniversalToolchain.Functions.Abstractions
UniversalToolchain.Functions.Core
UniversalToolchain.Dialects.Wist.Functions
```

Типы:

```csharp
public sealed record BuiltinFunctionDescriptor(
    string Name,
    LanguageFeatureId FeatureId,
    IReadOnlyList<FunctionParameterDescriptor> Parameters,
    FunctionTypeDescriptor ReturnType,
    FunctionPurity Purity,
    IReadOnlyList<string> SupportedBackendAliases);

public sealed record FunctionParameterDescriptor(
    string Name,
    FunctionTypeDescriptor Type);

public sealed record FunctionTypeDescriptor(string Name);

public enum FunctionPurity
{
    Pure,
    ReadsHostState,
    HasSideEffects
}
```

Catalog:

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

Resolution:

```csharp
public sealed record BuiltinFunctionResolution(
    bool IsSuccess,
    BuiltinFunctionDescriptor? Descriptor,
    FunctionTypeDescriptor? ReturnType,
    IReadOnlyList<RuleDiagnostic> Diagnostics);
```

### 7.3. Availability rules

Function доступна, если:

```text
function descriptor exists
function's FeatureId is available in DialectFeatureExplanation
backendAlias is in SupportedBackendAliases
argument count matches
argument types match
```

### 7.4. Required diagnostics

```text
WST-FUNC-001 Unknown function '{name}'.
WST-FUNC-002 Function '{name}' is not available in the current dialect.
WST-FUNC-003 Function '{name}' is not supported by backend '{backend}'.
WST-FUNC-004 Function '{name}' expects {expected} arguments, got {actual}.
WST-FUNC-005 Function '{name}' argument {index} expects type '{expected}', got '{actual}'.
```

### 7.5. How to connect to existing intrinsics

MVP should avoid creating a second execution mechanism.

The descriptor layer should map user-facing functions to existing/new intrinsic descriptors or method call lowering.

Conceptual mapping:

```text
clamp(number, number, number) -> number
→ intrinsic id: safe_math.clamp.number
→ interpreter handler
→ CIL emitter/static method call
```

### 7.6. Tests

```text
BuiltinFunctionCatalog_Resolve_ClampWithValidTypes_ReturnsDescriptor
BuiltinFunctionCatalog_Resolve_UnknownFunction_ReturnsDiagnostic
BuiltinFunctionCatalog_Resolve_FunctionUnavailableInDialect_ReturnsDiagnostic
BuiltinFunctionCatalog_Resolve_UnsupportedBackend_ReturnsDiagnostic
BuiltinFunctionCatalog_Resolve_WrongArgumentCount_ReturnsDiagnostic
BuiltinFunctionCatalog_Resolve_WrongArgumentType_ReturnsDiagnostic
```

---

## 8. Блок 3: SafeMathFunctions Feature Pack

### 8.1. Зачем нужен

SafeMathFunctions — первый feature pack, который сразу полезен в pricing, validation, scoring, normalization.

Он выглядит просто, но его задача — доказать весь extension path:

```text
feature descriptor
function descriptors
dialect selection
semantic validation
interpreter implementation
CIL implementation
parity tests
negative restricted tests
docs/demo
```

### 8.2. MVP functions

Первая версия:

```text
min(number left, number right) -> number
max(number left, number right) -> number
abs(number value) -> number
clamp(number value, number min, number max) -> number
round(number value, number digits) -> number
```

Можно отложить:

```text
floor
ceil
sqrt
pow
log
trigonometry
```

Причина: MVP должен оставаться domain-safe и простым.

### 8.3. Syntax examples

```wist
clamp(price * discount, 0.0, maxDiscount)
max(finalPrice, 0.0)
round(total * taxRate, 2.0)
abs(delta)
```

### 8.4. Implementation components

Possible project:

```text
SafeMathFunctionsModule
```

or if project naming style prefers:

```text
SafeMathModule
```

Module responsibilities:

```text
register function descriptors
provide intrinsic descriptor provider
provide interpreter implementation
provide CIL lowering support
expose manifest entry
```

Do not make this module know about `PricingRules`.

### 8.5. Dialect use

```wist
dialect PricingRules
use NativeTypes,Identifier,Variables,Scopes,SafeMathFunctions
backend cil,interpreter
security restricted
```

### 8.6. Tests

```text
SafeMathFunctions_Clamp_CompilerAndInterpreterReturnSameResult
SafeMathFunctions_MinMax_CompilerAndInterpreterReturnSameResult
SafeMathFunctions_Round_CompilerAndInterpreterReturnSameResult
SafeMathFunctions_Abs_CompilerAndInterpreterReturnSameResult
SafeMathFunctions_WhenNotSelected_ClampReturnsUnavailableFunctionDiagnostic
SafeMathFunctions_WhenWrongArgumentCount_ReturnsDiagnostic
SafeMathFunctions_WhenWrongArgumentType_ReturnsDiagnostic
```

---

## 9. Блок 4: IfExpression

### 9.1. Зачем нужен

Без conditional expressions DSL остаётся calculator-like.

С `if-expression` можно писать business logic:

```wist
if customerLevel >= 3.0 then price * 0.85 else price * 0.95
```

### 9.2. Syntax

MVP syntax:

```wist
if condition then thenExpression else elseExpression
```

Examples:

```wist
if price > 100.0 then price * 0.9 else price
```

```wist
price * (if customerLevel >= 3.0 then 0.85 else 0.95)
```

### 9.3. Semantic rules

```text
condition must be bool
then branch and else branch must have compatible types
result type is common branch type
feature must be selected by dialect
backend must support branching
```

For MVP, avoid complex implicit conversions.

Accepted:

```text
number vs number -> number
bool vs bool -> bool
```

Rejected:

```text
number vs bool
string vs number
```

### 9.4. Lowering model

Conceptual lowering:

```text
evaluate condition
branch false to else label
evaluate then expression
jump end label
else label:
evaluate else expression
end label:
```

### 9.5. Tests

```text
IfExpression_WhenConditionTrue_ReturnsThenBranch
IfExpression_WhenConditionFalse_ReturnsElseBranch
IfExpression_WhenNestedInArithmeticExpression_ReturnsExpectedResult
IfExpression_WhenConditionIsNumber_ReturnsDiagnostic
IfExpression_WhenBranchTypesMismatch_ReturnsDiagnostic
IfExpression_WhenFeatureNotSelected_ReturnsDiagnostic
IfExpression_CompilerAndInterpreterParity
```

---

## 10. Блок 5: LetBindings polish

### 10.1. Зачем нужен

`let` делает правила читаемыми.

Без `let`:

```wist
max(price * quantity - clamp(price * quantity * discount, 0.0, maxDiscount), 0.0)
```

С `let`:

```wist
let base = price * quantity
let discountValue = clamp(base * discount, 0.0, maxDiscount)
let result = base - discountValue
max(result, 0.0)
```

### 10.2. Required behavior

```text
let name = expression
later expressions can reference name
let bindings are local to current rule/body
duplicate local name returns diagnostic
local name cannot silently shadow external parameter unless explicitly allowed
```

MVP recommendation:

```text
Do not allow local let to shadow rule parameters.
Return deterministic diagnostic instead.
```

Diagnostic:

```text
WST-BIND-002 Local binding 'price' conflicts with rule parameter 'price'.
```

### 10.3. Tests

```text
LetBinding_CanUsePreviousBinding
LetBinding_CanChainBindings
LetBinding_CannotUseBindingBeforeDeclaration
LetBinding_CannotShadowRuleParameter
LetBinding_DuplicateLocalName_ReturnsDiagnostic
LetBinding_CompilerAndInterpreterParity
```

---

## 11. Блок 6: RuleSet API

### 11.1. Зачем нужен

RuleSet API — главный высокий уровень для .NET-разработчика.

Без него:

```text
user compiles anonymous expression
host must know parameters externally
no named rule schema
no multiple rules
```

С ним:

```text
compile source with named rules
inspect rule schema
run rule by name
cache compiled rule set
show diagnostics
```

### 11.2. Syntax

MVP syntax:

```wist
rule RuleName(param1: number, param2: bool) -> number {
    expression
}
```

Examples:

```wist
rule FinalPrice(price: number, discount: number, fee: number) -> number {
    price * (1.0 - discount) + fee
}
```

```wist
rule CanApplyDiscount(price: number, discount: number, customerLevel: number) -> bool {
    price > 0.0
        and discount >= 0.0
        and discount <= price
        and customerLevel >= 2.0
}
```

### 11.3. Supported types in MVP

Keep it small:

```text
number -> double
bool -> bool
```

Maybe later:

```text
string
date
money
percent
custom host types
```

### 11.4. API types

Namespace:

```text
UniversalToolchain.Rules.Abstractions
UniversalToolchain.Rules.Core
UniversalToolchain.Dialects.Wist.Rules
```

Types:

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

public interface ICompiledRule
{
    CompiledRuleDescriptor Descriptor { get; }

    object? Run(IReadOnlyDictionary<string, object?> arguments);
}

public sealed record CompiledRuleDescriptor(
    string Name,
    IReadOnlyList<RuleParameterDescriptor> Parameters,
    RuleTypeDescriptor ReturnType);

public sealed record RuleParameterDescriptor(
    string Name,
    RuleTypeDescriptor Type,
    bool IsRequired);

public sealed record RuleTypeDescriptor(string Name, Type RuntimeType);
```

Compile result:

```csharp
public sealed class RuleSetCompileResult
{
    public bool IsSuccess { get; }

    public ICompiledRuleSet? RuleSet { get; }

    public IReadOnlyList<RuleDiagnostic> Diagnostics { get; }
}
```

Facade extension:

```csharp
public RuleSetCompileResult CompileRuleSet(
    string source,
    string mode = "compiler");
```

### 11.5. Implementation shortcut for MVP

Do not build a full multi-function compiler yet.

MVP implementation:

```text
Parse top-level rule declarations.
For each rule:
  extract name
  extract typed parameters
  extract body expression text or AST
  convert parameters to declared bindings
  compile body through existing artifact compiler
  store compiled artifact in CompiledRule
CompiledRuleSet maps rule names to CompiledRule.
```

This allows reusing existing:

```text
WistDialectExecutionHost
IArtifactCompiler<TCompilationOutput>
ICompiledArtifact
ICompiledArtifactSession
```

### 11.6. Diagnostics

```text
WST-RULE-001 Duplicate rule name '{name}'.
WST-RULE-002 Unknown rule type '{type}'.
WST-RULE-003 Rule '{name}' declares return type '{expected}', but body returns '{actual}'.
WST-RULE-004 Rule parameter name '{name}' is duplicated.
WST-RULE-005 Rule name must not be empty.
WST-RULE-006 Rule body must contain an expression.
WST-RULE-007 Rule '{name}' was not found.
```

### 11.7. Tests

```text
CompileRuleSet_OneNumericRule_CanRunByName
CompileRuleSet_OneBooleanRule_CanRunByName
CompileRuleSet_TwoRules_CanRunIndependently
CompileRuleSet_DuplicateRuleName_ReturnsDiagnostic
CompileRuleSet_DuplicateParameterName_ReturnsDiagnostic
CompileRuleSet_UnknownType_ReturnsDiagnostic
CompileRuleSet_ReturnTypeMismatch_ReturnsDiagnostic
CompiledRuleSet_Run_UnknownRuleName_ReturnsDiagnosticOrThrowsThroughThrower
CompiledRuleSet_Run_MissingArgument_ReturnsDiagnosticOrThrowerError
CompiledRuleSet_Run_WrongArgumentType_ReturnsDiagnosticOrThrowerError
CompileRuleSet_CompilerAndInterpreterParity
```

---

## 12. Блок 7: Diagnostics

### 12.1. Зачем нужны

Для embedded DSL diagnostics — не второстепенная фича. Это часть продукта.

Плохой UX:

```text
InvalidOperationException: Stack state invalid.
```

Хороший UX:

```text
pricing.wist:4:27 WST-FUNC-004 Function 'clamp' expects 3 arguments, got 2.
```

### 12.2. Diagnostic model

```csharp
public sealed record RuleDiagnostic(
    string Code,
    RuleDiagnosticSeverity Severity,
    string Message,
    SourceSpan? Span,
    IReadOnlyList<RuleDiagnosticHint> Hints);

public enum RuleDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record SourceSpan(
    string SourceName,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);

public sealed record RuleDiagnosticHint(string Message);
```

### 12.3. Diagnostic formatter

```csharp
public sealed class RuleDiagnosticFormatter
{
    public string Format(IReadOnlyList<RuleDiagnostic> diagnostics)
    {
        // Stable ordering by source location, code, message.
    }
}
```

### 12.4. MVP diagnostic categories

```text
WST-RULE-*    rule declaration errors
WST-FUNC-*    function resolution errors
WST-TYPE-*    type errors
WST-BIND-*    binding/name errors
WST-FEAT-*    unavailable feature errors
WST-BACK-*    backend support errors
```

### 12.5. Tests

```text
RuleDiagnosticFormatter_Format_SingleDiagnosticContainsCodeAndMessage
RuleDiagnosticFormatter_Format_WithSpanContainsLineAndColumn
RuleDiagnosticFormatter_Format_RepeatedCallsAreDeterministic
Diagnostics_FunctionUnavailable_ContainsFeatureHint
Diagnostics_UnknownFunction_SuggestsClosestName
Diagnostics_TypeMismatch_ContainsExpectedAndActualTypes
```

---

## 13. Блок 8: Schema / Introspection

### 13.1. Зачем нужно

Schema даёт сильный practical value:

```text
UI can build forms.
Host can validate inputs.
Docs can be generated.
Rule contracts can be inspected.
Compiled artifacts can be cached by rule signature.
```

### 13.2. Types

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

### 13.3. JSON export helper

Optional but useful:

```csharp
public sealed class RuleSetSchemaJsonFormatter
{
    public string Format(RuleSetSchema schema)
    {
        // Deterministic JSON formatting.
    }
}
```

### 13.4. Example schema

For:

```wist
rule FinalPrice(price: number, quantity: number, discount: number) -> number {
    price * quantity * (1.0 - discount)
}
```

Schema:

```json
{
  "rules": [
    {
      "name": "FinalPrice",
      "parameters": [
        { "name": "price", "type": "number", "isRequired": true },
        { "name": "quantity", "type": "number", "isRequired": true },
        { "name": "discount", "type": "number", "isRequired": true }
      ],
      "returnType": "number",
      "usedFeatures": ["RuleDeclarations", "TypedParameters"]
    }
  ]
}
```

### 13.5. Tests

```text
RuleSetSchema_FromSingleRule_ReturnsExpectedParameters
RuleSetSchema_FromMultipleRules_ReturnsRulesInDeterministicOrder
RuleSetSchemaJsonFormatter_Format_RepeatedCallsAreDeterministic
RuleSetSchema_IncludesUsedFeatures_WhenRuleUsesClampAndIf
```

---

## 14. Product profiles

### 14.1. Зачем нужны

Product profiles — это runnable proof, что UniversalToolchain может собирать разные DSL.

Нужно добавить shipped dialect examples:

```text
pricing-rules
validation-rules
policy-rules
formula-safe
```

### 14.2. pricing-rules

Dialect:

```wist
dialect PricingRules
use NativeTypes,Identifier,Variables,Scopes,LetBindings,IfExpression,RuleDeclarations,SafeMathFunctions
backend cil,interpreter
security restricted
```

Program:

```wist
rule FinalPrice(price: number, quantity: number, discount: number, maxDiscount: number) -> number {
    let base = price * quantity
    let discountValue = clamp(base * discount, 0.0, maxDiscount)
    let result = base - discountValue

    if result < 0.0 then 0.0 else result
}
```

Expected result with:

```text
price = 100.0
quantity = 3.0
discount = 0.15
maxDiscount = 50.0
```

```text
base = 300.0
discountValue = 45.0
result = 255.0
```

### 14.3. validation-rules

Dialect:

```wist
dialect ValidationRules
use NativeTypes,BooleanConditions,ComparisonConditions,Identifier,Variables,Scopes,RuleDeclarations,ValidationFunctions
backend interpreter
security restricted
```

Program:

```wist
rule CanApplyDiscount(price: number, discount: number, customerLevel: number) -> bool {
    positive(price)
        and between(discount, 0.0, price)
        and customerLevel >= 2.0
}
```

### 14.4. policy-rules

Dialect:

```wist
dialect PolicyRules
use NativeTypes,BooleanConditions,ComparisonConditions,Identifier,Variables,Scopes,IfExpression,RuleDeclarations,SafeMathFunctions
backend cil,interpreter
security restricted
```

Program:

```wist
rule ShouldManualReview(amount: number, riskScore: number, isNewCustomer: bool) -> bool {
    amount > 10000.0 or riskScore > 0.8 or isNewCustomer
}
```

### 14.5. formula-safe

Dialect:

```wist
dialect FormulaSafe
use NativeTypes,Identifier,Variables,Scopes,SafeMathFunctions
backend cil,interpreter
security restricted
```

Use case:

```text
Simple formulas without rules, loops, labels, or CSharpInterop.
```

### 14.6. Per-profile README requirements

Each profile README must include:

```text
what this profile demonstrates
enabled modules/features/backends
intentionally excluded capabilities
exact CLI commands from repository root
expected behavior/result
security note: restricted composition is not hardened sandboxing
```

---

## 15. CLI additions

### 15.1. New CLI verbs/options

Possible additions:

```text
wistc features --dialect-file <path>
wistc rule-run --dialect-file <path> --file <rules.wist> --rule FinalPrice --arg price=100 --arg discount=0.15
wistc rule-schema --dialect-file <path> --file <rules.wist>
```

Keep minimal for MVP.

Priority:

```text
1. dialect features/explain
2. rule-schema
3. rule-run
```

### 15.2. Example

```bash
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- features --dialect-file UniversalToolchain/Dialects/examples/wist/pricing-rules/dialect.wistdialect
```

Output:

```text
Dialect: PricingRules
Features:
- RuleDeclarations
- TypedParameters
- LetBindings
- IfExpression
- SafeMathFunctions

Functions:
- clamp(number, number, number) -> number
- max(number, number) -> number
- min(number, number) -> number
```

---

## 16. Documentation additions

### 16.1. New docs

Add:

```text
docs/features.md
docs/rules.md
docs/feature-authoring.md
docs/rule-diagnostics.md
docs/product-profiles.md
```

### 16.2. docs/features.md

Should explain:

```text
what a feature is
how feature differs from module
feature system is projection, not source of truth
how features are shown for selected dialect
how function packs map to features
```

### 16.3. docs/rules.md

Should explain:

```text
rule declaration syntax
typed parameters
return types
let
if
safe functions
RuleSet API
schema
examples
```

### 16.4. docs/feature-authoring.md

Should explain:

```text
how to add feature descriptor
how to add function descriptors
how to connect module manifest
how to add tests
how to avoid hidden source of truth
```

### 16.5. README update

README should get a new strong example:

```text
Build and run a restricted pricing rules DSL
```

Not just:

```text
Run expression `(2 + 2) * 3`
```

---

## 17. Test strategy

### 17.1. Test categories

Add tests in appropriate existing/new projects:

```text
Feature metadata tests
Function resolution tests
SafeMathFunctions tests
IfExpression tests
LetBinding tests
RuleSet API tests
Schema tests
Diagnostics tests
Product profile smoke tests
Architecture guardrail tests
```

### 17.2. Architecture guardrail tests

Important tests:

```text
FeatureProjection_DoesNotCreateHostOrBackendRuntime
FeatureSystem_DoesNotActivateRuntimeComponents
FeatureCatalog_DoesNotBranchOnDialectName
RuleSetApi_UsesExistingDialectHostAndArtifactCompiler
ProductProfiles_DoNotUseCSharpInteropUnlessExplicitlySelected
PricingRules_DoesNotExposeLoopsOrLabels
ValidationRules_DoesNotExposeCSharpInterop
FeatureReports_AreDeterministicAcrossRepeatedCalls
```

### 17.3. Parity tests

For each executable feature:

```text
Compiler result == Interpreter result
```

Examples:

```text
SafeMathFunctions_Clamp_CompilerAndInterpreterParity
IfExpression_CompilerAndInterpreterParity
LetBindings_CompilerAndInterpreterParity
PricingRules_FinalPrice_CompilerAndInterpreterParity
PolicyRules_ShouldManualReview_CompilerAndInterpreterParity
```

### 17.4. Negative restricted tests

```text
MinimalArithmetic_Clamp_ReturnsUnavailableFunctionDiagnostic
PricingRules_CSharpInterop_IsUnavailable
ValidationRules_Loops_AreUnavailable
FormulaSafe_RuleDeclarations_AreUnavailableIfNotSelected
```

### 17.5. Diagnostics determinism tests

```text
RuleDiagnostics_RepeatedCompilation_ReturnsSameDiagnostics
FeatureExplanation_RepeatedProjection_ReturnsSameText
SchemaFormatter_RepeatedCalls_ReturnsSameJson
```

---

## 18. Proposed PR sequence

### PR 1: Feature Metadata Core

Scope:

```text
LanguageFeatureId
LanguageFeatureDescriptor
LanguageFeatureSymbolDescriptor
ILanguageFeatureCatalog
DialectFeatureExplanation
DialectFeatureExplanationProjector
DialectFeatureExplanationFormatter
basic Wist feature descriptors for existing modules
unit tests
small docs/features.md
```

No language behavior changes.

Acceptance criteria:

```text
Can print deterministic feature report for existing dialect profiles.
Does not activate runtime components.
Does not change execution behavior.
```

---

### PR 2: Function Descriptor Core

Scope:

```text
BuiltinFunctionDescriptor
FunctionParameterDescriptor
FunctionTypeDescriptor
IBuiltinFunctionCatalog
BuiltinFunctionResolution
function diagnostics
basic resolver tests
```

No or minimal language behavior changes.

Acceptance criteria:

```text
Can resolve known descriptors.
Can report unknown/unavailable/wrong-arity/wrong-type errors.
Catalog order deterministic.
```

---

### PR 3: SafeMathFunctions

Scope:

```text
SafeMathFunctions feature descriptor
SafeMathFunctions module
min/max/abs/clamp/round
interpreter support
CIL support
manifest entry
profile integration
parity tests
negative availability tests
```

Acceptance criteria:

```text
PricingRules can use clamp.
MinimalArithmetic without SafeMathFunctions cannot use clamp.
Compiler/interpreter parity passes.
Diagnostics are readable.
```

---

### PR 4: IfExpression

Scope:

```text
IfExpression feature descriptor
syntax support
semantic validation
lowering
interpreter/CIL support
parity tests
negative tests
```

Acceptance criteria:

```text
if condition then expr else expr works in pricing/policy profiles.
Non-bool condition returns diagnostic.
Branch type mismatch returns diagnostic.
Feature unavailable when not selected.
```

---

### PR 5: LetBindings polish

Scope:

```text
LetBindings feature descriptor
rule/body-local binding behavior
shadowing diagnostics
duplicate binding diagnostics
parity tests
```

Acceptance criteria:

```text
Readable multi-step formulas work.
Shadowing rule parameters is rejected or explicitly handled.
Compiler/interpreter parity passes.
```

---

### PR 6: RuleSet API MVP

Scope:

```text
Rule declaration parser/extractor
Typed parameter model
RuleSetCompileResult
ICompiledRuleSet
ICompiledRule
RunRule
RuleSetSchema
Rule diagnostics
facade CompileRuleSet
unit/integration tests
```

Acceptance criteria:

```text
Can compile one rule.
Can compile multiple rules.
Can run rule by name.
Can inspect schema.
Can return diagnostics instead of raw exceptions for common authoring errors.
Compiler/interpreter parity works for rule bodies.
```

---

### PR 7: Product Profiles + Docs

Scope:

```text
pricing-rules profile
validation-rules profile
policy-rules profile
formula-safe profile
per-profile README
README strong example
docs/rules.md
docs/product-profiles.md
CLI feature report if feasible
```

Acceptance criteria:

```text
A new user can run pricing-rules example from repository root.
A new user can see feature report.
Docs explain why this is not just expression evaluation.
Restricted profiles explicitly avoid sandbox claims.
```

---

## 19. MVP acceptance criteria

The MVP is strong enough when all statements below are true.

### 19.1. Functional criteria

```text
Can define a named rule with typed parameters.
Can use let bindings in rule body.
Can use if-expression in rule body.
Can use SafeMathFunctions in selected dialect.
Can compile rule set through facade.
Can run rule by name from .NET.
Can get rule schema.
Can get readable diagnostics.
Can run at least one pricing rule on compiler and interpreter with same result.
Can run at least one validation/policy rule.
```

### 19.2. Architecture criteria

```text
Feature system does not activate modules.
Runtime activation remains manifest-backed canonical path.
No profile-specific branching in framework-level logic.
Convenience APIs remain optional.
Restricted profiles are implemented through dialect/module selection.
No direct throw in new project code outside Thrower-approved helpers.
No mutable static registries.
Ordering is deterministic.
```

### 19.3. Demo criteria

There must be one impressive demo:

```wist
rule FinalPrice(price: number, quantity: number, discount: number, maxDiscount: number) -> number {
    let base = price * quantity
    let discountValue = clamp(base * discount, 0.0, maxDiscount)
    let result = base - discountValue

    if result < 0.0 then 0.0 else result
}
```

And one .NET usage example:

```csharp
var compileResult = runtime.CompileRuleSet(source);
var result = compileResult.RuleSet.Run("FinalPrice", args);
```

And one feature report:

```text
PricingRules exposes:
- RuleDeclarations
- TypedParameters
- LetBindings
- IfExpression
- SafeMathFunctions

Disabled:
- CSharpInterop
- Loops
- Labels
```

---

## 20. Risks and mitigations

### Risk 1: Overengineering

Problem:

```text
Too many descriptors and abstractions before any visible feature works.
```

Mitigation:

```text
Every abstraction must be used by at least one real feature and one test.
Start with SafeMathFunctions and PricingRules demo.
Avoid FeatureGraph/CapabilityUniverse/global semantic model in MVP.
```

### Risk 2: Feature system becomes source of truth

Problem:

```text
Feature descriptors start deciding runtime activation.
```

Mitigation:

```text
Add guardrail tests.
Keep activation in dialect/build plan/manifests/selected runtime plan.
Document feature system as projection/explanation layer.
```

### Risk 3: RuleSet API bypasses canonical runtime path

Problem:

```text
RuleSet compiler manually wires modules/backends.
```

Mitigation:

```text
RuleSet API must use WistDialectExecutionHost and existing artifact compilers.
No manual module composition.
```

### Risk 4: Diagnostics require too much parser rewrite

Problem:

```text
Source spans may be hard to add everywhere.
```

Mitigation:

```text
MVP diagnostics may have nullable SourceSpan.
Add source spans incrementally for rule declarations and function calls first.
Do not block RuleSet API on perfect spans.
```

### Risk 5: IfExpression touches too much compiler code

Problem:

```text
Branching may require bytecode/AIR/compiler changes.
```

Mitigation:

```text
Keep if-expression expression-only.
No blocks, no elif, no statements.
Add narrow lowering path and parity tests.
```

### Risk 6: Rule declarations require full multi-function compiler

Problem:

```text
Trying to compile whole rule set as one program may explode scope.
```

Mitigation:

```text
MVP compiles each rule body as separate existing artifact.
RuleSet is a high-level container over artifacts.
```

---

## 21. What not to do in MVP

Do not add:

```text
closures
recursive functions
user-defined functions
imports
packages
namespaces
classes
arrays
lists
dictionaries
string functions unless string type is already solid
real sandbox claims
web UI
IDE tooling
language server
macro system
```

Do not rewrite:

```text
runtime reflection
manifest activation
BasicCore orchestration
full parser architecture
full type system
```

Do not introduce:

```text
static mutable global registries
profile-specific branches
hardcoded module sets in framework logic
parallel execution pipeline
manual backend wiring in RuleSet API
```

---

## 22. Suggested internal terminology

Use consistent names.

```text
Feature
FeatureDescriptor
FeatureCatalog
FeatureExplanation
FeatureProjection
FunctionDescriptor
FunctionCatalog
RuleSet
CompiledRuleSet
CompiledRule
RuleSchema
RuleDiagnostic
ProductProfile
```

Avoid vague names:

```text
FeatureManager
RuleHelper
MagicRegistry
ProfileService
UniversalFeatureThing
```

---

## 23. How this MVP should be presented

### 23.1. One-sentence pitch

```text
UniversalToolchain can build restricted, embeddable .NET rule DSLs with selectable language features, compiler/interpreter execution, diagnostics, and schema-aware host integration.
```

### 23.2. Short demo pitch

```text
Here is a PricingRules dialect.
It enables rule declarations, typed parameters, let-bindings, if-expressions, and safe math functions.
It disables CSharp interop, loops, and labels.
The same rule runs through compiler and interpreter.
The host can inspect schema and execute named rules.
```

### 23.3. Comparison pitch

```text
NCalc evaluates expressions.
RulesEngine runs JSON-defined rules.
ANTLR helps build parsers.
UniversalToolchain builds controlled DSL runtime surfaces that can be composed, inspected, restricted, compiled, and interpreted inside .NET applications.
```

---

## 24. Final recommended MVP scope

The strongest realistic MVP is:

```text
Feature Metadata Layer
Function Descriptor Layer
SafeMathFunctions
IfExpression
LetBindings polish
RuleSet API
Diagnostics
Schema
PricingRules profile
ValidationRules profile
PolicyRules profile
README/docs/demo
```

The absolute minimal version, if deadlines become brutal:

```text
SafeMathFunctions
IfExpression
RuleSet API with typed parameters
PricingRules demo
Diagnostics for common errors
Compiler/interpreter parity tests
```

If even that is too much:

```text
Feature report + SafeMathFunctions + PricingRules demo
```

But the recommended target should remain the full strong MVP, because it gives maximum architectural and product profit without requiring low-level runtime rewrite.

---

## 25. Final conclusion

This MVP should not be framed as “adding functions to Wist”.

It should be framed as:

```text
Adding a high-level feature and rule authoring layer over the existing UniversalToolchain runtime.
```

That layer gives:

```text
.NET app developers: CompileRuleSet, RunRule, schema, diagnostics.
Business rule authors: readable rule syntax, let, if, safe functions.
DSL designers: feature packs, restricted profiles, feature reports.
Module authors: descriptor-based extension model.
Architects: proof that the project is a real DSL framework, not just an evaluator.
```

The project rules are preserved because:

```text
runtime activation remains manifest-backed
feature system is projection/explanation, not authority
no hardcoded shipped profile branching
convenience APIs remain optional
new behavior is covered by parity and negative tests
restricted profiles do not claim sandbox security
```

This is the best next stage if the goal is to make UniversalToolchain useful, impressive, and understandable without spending the deadline on low-level architectural cleanup.
