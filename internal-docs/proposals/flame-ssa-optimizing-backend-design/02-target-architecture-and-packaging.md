# Target architecture, alternatives, identity, and packaging

## 8. Target architecture

The target runtime flow is:

```text
source
  -> selected lexer/parser/modules
  -> AST
  -> Bytecode
  -> AIR
  -> AIR processing modules allowed by selected backend capabilities
  -> shared AIR verifier
  -> backend compiler
       interpreter:
         AIR artifact
       cil:
         DynamicMethod artifact
       optimized-cil:
         verified AIR CFG
           -> stack-to-SSA lowering
           -> Flame FlowGraph
           -> conservative optimization pipeline
           -> Mono.Cecil method/assembly emission
           -> collectible loaded artifact or persisted assembly
  -> backend-neutral execution facade/session
```

The key new shared components are:

```text
UniversalToolchain.Air.Analysis
  AirControlFlowGraphBuilder
  AirStackDataflowAnalyzer
  AirVerifier
  AirVerificationDiagnostic
  AirVerificationResult

UniversalToolchain.ManagedInterop.Abstractions
  ManagedCallDescriptor
  ManagedConstructorDescriptor
  ManagedCallReceiver
  ManagedEffectDescriptor
  ManagedBindingIdentity

UniversalToolchain.Compilation.Contracts
  CompilationProfile
  CompilationReport
  CompiledArtifactIdentity
  CompiledArtifactProvenance
  ICompiledArtifactLifetime
  ICompiledCallable<TDelegate> or equivalent backend-neutral callable contract

UniversalToolchain.Backends.Flame
  FlameBackendDeclaration
  FlameBackendRuntimeRegistrar
  AirToFlameLowerer
  FlameTypeMapper
  FlameIntrinsicLowerer
  FlameManagedCallLowerer
  FlameOptimizationPipeline
  FlameArtifactEmitter
  FlameCompiledArtifact
  FlameExecutor
```

Exact project names may change after implementation discovery. The ownership boundaries should not.

## 9. Alternatives considered

### 9.1 Replace AIR with Flame IR

Rejected.

Advantages:

- fewer intermediate representations;
- immediate access to SSA and analyses.

Disadvantages:

- makes a third-party IR the framework's semantic source of truth;
- couples the interpreter to a compiler-oriented representation;
- forces every backend to depend on Flame concepts;
- risks GPL contamination of core packages;
- removes control over AIR evolution;
- turns UniversalToolchain into a frontend around Flame;
- creates a difficult migration for current optimizers and tests.

### 9.2 Lower AST directly to Flame

Rejected.

This would bypass Bytecode/AIR module semantics and duplicate lowering logic. It also makes the backend Wist- or frontend-node-aware.

### 9.3 Optimize generated CIL by round-tripping through Flame

Possible future experiment, not the preferred initial integration.

The flow would be:

```text
AIR -> current DynamicMethod-like CIL -> persisted assembly -> Flame CIL frontend -> Flame SSA -> optimized CIL
```

Problems:

- `DynamicMethod` is not naturally persisted as an input assembly;
- semantic source locations and intrinsic identities may be lost;
- the backend would optimize already lowered implementation details rather than explicit AIR semantics;
- runtime helper calls might become opaque before useful optimization;
- round-trip cost and code complexity are high.

Direct verified AIR-to-SSA lowering preserves more semantic intent.

### 9.4 Use Flame only as an offline external tool

Potentially useful as a licensing or isolation fallback.

Advantages:

- clearer process boundary;
- crash isolation;
- easier tool version pinning;
- no runtime package dependency in the application process.

Disadvantages:

- serialization protocol required;
- higher compilation latency;
- worker deployment and lifecycle complexity;
- process boundary does not automatically resolve licensing questions;
- managed type and method identity transfer is difficult;
- execution-scoped providers cannot be serialized trivially.

### 9.5 Build a clean-room SSA backend

Strategically viable, but expensive.

The generic AIR verifier, effect model, artifact contracts, and compatibility model proposed here remain useful if this alternative is chosen later.

### 9.6 Integrate MLIR.NET instead

MLIR.NET is better suited to MLIR syntax/model experimentation and future MLIR ecosystem bridges. Flame is currently closer to UniversalToolchain's immediate .NET needs because it already models CLR types, managed calls, SSA CFGs, optimizations, and CIL emission.

The two directions are not mutually exclusive, but they solve different problems.

## 10. Backend identity and selection

### 10.1 Canonical id

Proposed canonical id:

```text
optimized-cil
```

Possible non-canonical alias:

```text
flame
```

The alias should not appear in first-contact APIs unless troubleshooting the implementation.

### 10.2 Preserve existing aliases

The current `compiler` alias must remain mapped to `cil`.

Changing it would alter:

- compilation latency;
- artifact behavior;
- memory behavior;
- assembly loading;
- benchmark comparability;
- user expectations;
- possibly supported intrinsic coverage.

### 10.3 Explicit opt-in

The first release must require explicit selection:

```text
backend optimized-cil
```

or an equivalent programmatic backend selection.

Automatic tiering is a separate feature and requires runtime evidence.

### 10.4 Runtime manifest activation

The backend should be exported through the existing runtime component mechanism. A conceptual declaration is:

```csharp
[DialectBackendAlias("flame")]
[DialectBackendRegistrarType(typeof(FlameDialectBackendRuntimeRegistrar))]
[DialectRuntimeExport("Backend", "optimized-cil")]
internal sealed class FlameBackendDeclaration : DialectBackendDeclaration
{
    public override DialectBackendId BackendId =>
        new("optimized-cil");
}
```

The exact declaration must follow repository conventions and should not require adding a central `KnownBackends.Flame` branch in generic framework code.

## 11. Package and dependency boundaries

The base packages must not depend on Flame.

Expected dependency direction:

```text
UniversalToolchain.Backends.Flame
  -> UniversalToolchain AIR/compiler/runtime contracts
  -> Flame.Compiler
  -> Flame.Clr
  -> Mono.Cecil as required by Flame

UniversalToolchain.Wist
  -/-> Flame

BasicCore
  -/-> Flame

IntermediateRepresentationAbstractions
  -/-> Flame

UniversalToolchain.Dialects.Core
  -/-> Flame
```

The optional package should be separately installable and separately versioned.

Conceptual installation, not a currently supported command:

```text
dotnet add package UniversalToolchain.Backends.Flame
```

The optional package may provide a small registration extension, but activation truth must still come from selected runtime composition.


[Back to the design dossier index](index.md)
