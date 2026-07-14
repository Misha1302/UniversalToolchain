# Intrinsics, capabilities, managed interop, and effects

## 17. Intrinsic lowering

### 17.1 No arbitrary passthrough

Flame supports named intrinsic prototypes, but its CIL selector recognizes specific intrinsic families. Creating a Flame intrinsic with the same string as an AIR intrinsic does not guarantee executable output.

Every supported AIR intrinsic needs an explicit lowering rule:

```text
AIR intrinsic
  -> Flame standard instruction/prototype
  -> Flame supported intrinsic
  -> statically resolved managed call
  -> structured unsupported diagnostic
```

### 17.2 Initial intrinsic groups

The first executable subset should cover:

- constants;
- arithmetic for proven primitive types;
- comparisons;
- equality;
- Boolean operations;
- conditional and unconditional control flow;
- statically resolved managed calls;
- statically resolved constructors;
- external argument loads through an explicit representation;
- return/result production.

### 17.3 Capability declaration

The backend registrar should report only intrinsics for which the complete path exists:

```text
AIR production
  -> verifier stack rule
  -> Flame lowering
  -> optimization legality
  -> CIL selection/emission
  -> execution parity test
```

A partially implemented intrinsic must not be advertised.

### 17.4 Unknown intrinsic diagnostics

Diagnostics should contain:

- intrinsic id;
- AIR instruction index/block;
- owning feature/module when available;
- selected backend;
- whether the failure is missing capability, missing lowering, unsupported type, or unsupported effect semantics;
- suggested backend or registration change where appropriate.

## 18. Capability model evolution

### 18.1 Current problem

Built-in function descriptors and runtime bindings currently contain `SupportedBackendAliases`. This couples function availability to a list of concrete backend names.

A third backend would require editing existing providers to append `optimized-cil`, even if the function only semantically requires a static managed call.

This is the wrong direction for long-term extensibility.

### 18.2 Semantic requirements

Functions and modules should declare requirements such as:

```text
managed.static-call
managed.instance-call
managed.constructor
primitive.arithmetic
primitive.comparison
decimal.runtime-operators
local.storage
exception.flow
runtime-provider-access
```

Backends declare supported capabilities. Composition checks whether requirements are satisfied.

### 18.3 Migration strategy

A compatible migration can be staged:

1. Introduce semantic requirement descriptors alongside `SupportedBackendAliases`.
2. Prefer requirements when present.
3. Preserve aliases as a legacy narrowing constraint during alpha.
4. Add diagnostics when aliases and requirements conflict.
5. Migrate built-in providers.
6. Remove or deprecate alias coupling only after all shipped providers and tests are migrated.

### 18.4 No central concrete capability switch

Generic code must not contain:

```csharp
if (backend == "optimized-cil")
{
    // special case
}
```

Compatibility should be a data-driven relation between semantic requirements and backend capability descriptors.

## 19. Managed CLR interop

### 19.1 General direction

The current names `call C#`, `call C# ctor`, and `CSharpCallDescriptor` describe the source language of an implementation, not the runtime operation. Generated CIL invokes CLR members regardless of whether they were authored in C#, F#, Visual Basic, or another CLI language.

The target framework concept should be managed CLR interop.

### 19.2 Proposed descriptors

```csharp
public sealed record ManagedCallDescriptor(
    MethodInfo Method,
    ManagedCallReceiver Receiver,
    ManagedEffectDescriptor Effects,
    ManagedBindingIdentity BindingIdentity);
```

```csharp
public enum ManagedCallReceiverKind
{
    Static,
    ExternalArgument,
    ExecutionScopedProvider
}
```

A constructor uses a separate descriptor or a clearly typed member kind rather than string inspection.

### 19.3 Resolution boundary

Member and overload resolution must happen before backend lowering.

Correct flow:

```text
structured source/AST
  -> function/member resolver
  -> access policy
  -> exact MethodInfo/ConstructorInfo
  -> immutable managed descriptor
  -> AIR managed-call semantic operation
  -> Flame CallPrototype/NewObjectPrototype
```

Forbidden flow:

```text
Flame backend
  -> parse a type name string
  -> scan assemblies
  -> choose an overload
  -> decide whether access is allowed
```

### 19.4 C# interop

Supported C#-authored APIs may include:

- static methods;
- instance methods;
- virtual/interface calls;
- constructors;
- property getters/setters represented as methods;
- fields only after explicit field semantics are added;
- closed generic methods;
- delegates;
- `Task<T>` as a managed return type where execution semantics are defined.

### 19.5 F# interop

F# compiles to CLR metadata and is callable through the same managed descriptors.

The recommended public F# integration surface is a .NET-friendly facade containing normal types and methods.

Straightforward:

- static members;
- instance members;
- properties;
- constructors;
- interfaces;
- delegates;
- `Task`/`Task<T>`.

Possible but not a first-release priority:

- curried `FSharpFunc` values;
- `FSharpAsync<T>`;
- discriminated union internals;
- F# lists and options as language-specific runtime representations;
- quotations;
- active patterns;
- inline functions with statically resolved type parameters;
- units-of-measure metadata that is erased at CLR runtime.

Users should be encouraged to expose a stable facade:

```fsharp
[<AbstractClass; Sealed>]
type PricingApi =
    static member CalculateDiscount(price: decimal, level: int) =
        InternalPricing.calculateDiscount price level
```

### 19.6 Execution-scoped providers

Dependency-injected services should not be accessed through a global service locator.

The compiled method may receive a hidden execution environment/context parameter or explicit provider arguments. The artifact session owns per-execution state and binding values.

The exact ABI must be versioned and included in artifact provenance.

## 20. Effect model

### 20.1 Why purity alone is insufficient

Current `FunctionPurity` values are useful for coarse policy, but an optimizing compiler needs finer distinctions.

Two operations can both be non-pure but differ materially:

- reading the current time;
- writing to a database;
- allocating an object;
- reading immutable memory;
- throwing on invalid input;
- mutating only a private local object;
- invoking an unknown external service.

### 20.2 Proposed effect descriptor

```csharp
public sealed record ManagedEffectDescriptor
{
    public bool IsDeterministic { get; init; }

    public bool MayThrow { get; init; }

    public bool Allocates { get; init; }

    public bool ReadsLocalMemory { get; init; }

    public bool WritesLocalMemory { get; init; }

    public bool ReadsExternalState { get; init; }

    public bool WritesExternalState { get; init; }

    public bool HasObservableSideEffects { get; init; }
}
```

A richer future model may include alias sets, exception types, idempotence, and synchronization effects. The first model should remain conservative and understandable.

### 20.3 Conservative default

Unknown managed calls default to:

```text
IsDeterministic: false
MayThrow: true
Allocates: true or unknown
ReadsExternalState: true
WritesExternalState: true
HasObservableSideEffects: true
```

This prevents unsafe elimination, duplication, reordering, or constant evaluation.

### 20.4 Example effects

```text
Math.Abs(int):
  deterministic
  no external state
  no observable side effect
  exact throw behavior declared by binding

DateTime.UtcNow:
  nondeterministic
  reads external state

Random.Next:
  nondeterministic
  reads and writes receiver state

PaymentService.Charge:
  may throw
  writes external state
  observable side effect
```

### 20.5 Optimization legality

The effect model must guard:

- dead-value elimination;
- common subexpression elimination;
- loop-invariant code motion;
- code duplication during inlining;
- instruction scheduling;
- constant folding through managed calls;
- memory access elimination.

A pass must be disabled or restricted when required effect information is unavailable.


[Back to the design dossier index](index.md)
