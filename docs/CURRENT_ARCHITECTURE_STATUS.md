# Current architecture status

This document describes the currently supported surface of the branch. It is intentionally short and practical.

Use it to distinguish current behavior from future or historical design plans.

## Release positioning

Status: public preview / release-gate scope, not a finalized 1.0 platform.

Latest architecture/release-hardening pass: v7 (preview.4 SSA public-API candidate). The canonical validation entrypoint is `./build.sh`; release evidence must record the exact SDK, restore source policy, build result, three test-project totals, package-surface check, external consumer smoke, and documentation checks.

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

### Preview.3 boundary changes

- CLR type/method discovery is execution-scoped and immutable. Hosts supply `AllowedAssemblies`; only shipped `BasicStdLib` is added automatically, and AIR/SSA/backends do not scan dialect implementation assemblies, the AppDomain, or the filesystem.
- Dynamic-method invokers own their method and runtime handle lifetime; compiled functions are no longer rooted in process-wide static storage.
- `Validate` and `TryCompile` expose structured diagnostics with stable Wist codes and stage metadata.
- Source length and external parameter count are bounded by host-owned preflight limits. These controls are not execution-time or memory isolation.
- `CreateSafeFormulas`, `CreateBusinessRules`, `CreateTrusted`, and `CompileFunc` are obsolete compatibility APIs. First-party code uses `CreateRestrictedArithmetic` and `Compile<TDelegate>`.
- `UniversalToolchain.Wist.dll` is the supported public facade. The current broad physical package closure remains tracked debt and is protected from accidental growth, not declared stable.

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

Status: experimental, verifier-gated `AIR -> SSA -> AIR` optimization route with a public Wist facade option. It is not an SSA-native backend and is not enabled by default.

Current public Wist contract:

- `WistEngineOptions.Optimization.Ssa` selects `Disabled`, `Prefer`, `Require`, or `Debug`;
- `Summary` and `Detailed` diagnostics are supported;
- `Validate`, `TryCompile`, and successful compiled programs expose a `WistOptimizationReport`;
- the report records actual route use, fallback, profile, instruction counts, executed passes, diagnostics, and debug trace;
- only known unsupported-route diagnostics may fall back under `Prefer`; unexpected optimizer defects fail with structured diagnostic code `UTC-WIST-SSA-001`;
- `WistDialectSource.FromText` and `WistEngineOptions.FromDialectText` allow inline dialects without temporary files;
- the shipped `ssa-preview` example demonstrates the opt-in route.

Current internal contract:

- generic IR identity and pipeline contracts live in `UniversalToolchain.Ir.Abstractions`;
- AIR is structurally verified before lowering, SSA is verified after lowering and after every pass, and emitted AIR is verified again;
- the immutable SSA model uses block arguments, typed values, callable descriptors, facts, effects, and deterministic descriptor snapshots;
- the route includes local constant folding, SCCP-lite, branch folding/unreachable-block cleanup, and dead pure-instruction elimination;
- Wist projects native int32 add/subtract/multiply calls onto canonical SSA callables so supported arithmetic can be optimized rather than merely round-tripped;
- managed calls carry exact execution-scoped `MethodInfo`/`ConstructorInfo` bindings through lowering, optimization, and emission; production SSA code does not rediscover methods through `AppDomain`, `Type.GetType`, or filesystem scanning;
- repeated equivalent bindings are compared structurally, while conflicting bindings and duplicate extension-pack/pass identifiers fail fast;
- target capabilities and diagnostic modes affect route construction and execution rather than acting as stored no-op options;
- the runtime optimizer alias is `Ssa`; `SsaPreview` is not a supported alias.

Current boundary:

- supported Wist arithmetic and selected managed calls can use the route;
- unsupported shapes are rejected or, only under `Prefer`, reported as a controlled fallback;
- CLR value mapping and arbitrary SSA scheduling remain intentionally limited;
- CIL and interpreter continue to consume emitted AIR;
- no numerical performance advantage is claimed without a dedicated reproducible benchmark run;
- low-level `UniversalToolchain.Ssa.*` APIs remain experimental and are not part of the supported `UniversalToolchain.Wist` facade contract.

Required policy:

- new IR behavior must enter through generic contracts, explicit descriptors/bindings, and verifier-backed tests;
- `BasicCore` must not acquire Wist- or SSA-specific routing branches;
- broad conversion/optimization support requires differential or oracle coverage first;
- see `docs/architecture/ssa-coverage-matrix.md` before extending the route.

## Architecture-hardcode v4 status

Current behavior after v4:

- runtime provider allowlists are owned by selected backend/runtime composition, not inferred from optimized AIR;
- `RuntimeProviderPolicyComponent` is an auxiliary backend pipeline component and is excluded from module-contract selected-module tables unless a component explicitly implements the module-contract backend component interface;
- reusable modules `IdentifierModule`, `ScopesModule` and `VariablesModule` use module-owned facts rather than `UniversalToolchain.Wist.Contracts`;
- production optimizer/runtime planning reads C# calls through the typed/legacy `CSharpCallIntrinsicReader` path instead of comparing the raw `"call C#"` display string;
- `CallCSharp(...)` helper emission remains legacy-compatible so existing runtime/parity tests observe the normalized `call C#` AIR shape.

Verified after v4:

- `dotnet build Wist.sln` passed in the user environment;
- `dotnet test Wist.sln` passed in the user environment;
- the v4 report records sandbox restore/build limitations separately and should not be upgraded to a sandbox full-suite claim.

Required policy:

- auxiliary runtime/backend policy components must not become selected module-contract participants by convention;
- provider authorization remains composition-owned;
- AIR may request provider-backed calls but must not be the source of provider authorization;
- future intrinsic migrations must preserve legacy compatibility at decoder/emission boundaries until all compatibility tests and public docs are intentionally migrated.

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
