# Optimization, artifacts, explainability, and caching

## 21. Optimization profiles

### 21.1 User-facing profiles

Potential profiles:

```text
none
balanced
aggressive
size
```

Only `balanced` should be considered for the initial executable release. More names should not be exposed before distinct, tested pipelines exist.

### 21.2 Initial balanced pipeline

A conservative starting sequence may include:

```text
control-flow simplification
copy propagation
constant propagation
instruction simplification
dead-value elimination
global value numbering
copy propagation
dead-value elimination
```

The exact order must be validated against Flame APIs and UniversalToolchain effect semantics.

### 21.3 Passes deferred initially

Defer until effect, exception, and alias semantics are proven:

- aggressive inlining;
- partial/scalar replacement of user objects;
- broad memory access elimination;
- loop-invariant code motion;
- speculative transforms;
- exception-flow transforms;
- LINQ expansion;
- transformations that duplicate unknown calls;
- tail-recursion elimination for functions whose call semantics are not fully represented.

### 21.4 Pass determinism

Profiles must define an ordered immutable pass list. Service registration order must not change the pipeline.

A compilation report should record:

- profile id;
- pass ids and versions where possible;
- pass order;
- skipped passes and reasons;
- instruction/block counts before and after each optional reporting checkpoint.

## 22. Compiled artifact model

### 22.1 Current mismatch

The current CIL backend produces a `DynamicMethod`. Flame emits `Mono.Cecil.Cil.MethodBody` attached to a method definition and naturally produces an assembly image.

A serious third backend therefore exposes whether the public artifact model is genuinely backend-neutral.

### 22.2 Backend-local payload

A conceptual Flame payload is:

```csharp
internal sealed class FlameCompilationOutput
{
    public ReadOnlyMemory<byte> PeImage { get; }

    public ReadOnlyMemory<byte> PdbImage { get; }

    public CompiledEntryPointDescriptor EntryPoint { get; }

    public CompilationReport Report { get; }

    public CompiledArtifactProvenance Provenance { get; }
}
```

Loading may occur during compilation or lazily when creating an execution session. The choice must be explicit and measured.

### 22.3 Artifact/session separation

The immutable artifact owns:

- source snapshot identity;
- declared binding layout;
- PE/PDB bytes or loaded assembly handle;
- entry point metadata;
- backend/profile identity;
- provenance;
- report;
- lifetime/unload resources.

A session owns:

- mutable external argument values;
- execution-scoped provider state;
- per-run context;
- no shared mutable local variable state with other sessions.

### 22.4 Callable abstraction

Typed Wist convenience APIs should depend on a backend-neutral callable or invoker contract rather than directly on `DynamicMethod`.

A possible direction:

```csharp
public interface ICompiledCallable<out TDelegate> : IDisposable
    where TDelegate : Delegate
{
    TDelegate EntryPoint { get; }

    CompilationReport Report { get; }
}
```

This is only a sketch. The final contract must align with existing `ICompiledArtifactSession`, execution environments, and fast native pointer wrappers.

### 22.5 Lifetime contract

Not every current artifact requires disposal. Introduce an optional lifecycle contract rather than forcing unrelated backends into artificial ownership:

```csharp
public interface ICompiledArtifactLifetime : IDisposable
{
    bool IsUnloadRequested { get; }
}
```

The facade should dispose owned artifacts and sessions predictably.

## 23. Assembly loading and unloadability

### 23.1 Collectible load contexts

Runtime-generated assemblies should be loaded into a collectible `AssemblyLoadContext` when supported.

Tests must prove that the context can be collected after:

- delegates are released;
- sessions are disposed or no longer referenced;
- caches evict the artifact;
- static event handlers and reflection caches do not retain the assembly;
- runtime provider descriptors do not retain backend-generated types unnecessarily.

### 23.2 Dependency resolution

Generated assemblies may reference:

- framework assemblies;
- UniversalToolchain runtime helper assemblies;
- user assemblies containing bound managed functions;
- FSharp.Core for F#-specific signatures;
- other selected dependencies.

Resolution must be deterministic and bounded. The generated artifact should record referenced assembly identities.

### 23.3 Type identity

Loading a dependency into a separate context can produce type identity mismatches. The design must specify which assemblies are shared from the default context and which may be isolated.

The initial strategy should prefer shared host dependency identity and isolate only the generated assembly where practical.

### 23.4 Failure handling

Loading failures should report:

- missing assembly identity;
- requested version/public key token;
- generated artifact identity;
- backend/profile;
- load context strategy;
- suggested deployment correction.

## 24. Persistent offline artifacts

### 24.1 Product opportunity

An offline build mode would let users convert a selected DSL program into a normal managed assembly.

Conceptual future command:

```text
wistc build pricing.wist \
  --dialect pricing-restricted.wistdialect \
  --backend optimized-cil \
  --output PricingRules.dll
```

This is not a current CLI command.

### 24.2 Output set

A build may produce:

```text
PricingRules.dll
PricingRules.pdb
PricingRules.runtime.json
PricingRules.compilation-report.json
```

The runtime manifest should contain:

- source hash;
- dialect plan hash;
- backend id and version;
- optimization profile;
- AIR schema/version;
- external binding ABI;
- managed method binding identities;
- required assembly identities;
- artifact hash;
- toolchain version;
- compatibility constraints.

### 24.3 Stable generated API

A generated assembly can expose a conventional .NET method:

```csharp
public static class GeneratedPricingRules
{
    public static decimal Calculate(decimal price, int customerLevel);
}
```

This allows invocation from C#, F#, Visual Basic, or reflection-based hosting without requiring source parsing at runtime.

The generated public API must be stable only when explicitly requested. Internal runtime artifacts may use a versioned hidden ABI.

## 25. Compilation report and explainability

### 25.1 User value

Optimization should be observable. A user should be able to understand whether the higher compilation cost produced meaningful simplification.

### 25.2 Report contents

```text
backend identity
backend implementation/version
optimization profile
source hash
dialect/build-plan identity
AIR instruction count
CFG block/edge count
SSA instruction count before/after optimization
per-pass summary
unsupported or conservatively lowered operations
managed calls and effect classifications
compilation timings
artifact size
cache result
warnings
provenance
```

### 25.3 Example

```text
Backend: optimized-cil
Profile: balanced

AIR verification:
  instructions: 184
  blocks: 18
  edges: 24

SSA optimization:
  instructions: 129 -> 73
  blocks: 18 -> 11
  constants folded: 14
  dead values removed: 27
  branches simplified: 5
  common expressions eliminated: 8

Timings:
  verify: 0.7 ms
  lower to SSA: 2.4 ms
  optimize: 5.8 ms
  emit CIL: 3.1 ms

Artifact:
  PE size: 18.4 KiB
  cache: miss
```

### 25.4 Report stability

Machine-readable report fields should be versioned. Human-readable formatting can evolve independently.

## 26. Compatibility analysis before compilation

Users should be able to ask whether a program is supported by a backend before paying full compilation cost.

Conceptual API:

```csharp
BackendCompatibilityResult result = runtime.AnalyzeBackendCompatibility(
    source,
    backend: "optimized-cil");
```

The result should distinguish:

- unsupported AIR intrinsic;
- unsupported type;
- unresolved managed member;
- effect metadata too weak for requested profile;
- unsupported control-flow shape;
- unsupported runtime provider receiver;
- licensing/package unavailable;
- backend not present in selected runtime catalog;
- dialect policy forbids a required operation.

Compatibility analysis must consume structured compiler output, not scan raw source text.

## 27. Caching and artifact provenance

### 27.1 Cache key

Source text alone is insufficient. The key should include at least:

- normalized source hash or exact source hash according to source semantics;
- compiled dialect/build-plan identity;
- selected module identities and versions;
- selected optimizer identities and order;
- backend canonical id;
- backend implementation version;
- optimization profile;
- AIR schema/version;
- intrinsic catalog version;
- parameter names, types, kinds, and order;
- managed binding identities;
- effect metadata version;
- runtime helper ABI;
- target framework;
- platform identity when output is platform-sensitive.

### 27.2 Managed binding identity

`MethodInfo` object identity is not a stable persistent cache key. Persistable identity should include:

- assembly name/version/public key token or stronger artifact identity;
- module MVID where appropriate;
- declaring type identity;
- member name;
- generic arguments;
- parameter and return types;
- metadata token only together with module identity;
- optional assembly content hash for strict deployment modes.

### 27.3 Cache ownership

No unbounded global static cache.

The cache should support:

- size limits;
- eviction;
- artifact leases;
- disposal/unload callbacks;
- hit/miss reporting;
- explicit disablement;
- separation between in-memory callable cache and persistent PE cache;
- concurrency-safe single-flight compilation for identical keys.

### 27.4 Stale artifact prevention

A cached artifact must be rejected when any required ABI, binding, dialect, backend, or toolchain identity is incompatible.


[Back to the design dossier index](index.md)
