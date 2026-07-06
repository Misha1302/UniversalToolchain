# Current architecture status

This document describes the currently supported surface of the branch. It is intentionally short and practical.

Use it to distinguish current behavior from future or historical design plans.

## Release positioning

Status: public preview / release-gate scope, not a finalized 1.0 platform.

This branch can be released as a scoped Wist facade preview when build, test,
package, CLI and documentation checks pass. Do not describe it as a completed
general-purpose DSL workbench, a hardened sandbox, a stable generic runtime
package family, or a production SSA optimizer/backend layer.

The supported release claim is narrower:

- `UniversalToolchain.Wist` provides a first-contact Wist facade for controlled
  formula evaluation, validation and typed compiled invocation;
- shipped dialect presets demonstrate restricted and full Wist profiles with
  documented preview mappings;
- neutral runtime host, structured trace and SSA route exist as current
  foundations with the limitations listed below.

## Rules feature

Status: removed from this branch.

Removed surfaces:

- `UniversalToolchain.Rules.Abstractions`;
- Wist rule runtime files under `UniversalToolchain.Dialects.Wist/Rules`;
- `RuleDeclarationsModule`;
- `CompileRuleSet`;
- `rule-run`;
- `rule-schema`;
- executable `pricing-rules`, `validation-rules`, and `policy-rules` profiles;
- rule runtime type bindings;
- rule-specific diagnostic codes;
- raw-source rule declaration parser.

Do not restore rules code, rule profiles, rule CLI commands, or marker-only rule modules without a new explicit architecture task.

## FunctionCalls and SafeMathFunctions

Status: limited MVP.

Currently supported:

- source-level calls to provider-backed built-in functions;
- SafeMathFunctions through the neutral `function-calls-safe-math` example profile;
- interpreter/compiler parity coverage for the supported SafeMath MVP.

Not final yet:

- a shared function call planner;
- full type-directed overload resolution;
- final diagnostics parity for every function authoring error.

## Let bindings

Status: normal Wist `let` support exists.

There is no rule-local LetBindings layer in this branch because the rules feature has been removed.

Forbidden shortcuts:

- raw-source local binding scanners;
- restoring rule-local validation tests without first restoring an explicit rules architecture task.

## Interpreter runtime path

Status: reference universal-call backend.

Current policy:

- the interpreter executes core AIR control-flow/data opcodes and the two universal C# call intrinsics only: `call C#` and `call C# ctor`;
- the interpreter must not implement feature-specific or optimization-specific intrinsics;
- if a feature must work in the interpreter, it must lower to ordinary C# runtime calls before interpretation;
- backend-optimized intrinsics belong to backends that explicitly support them, such as CIL.

Forbidden interpreter intrinsics include `load_local`, `store_local`, `load_local_ref`, `load_external`, `store_external`, `load_*`, `add_*`, `sub_*`, `mul_*`, `div_*`, `cmp_*`, `load_bool`, and boolean operation intrinsics.

## Neutral runtime host

Status: first neutral execution host extracted inside the dialect integration boundary.

Currently supported:

- `ToolchainRuntimeHost` executes a selected dialect runtime configuration through backend-neutral `IArtifactCompiler` and `ICompiledArtifact` contracts;
- `ToolchainBackendRuntime` wraps selected backend runtimes without naming Wist facade types;
- Wist execution host remains as compatibility/convenience wrapper over the neutral runtime host;
- backend registrars register both the neutral runtime wrapper and the Wist compatibility runtime wrapper.

Not final yet:

- there is no standalone `UniversalToolchain.Runtime` package/project boundary;
- public non-Wist convenience APIs still need a stable embedding surface;
- Wist shipped presets remain reference-language assets, not generic runtime truth.

Required policy:

- new runtime execution behavior should target the neutral host/contracts first;
- Wist facade APIs must remain thin wrappers over selected runtime plans and compiled artifacts.

## Local variables runtime path

Status: migrated to execution-scoped C# runtime calls.

Current behavior:

- local variables are lowered to ordinary `call C#` instructions via `VariablesRuntimeCallProvider` and `VariablesRuntimeCalls`;
- local variable state is session-scoped through `ExecutionEnvironment` runtime context storage;
- `VariablesContainer<T>` static storage is removed from the production runtime path;
- `LocalVariablesOptimization` is removed from the current runtime path.

Interpreter path:

- the interpreter executes local variables through the canonical execution-scoped C# runtime calls;
- the interpreter must not receive `load_local`, `store_local`, or `load_local_ref`;
- local-variable intrinsics are reserved for backend-capability-gated optimized paths, such as CIL.

Future direction:

- any local-variable optimization must operate on generated C# runtime call patterns;
- such optimization may compress runtime-call patterns to local intrinsics only for backends that explicitly support those intrinsics;
- do not reintroduce local-variable intrinsics into the interpreter or static/global variable storage.

## Structured debug trace

Status: first CLI artifact phase implemented.

Currently supported:

- `wistc run --trace trace.json ...` writes a versioned JSON artifact;
- source text and runtime values are redacted by default;
- the trace includes source length/hash, dialect/backend metadata, coarse stages and result/error summary.

Not final yet:

- lexer/parser/AST/bytecode/AIR/SSA/backend artifact dumps are not emitted as detailed trace stages;
- there is no visual trace viewer;
- schema compatibility is protected for the first artifact shape only.

## Generic IR routing and SSA foundation

Status: callable-first pre-release foundation plus minimal SSA
model/verifier/conversion/optimization boundary, a no-optimization
`AIR -> SSA -> AIR` route, and opt-in preview runtime wiring through a dialect
optimizer.

Currently supported:

- generic IR identity and pipeline contracts in `UniversalToolchain.Ir.Abstractions`;
- the existing AIR boundary wrapped as `AirArtifact`;
- AIR-only legacy optimizer execution through a generic stage adapter inside `BasicCore`;
- deterministic AIR CFG construction and structural AIR verification in `UniversalToolchain.Air.Analysis`;
- typed AIR stack analysis for the current generic subset;
- immutable SSA model and `SsaArtifact` in `UniversalToolchain.Ssa.Abstractions`;
- structural SSA verifier in `UniversalToolchain.Ssa.Core`;
- descriptor-driven SSA operations with deterministic descriptor snapshots;
- shared SSA structural verification fact in `SsaFacts.StructuralVerification`;
- minimal `AIR -> SSA` converter in `UniversalToolchain.Ssa.Lowering`;
- minimal verifier-gated `SSA -> AIR` converter in
  `UniversalToolchain.Ssa.Emission`;
- verifier-gated SSA optimizer pipeline and local int32 constant folding pass
  in `UniversalToolchain.Ssa.Optimization`.
- verifier-gated `SsaRoundtripRoute` with `Off`, `Prefer`, `Require` and
  `Debug` policies for running `AIR -> SSA -> AIR` without applying SSA
  optimization passes;
- callable-first semantic descriptors, including managed `MethodInfo` and
  `ConstructorInfo` callables with conservative default effects and optional
  trusted `SsaManagedCallableAttribute` declarations;
- managed static methods, stack-receiver instance methods and constructors can
  round-trip through `AIR -> SSA -> AIR` as regular `SsaCall` instructions,
  lowering back to the ordinary AIR `call C#` and `call C# ctor` runtime
  surfaces.
- callable lowering is target-shaped: AIR intrinsic and managed-call targets
  can emit to the current AIR route; CIL opcode and interpreter-primitive
  targets are explicit unsupported targets for this route; ambiguity is reported
  only when multiple supported targets share the best priority;
- opt-in `Ssa` runtime optimizer manifest entry implemented by
  `SsaPreviewOptimizerModule`, which runs `AIR -> SSA -> SSA optimization ->
  AIR` without changing default Wist dialect profiles.

Verified on 2026-07-03:

- the uploaded local NuGet package bundle restored the test dependencies
  required by `Tests.csproj`, including NUnit, NUnit3TestAdapter,
  Microsoft.NET.Test.Sdk, coverlet and SharpFuzz;
- `Tests.csproj` restore and build succeeded with the local .NET 10 SDK and
  local package feeds;
- focused AIR/SSA tests passed 16/16;
- after the SSA optimization addition, focused AIR/SSA tests passed 20/20;
- after the SSA emission addition, focused AIR/SSA tests passed 23/23;
- after the SSA contract hardening pass, focused AIR/SSA tests passed 27/27,
  including return-shape/type validation, SSA fact propagation, optimizer fact
  effects and SSA-to-AIR CFG layout regression coverage;
- grouped contract/runtime regression checks passed 294/294 across
  `Tests.Ir`, `Tests.Architecture`, `Tests.Internal`, `Tests.Intrinsics`,
  `Tests.Backends`, `Tests.Stress`, and `Tests.Core` except CLI e2e;
- broader grouped NUnit execution passed 310/321 discovered test cases across
  `Tests.Air`, `Tests.Ssa`, `Tests.Ir`, `Tests.Architecture`,
  `Tests.Internal`, `Tests.Intrinsics`, `Tests.Core` except CLI e2e,
  `Tests.Backends` and `Tests.Stress`;
- `Wistc` Release build succeeded, and direct CLI smoke checks for
  `run --eval --backend interpreter "1 + 2"` and
  `run --eval --backend compiler "1 + 2"` both returned `3`;
- after the SSA model/verifier addition, `UniversalToolchain.Ssa.Core` builds
  successfully with its project references;
- after the AIR analysis/lowering addition, `UniversalToolchain.Ssa.Lowering`
  builds successfully with its project references;
- after the SSA emission addition, `UniversalToolchain.Ssa.Emission` builds
  successfully with its project references;
- a no-package smoke runner verified valid `AIR -> SSA` conversion and the
  unsupported-intrinsic failure path.

Verified on 2026-07-04:

- `Directory.Build.props` now disables parallel project-reference restore/build
  traversal because the local .NET 10 SDK can otherwise fail this repository's
  large project graph with silent `MSBuild` task failures;
- the SSA/AIR projects declare the project references they consume directly;
- `UniversalToolchain.Ssa.Core`, `UniversalToolchain.Ssa.Lowering`,
  `UniversalToolchain.Ssa.Emission` and
  `UniversalToolchain.Ssa.Optimization` all build in Release;
- `UniversalToolchain.Ssa.Optimization` emits
  `UniversalToolchain.Ssa.Optimization.dialect.runtime.json` with the canonical
  optimizer alias `Ssa`;
- `Tests.csproj` restore succeeded against the local package cache, with
  external `NU1900` vulnerability-feed warnings for `api.nuget.org`;
- `Tests.csproj` Release build succeeded with 45 `NU1900` warnings and
  0 errors.
- after the opt-in SSA preview optimizer wiring, `Tests.csproj` Release build
  succeeded with 0 warnings and 0 errors, and focused `Tests.Ssa` passed 32/32.
- after the callable-first pre-release pass, focused `Tests.Ssa` passed 61/61
  with managed callable attributes, trust-boundary tests, unsupported CLR type
  and open-generic rejection tests, managed static method/constructor
  round-trip tests and trusted pure managed callable folding coverage.
- after the no-optimization SSA route and callable lowering review fixes,
  focused `Tests.Ssa` passed 70/70 and full `Tests.csproj` passed 379/379
  on the local .NET 10 sidecar with local package feeds.

Not implemented yet:

- complete AIR to SSA conversion for all existing AIR intrinsics and runtime value types;
- complete SSA to AIR lowering for arbitrary SSA stack scheduling,
  multi-return shapes and backend execution beyond the current AIR intrinsic
  and managed-call lowering surfaces;
- a full SSA optimizer suite beyond the initial local constant folding pass;
- dialect syntax for intermediate-layer policies;
- SSA-native backend support.
- execution-scoped provider descriptors and unresolved generic methods are not
  backend-neutral SSA managed callables yet;
- CLR type mapping is still limited to bool, int32, float64 and managed object
  references.

Required policy:

- new IR layers must be introduced through generic IR contracts and verifiers;
- `BasicCore` must not hardcode SSA, Wist, or backend-specific routing branches;
- existing interpreter and CIL paths remain AIR consumers until a separate verified route is added.
- see `docs/architecture/ssa-coverage-matrix.md` before expanding SSA conversion or optimization support.

## Interpreter intrinsic surface

Status: intentionally minimal reference backend.

Current behavior:

- the interpreter executes core AIR opcodes (`Nop`, `Push`, `Drop`, `Jmp`, `JmpIf`, `JmpIfNot`, `Label`, `Annotate`);
- the interpreter executes only two intrinsics: `call C#` and `call C# ctor`;
- feature-specific or backend-optimized intrinsics (`load_*`, arithmetic, comparison, boolean, local/external storage intrinsics) are rejected by interpreter execution.

Required optimization policy:

- optimizers that produce non-call intrinsics must be backend-capability gated;
- optimized intrinsic IR may be produced for backends that explicitly support it (for example CIL);
- interpreter execution must remain a universal reference path and must not be used as a high-performance intrinsic backend.

## Documentation policy

`docs/DOCUMENTATION_RULES.md` defines how agents must handle stale Markdown examples and architecture documents.
