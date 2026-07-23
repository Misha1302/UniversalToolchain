# Rollout, implementation plan, acceptance, and success criteria

## 35. Rollout strategy

### 35.1 Observe before recommending

Stages:

1. IR inspection only.
2. Internal experimental execution.
3. Explicit opt-in alpha backend.
4. Offline artifact inspection.
5. Performance and lifecycle evidence.
6. Documented supported subset.
7. Stable optional backend package.

### 35.2 No behavior change for existing users

Existing users should retain:

- the same default backend;
- the same `compiler` alias;
- the same interpreter semantics;
- the same existing package dependency graph;
- the same source and dialect files;
- the same failure behavior unless a shared verifier intentionally improves diagnostics before backend compilation.

### 35.3 Kill switch

Hosts should be able to omit or disable the backend package/manifest entry. A failing Flame backend must not prevent unrelated interpreter/CIL runtimes from being constructed when it is not selected.

### 35.4 Versioning

The backend should have:

- backend implementation version;
- generated artifact ABI version;
- report schema version;
- cache key schema version;
- AIR compatibility range;
- supported Flame revision range.

## 36. Implementation phases

### Phase 0: licensing and ADR acceptance

Deliverables:

- accepted architecture decision;
- documented licensing path;
- canonical backend id;
- non-goals and dependency boundaries;
- selected Flame projects and revision for prototype.

Exit criteria:

- no ambiguity about whether the prototype may be distributed;
- no Flame dependency added to core projects.

### Phase 1: shared AIR CFG and verifier

Deliverables:

- deterministic CFG builder;
- fixed-point typed stack analysis;
- structured verification diagnostics;
- tests for branches, joins, loops, and invalid AIR;
- initial integration with current backend validation where practical.

Exit criteria:

- all known valid current test programs verify;
- malformed merge states fail deterministically;
- analysis is thread-safe and compilation-scoped;
- no Flame dependency exists yet.

### Phase 2: semantic capabilities and effects

Deliverables:

- semantic backend requirement descriptors;
- migration path from `SupportedBackendAliases`;
- managed effect descriptor;
- conservative defaults;
- compatibility analysis contracts;
- tests for capability matching and effect safety.

Exit criteria:

- a hypothetical third backend can support existing built-ins without editing every provider alias list;
- unknown calls cannot be optimized as pure.

### Phase 3: CLR-neutral managed interop

Deliverables:

- managed call/constructor descriptors;
- compatibility adapters for existing C#-named contracts;
- exact pre-backend member resolution;
- C# and F# facade integration tests;
- security allowlist enforcement before lowering.

Exit criteria:

- backend lowering receives exact members and effects;
- no backend assembly scan or overload resolution is needed.

### Phase 4: AIR-to-Flame inspection prototype

Supported subset:

- constants;
- primitive arithmetic;
- comparisons and Booleans;
- labels and branches;
- block parameters;
- external arguments;
- basic managed calls.

Deliverables:

- Flame type mapper;
- stack-to-SSA lowerer;
- deterministic SSA formatter/snapshots;
- no executable public backend yet.

Exit criteria:

- nested conditions and loops lower correctly;
- golden tests are deterministic;
- unsupported constructs produce structured diagnostics.

### Phase 5: conservative executable backend

Deliverables:

- backend declaration and registrar;
- balanced pass pipeline;
- CIL assembly emitter;
- execution adapter;
- explicit `optimized-cil` selection;
- three-way parity tests.

Exit criteria:

- supported subset matches interpreter and current CIL semantics;
- no silent fallback;
- backend package remains optional;
- existing defaults remain unchanged.

### Phase 6: artifact lifetime and caching

Deliverables:

- collectible load context strategy;
- backend-neutral lifetime contract;
- bounded artifact cache;
- provenance and cache key schema;
- unload and concurrency tests.

Exit criteria:

- generated contexts are collectible;
- cache eviction releases artifacts;
- no unbounded global state exists.

### Phase 7: compilation reports and inspection UX

Deliverables:

- machine-readable report;
- CLI/programmatic inspection surfaces;
- pass summaries;
- compatibility explanation;
- documentation for choosing backends.

Exit criteria:

- users can understand backend support, cost, and transformation results without reading internal logs.

### Phase 8: offline assembly build

Deliverables:

- versioned assembly output;
- PDB/source mapping where feasible;
- runtime/provenance manifest;
- fresh-process loading tests;
- deployment documentation.

Exit criteria:

- a generated assembly executes in a clean host with declared dependencies;
- stale or incompatible artifacts are rejected.

### Phase 9: performance graduation

Deliverables:

- benchmark suite;
- break-even analysis;
- memory and artifact-size evidence;
- supported workload guidance;
- stable alpha/stable decision.

Exit criteria:

- public claims are supported by reproducible data;
- backend recommendations identify workload assumptions.

## 37. Acceptance criteria for the first useful release

The first user-meaningful alpha should satisfy all of the following:

1. Installation is optional and does not change default Wist dependencies.
2. `compiler` still resolves to `cil`.
3. `optimized-cil` is selected explicitly through runtime manifests/plans.
4. Supported programs pass interpreter/CIL/optimized-CIL parity tests.
5. Side-effecting managed calls are neither removed, duplicated, nor reordered illegally.
6. C# static/instance calls and one documented F# facade scenario work.
7. Unsupported constructs fail before executing invalid generated code.
8. AIR joins and loops are verified by a shared fixed-point analysis.
9. Compilation reports expose timing and transformation summaries.
10. Generated runtime assemblies are unloadable or the limitation is explicit and bounded.
11. No core project references Flame.
12. No generic framework layer branches on a concrete Flame/backend name.
13. Licensing permits the chosen distribution model.
14. Documentation clearly labels the backend as optional and describes when not to use it.
15. Benchmarks report compilation cost and break-even counts, not only steady-state speed.

## 38. Risk register

| Risk | Impact | Mitigation |
|---|---|---|
| GPL incompatibility | Cannot distribute integration as planned | Resolve dual license before release; keep prototype isolated |
| Incorrect stack-to-SSA joins | Miscompilation | Shared verifier, fixed-point analysis, golden and parity tests |
| Unknown call effects optimized unsafely | Data loss or duplicated side effects | Conservative effect defaults; disable aggressive passes |
| Artifact unload leaks | Long-running host memory growth | Collectible ALC tests, bounded caches, no global delegate retention |
| Compilation cost exceeds benefit | Poor user experience | Explicit backend selection, reports, break-even benchmarks |
| Public API leaks Flame | Long-term coupling | Backend-neutral reports/artifacts/callables |
| F# representation instability | Fragile interop | Recommend .NET-friendly facades; exact CLR member binding |
| Type identity mismatch across ALCs | Runtime cast/call failures | Shared host dependencies; explicit load strategy tests |
| Existing `compiler` behavior changes | Backward compatibility regression | Preserve alias and package defaults |
| Function providers require backend-name edits | Extensibility erosion | Semantic capability migration |
| Incomplete exception semantics | Wrong behavior | Defer exception-sensitive passes and unsupported flows |
| Upstream Flame API drift | Maintenance burden | Pin revision/package; adapter boundary; upgrade tests |
| Non-deterministic generated artifacts | Cache and reproducibility problems | Stable ordering/naming; provenance; reproducibility tests |
| Backend becomes Wist-specific | Framework architecture erosion | AIR-only input; architecture guardrails |
| Silent fallback hides unsupported paths | Unexpected latency/semantics | Structured explicit failure |

## 39. Forbidden shortcuts

Do not implement the integration by:

- replacing AIR with Flame IR;
- bypassing Bytecode/AIR from Wist AST;
- adding Flame references to BasicCore or frontend modules;
- adding `if (backend == "flame")` in generic framework layers;
- adding `Flame` to a central enum as the only extension mechanism;
- changing `compiler` to mean `optimized-cil`;
- forwarding arbitrary intrinsic strings to Flame and hoping the CIL selector supports them;
- resolving methods by string inside the backend;
- scanning all loaded assemblies;
- treating unknown managed calls as pure;
- adding feature intrinsics to the interpreter for parity convenience;
- storing locals or artifact state in static global containers;
- introducing an unbounded static assembly/delegate cache;
- silently falling back to another backend;
- claiming all programs are faster;
- shipping GPL dependencies inside Apache packages without an explicit decision;
- documenting future CLI commands as executable current behavior;
- weakening existing architecture or documentation tests to admit the backend.

## 40. Open questions

These questions require explicit decisions during implementation:

1. Can Flame be dual-licensed or otherwise consumed under a compatible distribution model?
2. Which exact Flame projects are necessary for the first prototype?
3. Should the initial artifact load eagerly at compile time or lazily at first session creation?
4. What is the stable hidden ABI for execution environment and external bindings?
5. Should a backend-neutral value graph exist between verified AIR CFG and Flame, or is a carefully separated direct lowerer sufficient?
6. How should source locations be preserved into PDB and reports?
7. Which current CIL stack simulation logic should be replaced by the shared verifier, and in which phase?
8. How should decimal operations be represented for maximal parity and useful optimization?
9. What is the minimum effect model that Flame passes can consume safely without forking upstream?
10. Should custom UniversalToolchain effects be translated into Flame analyses or enforced by restricting pass selection?
11. How should persistent artifacts bind user assemblies across deployments?
12. Is offline generation a separate CLI package or part of `wistc`?
13. What artifact API can support both `DynamicMethod` and assembly-backed delegates without reducing fast-path performance?
14. Which F# interop shapes are officially supported beyond normal CLR methods?
15. Which benchmark workloads represent real intended users rather than synthetic arithmetic only?
16. What support policy applies when an upstream Flame revision changes generated CIL?
17. Should the backend remain named `optimized-cil` if the implementation later changes away from Flame?

## 41. Recommended first implementation slice

The first coding task should not reference Flame.

Implement:

```text
AIR instruction stream
  -> deterministic CFG
  -> typed fixed-point stack verification
  -> stable diagnostics
```

Then adapt the current CIL validation path to consume or cross-check that analysis where feasible.

The second slice should introduce semantic effects and managed call descriptors.

Only the third major slice should add the Flame dependency and produce non-executable SSA inspection output.

This order ensures that the project gains durable architecture even if licensing or upstream API constraints stop the integration.

## 42. Product positioning after successful implementation

A careful public description would be:

> UniversalToolchain is a modular .NET DSL/runtime framework that composes restricted domain languages and can execute the same selected semantics through a reference interpreter, a low-latency dynamic CIL compiler, or an optional SSA optimizing CIL backend.

A careful user recommendation would be:

- use `interpreter` for diagnostics, reference behavior, and low preparation cost;
- use `cil` for short formulas and low compilation latency;
- use `optimized-cil` for larger compile-once/run-many workloads after measuring the break-even point;
- use offline artifacts when startup-time compilation or deployment reproducibility matters.

Do not lead with “powered by Flame.” Flame is an implementation partner. UniversalToolchain's product value is language composition, controlled semantics, runtime selection, and explainable execution tiers.

## 43. Success definition

The integration is successful only if it provides user value while making the framework more universal.

Success is not:

- a project reference that compiles;
- a demo that lowers one arithmetic expression;
- a benchmark that omits compilation cost;
- a new backend name in an enum;
- a large list of enabled Flame passes.

Success is:

- correct shared AIR verification;
- backend-independent semantic contracts;
- safe CLR interop;
- explicit effect legality;
- real third-backend activation;
- unloadable and reproducible artifacts;
- structured user diagnostics;
- semantic and observable-effect parity;
- measured workload guidance;
- compatible licensing;
- no erosion of existing architecture laws.

## 44. Reference links

UniversalToolchain documents:

- [Project positioning](../../project-positioning.md)
- [Current architecture status](../../CURRENT_ARCHITECTURE_STATUS.md)
- [Architecture rules](../../ARCHITECTURE_RULES.md)
- [Bytecode and AIR](../../architecture/bytecode-and-air.md)
- [Backends and semantic parity](../../architecture/backends-and-parity.md)
- [Backend contracts](../../reference/backend-contracts.md)
- [Intrinsic reference](../../reference/intrinsics-reference.md)
- [Security](../../SECURITY.md)

Relevant implementation contracts:

- [`PreparedExecutionBuilder<TCompilationOutput>`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/BasicCore/Core/PreparedExecutionBuilder.cs)
- [`IAbstractIrCompiler<TCompilationOutput>`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/BasicCore/Contracts/IAbstractIrCompiler.cs)
- [`ICompiledArtifact<TCompilationOutput>`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/BasicCore/Compilation/ICompiledArtifact.cs)
- [`IDialectBackendRuntimeRegistrar`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/UniversalToolchain.Dialects.Integration/IDialectBackendRuntimeRegistrar.cs)
- [`RuntimeBackendIntrinsicRegistry`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/UniversalToolchain.Dialects.Integration/RuntimeBackendIntrinsicRegistry.cs)
- [`BuiltinFunctionDescriptor`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/UniversalToolchain.Functions.Abstractions/BuiltinFunctionDescriptor.cs)
- [`FunctionPurity`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/UniversalToolchain.Functions.Abstractions/FunctionPurity.cs)
- [`ThirdBackendRuntimeComponentContractTests`](https://github.com/Misha1302/Wist2/blob/master/UniversalToolchain/UniversalToolchain.Dialects.Tests/RuntimeLoading/ThirdBackendRuntimeComponentContractTests.cs)

External references:

- [Flame repository](https://github.com/jonathanvdc/Flame)
- [Flame introduction](https://jonathanvdc.github.io/Flame/articles/intro.html)
- [Flame API documentation](https://jonathanvdc.github.io/Flame/api/)
- [Mono.Cecil](https://github.com/jbevain/cecil)
- [.NET AssemblyLoadContext conceptual documentation](https://learn.microsoft.com/dotnet/core/dependency-loading/understanding-assemblyloadcontext)

[Back to the design dossier index](index.md)
