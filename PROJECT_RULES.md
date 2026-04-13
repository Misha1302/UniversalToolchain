# PROJECT_RULES

This document defines the mandatory coding rules for the project. It replaces undocumented or inconsistent conventions
with explicit standards.

The goal is to keep the codebase deterministic, readable, easy to review, and consistent across modules, translators,
optimizers, executors, and infrastructure code.

---

## 1. Core Principles

1. Prefer consistency over local preference.
2. Follow existing project architecture and extension points instead of adding ad hoc logic.
3. Keep modules focused: one module, one responsibility.
4. All new public behavior must be understandable from names, signatures, and short English documentation.
5. When a rule conflicts with old code, new code must follow this document.
6. During refactoring, align touched code with these rules.

---

## 2. Language and Comments

### 2.1 English-only rule

All comments, XML documentation, exception messages, debug messages, and user-facing technical text must be written in
English.

This includes:

- `//` comments
- `/* ... */` comments
- XML comments (`<summary>`, `<param>`, `<returns>`, etc.)
- assertion messages
- thrown error messages
- README-style inline notes inside code files

### 2.2 Forbidden

Do not write comments in Russian or mix English with Russian inside code files.

### 2.3 Comment quality

Comments must explain one of the following:

- intent
- invariant
- non-obvious limitation
- architectural reason
- correctness constraint

Do not write comments that only restate syntax.

### 2.4 Preferred documentation style

Use XML documentation for public APIs and short `//` comments only where local clarification is really needed.

```csharp
/// <summary>
/// Builds a prepared execution pipeline for the provided compilation input.
/// </summary>
public PreparedExecution<TCompilationOutput> Build(CompilationInput input)
{
    ...
}
```

---

## 3. Naming Conventions

The current codebase already strongly uses PascalCase for types and members such as `BasicCoreImpl`,
`PreparedExecutionBuilder`, `CompilationInput`, `ExecutionEnvironment`, `TryVisit`, and `NormalizeRuntimeInput`, and
underscore-prefixed private fields such as `_prepared`, `_inputNormalizer`, `_intrinsicCompiler`, `_assemblyCache`, and
`_separator`. These conventions must be treated as standard.

### 3.1 Types

Use `PascalCase` for:

- classes
- records
- interfaces
- enums
- delegates
- attributes

Examples:

- `BasicCoreImpl`
- `PreparedExecution`
- `IExecutionEnvironment`
- `ExternalBindingKind`

### 3.2 Methods and properties

Use `PascalCase` for:

- methods
- properties
- events
- local functions

Examples:

- `PrepareToRun`
- `RunPrepared`
- `Build`
- `GetExecutable`
- `TryVisit`

### 3.3 Parameters and local variables

Use `camelCase` for:

- parameters
- local variables
- lambda parameters

Examples:

- `code`
- `parameters`
- `targetBytecode`
- `labelStacks`
- `serviceType`

### 3.4 Private fields

Use `_camelCase` for private instance and static fields.

Examples:

- `_prepared`
- `_preparedExecutionBuilder`
- `_intrinsicTypeRegistry`
- `_methodCache`
- `_separator`

### 3.5 Interface naming

Interfaces must start with `I`.

Examples:

- `ICoreRunnable`
- `IAbstractMethodsTranslator`
- `IIRProcessingModule`

### 3.6 Attribute naming

Attribute types must end with `Attribute`.

Example:

- `AutoRegisterServiceAttribute`

### 3.7 Boolean naming

Boolean variables, fields, and methods should read like conditions.

Preferred:

- `isValid`
- `hasValue`
- `needCasting`
- `initializeWithDefault`
- `TryGetProcessor`

### 3.8 Collection and dictionary naming

Use plural names for collections and maps unless the variable represents one logical lookup object.

Preferred:

- `modules`
- `optimizers`
- `externalSlots`
- `labelStacks`
- `bindings`

### 3.9 Abbreviations

Avoid cryptic abbreviations in new code.

Allowed when already domain-standard in the project:

- `IR`
- `AIR`
- `CIL`
- `IL`
- `AST`

Do not introduce new unclear abbreviations without strong reason.

---

## 4. File and Type Layout

### 4.1 One main type per file

Each file should contain one main public type. Small tightly-coupled helper types may be nested when this improves
locality.

### 4.2 File name = main type name

The file name should match the main type name.

Examples:

- `PreparedExecutionBuilder.cs` → `PreparedExecutionBuilder`
- `Thrower.cs` → `Thrower`

### 4.3 Namespaces

Use file-scoped namespaces.

```csharp
namespace BasicCore.Core;
```

### 4.4 Global usings

Shared and stable imports should be moved to `GlobalUsings.cs` per project. The repository already uses this approach
and even lists broader migration to global usings as a project task.

---

## 5. Formatting and Braces

The codebase consistently places opening braces on a new line for methods, properties, constructors, control blocks, and
switch arms with block bodies, while often keeping single-line guard clauses without braces. This should be formalized
as the project style.

### 5.1 Base brace style

Use Allman style.

```csharp
public void PrepareToRun(CompilationInput input)
{
    _prepared = _preparedExecutionBuilder.Build(input);
}
```

### 5.2 Type declarations with primary constructors

Primary constructors are allowed when they make the code shorter without reducing readability.

```csharp
public sealed class PreparedExecution<TCompilationOutput>(
    string sourceText,
    TCompilationOutput compilationOutput,
    IExecutor<TCompilationOutput> executor,
    IExecutionEnvironment executionEnvironment)
{
    ...
}
```

### 5.3 Single-line statements

A single statement after `if`, `for`, `foreach`, or `while` may omit braces only when all of the following are true:

- the body is exactly one short statement
- there is no nested control flow
- readability is not reduced
- future modification risk is low

Preferred:

```csharp
if (obj == null)
    Thrower.ArgumentNull(nameof(obj));
```

Required braces:

```csharp
if (obj == null)
{
    LogFailure();
    Thrower.ArgumentNull(nameof(obj));
}
```

### 5.4 Guard clauses

Prefer early return / early throw over deep nesting.

```csharp
if (scope.SafeGet(childIndex) == null)
    return false;
```

### 5.5 Line length and wrapping

Break long argument lists and constructor parameter lists vertically.
Align continuation for readability, not for clever compactness.

### 5.6 Expression-bodied members

Allowed only for short trivial members.

Preferred:

```csharp
public object? GetExternalValue(int slot) => _values[slot];
```

Avoid expression-bodied members when the logic is not trivial.

### 5.7 Collection expressions

Collection expressions such as `[]` are allowed when they improve clarity and match target framework support already
used in the project.

---

## 6. Null Handling

The project currently uses both `Thrower`-based null handling and direct framework null guards such as
`ArgumentNullException.ThrowIfNull`. The new standard must be stricter: null validation in project code must go through
`Thrower` only. `Thrower` already provides `ArgumentNull`, `NotNull`, and `NullException`, so the project has a
dedicated mechanism for this rule.

### 6.1 General rule

Do not allow unexpected nulls to flow deeper into the pipeline.
Validate nulls at boundaries and assert invariants internally.

### 6.2 Mandatory API for null checks

Use only:

- `Thrower.ArgumentNull(...)`
- `obj.ArgNotNull(...)` for public API boundary arguments
- `obj.NotNull(...)`
- `Thrower.AssertAlways(...)` for internal invariants
- `Thrower.NullException<T>(...)` only in rare low-level helper scenarios

### 6.3 Forbidden

Do not use:

- `ArgumentNullException.ThrowIfNull(...)`
- direct `throw new ArgumentNullException(...)`
- silent fallback to null when null is invalid
- null-forgiving operator `!` when `.NotNull(...)` or proper validation can express the contract directly

### 6.4 Preferred boundary normalization style

For non-nullable reference arguments in public constructors and public methods, prefer immediate normalization through
`obj.ArgNotNull(...)` over repetitive defensive `if (obj == null) Thrower.ArgumentNull(...)` blocks.

Preferred:

```csharp
public Runner(IExecutor executor)
{
    _executor = executor.ArgNotNull();
}
```

Avoid when unnecessary:

```csharp
public Runner(IExecutor executor)
{
    if (executor == null)
        Thrower.ArgumentNull(nameof(executor));

    _executor = executor;
}
```

Do not apply this blindly to:

- intentionally nullable arguments
- trivial forwarding overloads
- private/internal helpers that already operate under validated invariants
- string arguments that require stronger semantic validation than null-only checks

### 6.5 Boundary validation

Validate public constructor arguments, public method arguments, DI entry points, and reflection inputs.

```csharp
public IntrinsicTypeRegistryBuilder Add(string name, IntrinsicTypeProcessor processor)
{
    if (string.IsNullOrWhiteSpace(name))
        Thrower.Argument(nameof(name), "Intrinsic type rule name must not be empty.");

    processor = processor.ArgNotNull();

    ...
}
```

### 6.6 Internal invariants

Use `obj.NotNull(...)` or `Thrower.AssertAlways` when null would mean an internal bug or broken invariant.

```csharp
Thrower.AssertAlways(_prepared != null, "Prepared execution must be initialized before RunPrepared.");
```

---

## 7. Exception Policy

The codebase already has a centralized `Thrower` service with `InvalidOpEx`, `AssertAlways`, `NotImplementedException`,
`ArgumentNull`, `Argument`, `FileNotFound`, `NotSupported`, and `InvalidCast`. This must be the only allowed way to
throw project exceptions.

### 7.1 Mandatory rule

All exceptions in project code must be thrown via `Thrower`.

### 7.2 Allowed APIs

Use:

- `Thrower.InvalidOpEx(...)`
- `Thrower.InvalidOpEx<T>(...)`
- `Thrower.AssertAlways(...)`
- `Thrower.NotImplementedException(...)`
- `Thrower.ArgumentNull(...)`
- `Thrower.Argument(...)`
- `Thrower.FileNotFound(...)`
- `Thrower.NotSupported<T>(...)`
- `Thrower.InvalidCast<T>(...)`

### 7.3 Forbidden

Do not write:

```csharp
throw new Exception(...);
throw new InvalidOperationException(...);
throw new ArgumentNullException(...);
throw new NotImplementedException(...);
```

### 7.4 Exception messages

Exception messages must:

- be in English
- be short and precise
- describe the violated invariant or invalid input
- avoid noise and implementation trivia

Preferred:

- `"Unknown intrinsic 'load_xyz'."`
- `"Expected return value on stack."`
- `"Argument 'provider' cannot be null."`

Avoid vague messages like:

- `"Error"`
- `"Something went wrong"`
- `"Invalid state"`

---

## 8. Public API Documentation

### 8.1 XML comments

Public types and non-trivial public members should have XML documentation.

### 8.2 Required for

Required XML docs for:

- public services
- extension points
- public options/configuration types
- infrastructure helpers with non-obvious behavior

### 8.3 Not required for

XML comments are optional for tiny obvious DTO properties or trivial one-line methods.

### 8.4 English only

XML documentation must be in English only.

---

## 9. Collections, Immutability, and Data Flow

### 9.1 Prefer read-only abstractions

Public APIs should prefer:

- `IReadOnlyList<T>`
- `IReadOnlyDictionary<TKey, TValue>`
- immutable snapshots where practical

### 9.2 Avoid leaking mutable internals

Do not expose mutable collections directly unless the design explicitly requires mutation.

### 9.3 Input normalization

Normalize user/runtime input once at the boundary, then pass structured models deeper into the system.
This matches the current split around `CompilationInput` and should remain the standard.

---

## 10. Parser and AST Rules

The existing parser rules in the current `PROJECT_RULES.md` remain valid and are retained here with tighter wording: use
`SafeGet`, mark processed nodes, do not mutate ancestors above the parent scope inside `IAstNodeCreator`, and process
`NodeCreators` in priority order.

### 10.1 `IAstNodeCreator`

Allowed:

- change `NodeType`
- move existing nodes within the allowed local scope
- create new nodes
- add tags

Forbidden:

- mutate ancestors above the current parent scope
- hide index changes after removal
- rely on unsafe child access

### 10.2 Child access

Use `SafeGet` when index may be out of range.

### 10.3 Processed nodes

When parser logic consumes or transforms a node in parser flow, mark it with `MarkAsParserHandled()` when required by
the parsing contract.

### 10.4 Visitor order

In AST visitors, process child nodes before the current node unless a specific visitor contract requires otherwise. This
is the current dominant project pattern.

---

## 11. Type Stack and IR Rules

The current project rules around stack order, type inference, generic resolution, backend-dependent intrinsics, and
AIR-level peephole optimizations remain valid.

### 11.1 Stack order

Arguments are pushed left to right and read from the end of the stack.

### 11.2 Type inference

Use actual stack types or explicit literal types. Do not invent implicit conversions without a clearly documented rule.

### 11.3 Generic resolution

Use `GenericTypeResolver` for generic method calls.

### 11.4 Intrinsics

Do not add new intrinsics without:

- backend support analysis
- type stack rule registration
- clear naming
- tests for both compiler and interpreter paths if applicable

---

## 12. Dependency Injection and Reflection

The project already highlights determinism problems around assembly scanning and reflection-based discovery, and
explicitly plans to centralize reflection and redesign DI behavior. These concerns should be reflected in the rules.

### 12.1 DI registration

Prefer explicit registrations for critical infrastructure.
Auto-registration is acceptable for modules and clearly marked extension points.

### 12.2 Reflection

Reflection usage should be centralized in dedicated helpers or registries.
Do not scatter ad hoc reflection logic across unrelated modules.

### 12.3 Determinism

Do not make behavior depend on unstable assembly scan order.
Where ordering matters, define it explicitly.

---

## 13. Using Directives

### 13.1 Order

Use the following order:

1. `System...`
2. third-party namespaces
3. project namespaces

### 13.2 Redundant usings

Do not keep redundant local `using` directives when a stable `GlobalUsings.cs` already exists.

---

## 14. Testing Rules

### 14.1 Every non-trivial rule change needs tests

Add or update tests when changing:

- parsing precedence
- binding behavior
- intrinsic mapping
- optimization passes
- interpreter/compiler semantic parity
- DI composition

### 14.2 Fixes need regression tests

Every bug fix should add a regression test when practical.

### 14.3 Semantic parity

For language behavior, prefer paired tests that verify both interpreter and compiled execution produce the same
observable result.

---

## 15. Forbidden Practices

Forbidden in new code:

- direct exception throwing instead of `Thrower`
- comments not in English
- XML docs not in English
- mixing null-check styles
- hidden mutation of parent/ancestor AST structure outside the allowed local scope
- unsafe indexing when `SafeGet` is required
- new reflection logic outside dedicated infrastructure helpers
- unclear abbreviations in public APIs
- magic strings for protocol-like behavior without shared constants or clear justification
- silent swallowing of exceptions without explicit reason

---

## 16. Preferred Patterns

### 16.1 Good

```csharp
public void RegisterAssembly(Assembly assembly)
{
    if (assembly == null)
        Thrower.ArgumentNull(nameof(assembly));

    lock (_syncLock)
    {
        if (!IsValidAssembly(assembly))
            return;

        _loadedAssemblies.Add(assembly);
        CacheAssembly(assembly);
        LoadDependencies(assembly);
    }
}
```

### 16.2 Bad

```csharp
public void RegisterAssembly(Assembly assembly)
{
    ArgumentNullException.ThrowIfNull(assembly);
    // Комментарий
    throw new InvalidOperationException("bad");
}
```

---

## 17. Migration Notes for Existing Code

This document is stricter than parts of the current codebase. The repository still contains mixed-language comments and
mixed null-check styles, so these rules should be treated as the target standard for cleanup rather than a claim that
all current files already comply. Evidence of mixed English/Russian comments and mixed guard approaches is visible in
the current code snapshot.

Priority cleanup order:

1. Convert all non-English comments and XML docs to English.
2. Replace direct framework null guards with `Thrower`-based guards.
3. Replace direct `throw new ...` with `Thrower` calls.
4. Normalize brace usage in touched files.
5. Move repeated imports into `GlobalUsings.cs` where stable.

---

## 18. Short Checklist for Contributors

Before merging code, verify:

- names follow project casing conventions
- comments and XML docs are English only
- null checks use `Thrower`
- exceptions use `Thrower`
- braces follow Allman style
- public APIs are documented when non-trivial
- parser/visitor/IR rules preserve stack invariants
- new intrinsics have backend and type-processing support
- tests cover non-trivial behavior changes

---

## 19. Source Basis

This document was updated based on the current `PROJECT_RULES.md`, the `Thrower` implementation, the existing core
pipeline classes, and the repository-wide conventions visible in the uploaded project snapshot.
