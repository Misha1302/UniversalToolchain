# Context, motivation, goals, and architecture laws

## 3. Current architecture context

UniversalToolchain currently follows the conceptual pipeline:

```text
Source/Text
  -> Lexer/Parser
  -> AST
  -> Bytecode
  -> AIR
  -> AIR processing modules
  -> selected backend compiler
  -> compiled artifact
  -> execution session
```

The important current boundaries are documented in:

- [Bytecode and AIR](../bytecode-and-air.md);
- [Backends and semantic parity](../backends-and-parity.md);
- [Current architecture status](../../CURRENT_ARCHITECTURE_STATUS.md);
- [Architecture rules](../../ARCHITECTURE_RULES.md);
- [Backend contracts](../../reference/backend-contracts.md).

The current implementation already contains several extension points that make an optional backend feasible:

- [`PreparedExecutionBuilder<TCompilationOutput>`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/BasicCore/Core/PreparedExecutionBuilder.cs) converts Bytecode to AIR, applies `IIRProcessingModule` instances, and delegates compilation through `IAbstractIrCompiler<TCompilationOutput>`.
- [`IAbstractIrCompiler<TCompilationOutput>`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/BasicCore/Contracts/IAbstractIrCompiler.cs) is the backend compilation contract.
- [`IDialectBackendRuntimeRegistrar`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/UniversalToolchain.Dialects.Integration/IDialectBackendRuntimeRegistrar.cs) registers a selected backend runtime.
- Runtime manifests and selected runtime plans activate backend components without requiring framework code to enumerate every concrete backend.
- [`ThirdBackendRuntimeComponentContractTests`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/UniversalToolchain.Dialects.Tests/RuntimeLoading/ThirdBackendRuntimeComponentContractTests.cs) already protect the ability to introduce a third backend through runtime component infrastructure.
- [`ICompiledArtifact<TCompilationOutput>`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/BasicCore/Compilation/ICompiledArtifact.cs) and execution sessions separate immutable compilation snapshots from mutable per-execution argument state.
- Runtime backend registrars declare supported intrinsic identifiers and feed the intrinsic capability registry.

These are valuable foundations. They are not yet sufficient by themselves, because serious SSA lowering exposes several current design debts:

- AIR verification is not a complete shared contract.
- Stack-state handling at control-flow joins is not yet a reusable fixed-point analysis.
- Wist convenience APIs still know concrete compiled artifact shapes in places.
- Function descriptors are coupled to supported backend aliases rather than semantic requirements.
- Managed interop terminology and descriptors are C#-oriented even though emitted calls are CLR calls.
- Existing purity metadata is too coarse for aggressive data-flow and memory transformations.
- The current CIL path produces `DynamicMethod`, while Flame's CIL path produces Mono.Cecil method bodies and assemblies.

The integration should repair these boundaries before or while adding the new backend. It must not hide the debt behind a Flame adapter.

## 4. Motivation

### 4.1 Product motivation

UniversalToolchain is positioned between narrow expression evaluators and full language workbenches. Its differentiating value is not parsing alone. It is the ability to compose a restricted language, select runtime capabilities, and execute the same language through controlled runtime paths.

A real third backend strengthens this position in several ways:

- It demonstrates that backend extensibility is real rather than an abstraction designed around only the interpreter and `DynamicMethod` compiler.
- It introduces execution tiers with distinct cost models rather than a binary interpreted/compiled story.
- It makes the framework more credible for larger DSL programs, not only arithmetic expressions.
- It creates a path to persistent managed assemblies and build-time compilation.
- It provides richer compiler diagnostics and optimization explainability.
- It attracts compiler, runtime, and language tooling contributors without turning Wist into the only architectural truth.

The strongest product message is:

> Compose the language once. Execute it at the right compilation depth.

A more technical version is:

> One selected language, one reference semantics, multiple compilation tiers.

### 4.2 User motivation

Different users have different cost constraints.

A user evaluating a short formula once cares about startup and compilation latency. The interpreter or current CIL backend is likely best.

A user compiling a pricing, routing, validation, or workflow program once and invoking it millions of times may accept a higher preparation cost in exchange for deeper optimization.

A user deploying rules into a service fleet may prefer build-time compilation into a versioned assembly instead of runtime `DynamicMethod` generation.

A DSL author needs diagnostics that explain why a backend cannot compile an operation and which semantic contract is missing.

A backend author needs stable AIR, intrinsic, artifact, and lifecycle contracts that do not assume the backend output is a `DynamicMethod`.

A contributor needs inspectable stages and deterministic representations to reason about compiler transformations.

### 4.3 Engineering motivation

Preparing UniversalToolchain for SSA forces useful formalization:

- explicit basic block boundaries;
- complete successor and predecessor graphs;
- typed stack states at every block boundary;
- fixed-point propagation through loops;
- deterministic merge behavior;
- explicit storage and managed call semantics;
- side-effect contracts;
- artifact ownership and unloadability;
- stable compilation reporting.

These improvements benefit the current CIL backend and interpreter even if Flame is later replaced or never shipped.

### 4.4 Why Flame instead of building everything immediately

Implementing SSA, dominance, liveness, value numbering, inlining, scalar replacement, instruction selection, register allocation, exception flow, and CIL emission from scratch is a multi-stage compiler project. Flame already contains substantial managed-code compiler infrastructure.

Using an established IR and pass library can:

- shorten the path to a meaningful prototype;
- expose missing UniversalToolchain contracts earlier;
- avoid creating superficial or incorrect optimizers;
- provide a reference implementation for managed SSA code generation;
- let project effort focus on language composition, semantic contracts, and user experience.

This benefit is conditional on licensing, maintainability, and API suitability. Flame must not be adopted only because it has a desirable feature list.

## 5. Goals

The design goals are:

1. Add an optional optimizing CIL backend without changing the semantics of existing dialects.
2. Preserve interpreter behavior as the reference semantic path.
3. Preserve the current `cil` backend as the low-compilation-latency path.
4. Make backend compatibility predictable before compilation.
5. Convert verified stack AIR into correct SSA across branches and loops.
6. Support statically resolved managed calls to C#, F#, and other CLR languages.
7. Prevent unsafe optimization of side-effecting or nondeterministic operations.
8. Produce deterministic compilation output and diagnostics where the underlying tools permit it.
9. Support collectible runtime artifacts and avoid permanent assembly retention.
10. Provide inspectable pre- and post-optimization representations.
11. Create a path to offline `.dll` and `.pdb` output.
12. Keep Flame and Mono.Cecil types out of public framework contracts.
13. Keep the default UniversalToolchain/Wist package independent of Flame.
14. Make the integration removable or replaceable without discarding generic improvements.
15. Establish measurable graduation criteria based on correctness, compilation cost, and break-even invocation counts.

## 6. Non-goals

The first implementation is not intended to:

- replace AIR with Flame IR;
- replace the current CIL backend;
- make `optimized-cil` the default backend;
- claim that every Wist program runs faster through Flame;
- expose arbitrary .NET reflection to untrusted DSL source;
- make Flame a sandbox;
- support every Flame optimization pass immediately;
- support every F# runtime representation directly;
- compile arbitrary dynamic reflection calls;
- provide transparent cross-process distributed compilation in the first release;
- expose Flame's public API as UniversalToolchain's stable API;
- introduce Wist-specific branches into generic runtime layers;
- introduce backend-specific AIR opcodes when an existing generic semantic form is sufficient;
- restore removed rule-system functionality merely to demonstrate the backend;
- claim LLVM or native-code support merely because Flame contains LLVM-oriented projects.

## 7. Architecture laws for the integration

The following rules are release-blocking invariants.

### 7.1 UniversalToolchain owns semantics

Flame must receive already selected and already validated semantic input. It must not:

- parse Wist source;
- inspect dialect files to decide semantics;
- activate modules;
- resolve product profiles;
- rediscover enabled syntax;
- choose security policy;
- become a second function resolver.

### 7.2 AIR remains the backend boundary

The canonical integration boundary is:

```text
validated AIR -> backend-local lowering
```

not:

```text
AST -> Flame
```

and not:

```text
Wist source -> Flame
```

Direct AST-to-Flame lowering would couple one backend to frontend node shapes, duplicate Bytecode/AIR semantics, and make semantic parity harder to prove.

### 7.3 The backend is selected through runtime composition

Generic framework code must not branch on `optimized-cil`, `flame`, or a concrete Flame registrar type.

Selection must continue to flow through:

```text
dialect definition
  -> compiled dialect slice
  -> build plan
  -> selected runtime plan
  -> runtime configuration
  -> backend registrar
```

### 7.4 The interpreter remains the semantic oracle

The optimizing backend must match interpreter-observable behavior for all shared supported programs. It must not redefine language rules based on optimizer convenience.

### 7.5 Unknown effects are conservative

An operation with insufficient effect metadata must be treated as potentially throwing and side-effecting. Missing metadata is not evidence of purity.

### 7.6 No silent fallback

If a user explicitly requests `optimized-cil`, unsupported input must produce a structured compatibility or compilation failure. Silent fallback to `cil` or `interpreter` would hide performance, semantic, and deployment differences.

### 7.7 Public contracts remain implementation-neutral

Public APIs may expose:

- backend identity;
- compilation profile;
- compatibility diagnostics;
- compilation report;
- artifact identity and provenance;
- callable/session abstractions.

They must not require callers to reference:

- `Flame.Compiler.FlowGraph`;
- `Mono.Cecil.MethodDefinition`;
- Flame pass types;
- Flame value tags;
- internal `AssemblyLoadContext` management classes.


[Back to the design dossier index](index.md)
