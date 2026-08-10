# Technical debt

This document tracks active architectural and repository hygiene debt for UniversalToolchain.
It is a current maintenance ledger, not a historical architecture snapshot.

## Canonical runtime materialization

### Problem

Broad reflection-heavy discovery, compatibility manifests and service-container registration must stay out of the canonical Wist semantic-selection path.

### Current control

Current Wist execution is planned once through:

```text
LanguageDefinition -> LanguageCompiler -> LanguagePlan -> LanguageRuntime
```

The Wist configuration frontend translates aliases/policy; `LanguageCompiler` owns dependency closure, contribution/provider resolution, exclusions, ordering and backend routes. `LanguageRuntime` materializes the exact selected runtime graph from exact package/component sources.

The former reflection/runtime-profile `ToolchainRuntimeHost` topology is retired in S13. Runtime-manifest emission metadata remains only for explicit tooling/package scenarios and is not a second Wist planner.

### Current risk

If runtime-manifest metadata or a recreated reflection/profile host becomes a hidden Wist decision-maker, unrelated assemblies or test-only exports could again create two sources of semantic truth.

### Desired direction

Keep `LanguagePlan` as the sole selected Wist semantic graph. Keep remaining manifest emission metadata explicitly scoped and unable to alter Wist plan selection.

### Exit criteria

- Wist feature/contribution/backend selection is deterministic and `LanguagePlan`-backed.
- Runtime materialization validates exact package/source provenance for executable components.
- Tooling-only planned contributions do not create false runtime-source requirements.
- Test-only exports cannot leak into canonical Wist component selection.
- Public/contributor docs distinguish generic manifest/host compatibility infrastructure from the Wist production path.
- Architecture tests reject reintroduction of the retired Wist planner/runtime owners and stale canonical documentation claims.

## BasicCore stage boundaries

### Current state

S12 physically retires `BasicCoreImpl` and `PreparedExecutionBuilder`. Their orchestration duties are not moved into a replacement coordinator: canonical execution is owned by `LanguageRuntime` and exact plan-owned artifact route components, while BasicCore retains only reusable stage contracts/mechanics.

### Remaining risk

S13 still needs to remove topology-only compatibility contracts and project edges that no longer have a live owner. A future helper could also regress into a second end-to-end coordinator if architecture guards are weakened.

### Exit criteria

- Frontend, bytecode, AIR, optimization, backend and runtime ownership remain explicit and test-protected.
- Retired BasicCore orchestrator symbols/paths cannot reappear.
- No BasicCore production file combines the full lexer/parser/lowering/compiler/executor ownership set.
- Remaining generic compatibility contracts either have an explicit owner/use case or are deleted during S13.

## Intrinsic governance

### Problem

Intrinsic generation and consumption need one canonical representation so backend-specific details cannot leak across layers.

### Current control

Production AIR uses structured `IntrinsicInvocation` payloads. Stable capability identifiers are metadata produced by the encoder, not instruction operands that require decoding.

### Exit criteria

- Production emitters generate only `IntrinsicInvocation` payloads.
- Architecture tests reject raw string intrinsic payloads.
- Backend capability identifiers remain metadata and are never decoded from AIR operands.

## Explicit compatibility boundary

`ModuleContractEnforcementPolicy.AllowUndeclared` is retained only where current module-contract profiles explicitly require observation/compatibility behavior. It must emit diagnostics rather than silently accepting unknown operations, and it must not become a mechanism for restoring retired Wist runtime selection.

Runtime profiles and `ToolchainRuntimeHost` are retired. Remaining runtime-manifest emission metadata is not an authoritative Wist semantic owner.

No compatibility adapter may select a second Wist contribution graph after `LanguageCompiler` has produced `LanguagePlan`.

## Compiler/interpreter parity

### Problem

Compiler and interpreter behavior parity is central to the project, but supported divergences need to be explicit and tested.

### Current control

S11 parity infrastructure executes both supported Wist backends from one canonical multi-route `LanguagePlan`, rather than constructing two independent semantic plans.

### Current risk

Optimizations, native arithmetic, external calls, or local-variable handling can create backend-specific behavior that is not visible until integration tests fail.

### Desired direction

Keep parity tests around public behavior and add explicit tests for intentional backend differences.

### Exit criteria

- Public language behavior has parity coverage for interpreter and compiled execution.
- Intentional divergences are documented and tested.
- Backend parity never depends on two independently planned Wist runtimes when one-plan parity is the contract under test.

## Global mutable state

### Problem

Global mutable state remains a risk for repeated runs, long-lived hosts, tests, and dynamically composed languages.

### Current risk

Shared registries, caches, or static mutable collections can make runtime behavior order-dependent.

### Desired direction

Keep mutable static state guarded, scoped, or replaced with injected/deterministic runtime state.

### Exit criteria

- Static mutable state is covered by guardrail tests.
- Known exceptions have documented rationale and containment boundaries.
- Independent `LanguageRuntime` instances do not share mutable per-session component state.

## Dialect subsystem maturity

### Problem

The generic dialect subsystem still spans parsing, core and integration compatibility layers while the external typed language-authoring flow continues to mature.

### Current risk

Users and contributors can still confuse runtime-manifest metadata with the current Wist public execution architecture or attempt to recreate the retired runtime-profile/host topology.

### Desired direction

Keep generic dialect parsing/profile/tooling contracts explicit while converging public language authoring on typed `LanguageDefinition`, package/contribution descriptors, `LanguageCompiler`, `LanguagePlan` and `LanguageRuntime` contracts. Do not rebuild Wist-specific planning inside generic compatibility helpers.

### Exit criteria

- Dialect authoring docs match real APIs and examples.
- Runtime-manifest docs state their metadata/tooling scope and retired runtime-profile docs point to typed `LanguageDefinition` policy.
- Wist public docs consistently show the single-plan runtime path.
- Module/backend authors have clear typed extension contracts.

## Module grouping and dependency ordering

### Problem

Wist group aliases are now data-only source shorthand, while semantic feature dependencies and contribution ordering live in typed planner contracts. The boundary must stay clear.

### Current control

`WistDialectGroupCatalog` expands group names to module aliases before `LanguageDefinition` construction. `LanguageCompiler` owns dependency closure and contribution-order constraints.

### Current risk

Group catalogs could accidentally grow into another dependency resolver, or contributors could duplicate transitive dependencies manually in every dialect and let docs drift from planner behavior.

### Desired direction

Keep groups ergonomic/data-only. Represent dependency and ordering semantics through typed feature/contribution contracts.

### Exit criteria

- Module order is deterministic.
- Dependency violations are diagnosed clearly by the canonical planner.
- `exclude` reaches `LanguageDefinition.ExcludedContributions` and required excluded contributions fail closed.
- Tests protect representative group/dependency combinations.

## Repository hygiene

### Problem

Generated artifacts, one-off repair scripts, stale examples and stale architectural prose can accumulate in source control.

### Current risk

The repository becomes harder to understand and new contributors cannot distinguish canonical code from temporary tooling or dated reviews.

### Desired direction

Keep source control focused on maintained runtime code, tests, docs, examples and intentionally supported tools. Mark historical review artifacts as dated snapshots instead of rewriting them as current truth.

### Exit criteria

- Obsolete generated artifacts and one-off scripts are removed.
- Internal tools are documented as internal tools.
- Examples remain runnable from repository root.
- Active policy/docs do not point at physically retired Wist owners as current architecture.

## Structured debug traces

### Problem

The previous text-log debugging surface represented only a partial legacy pipeline and could mislead users about current AIR, SSA, verifier and backend boundaries.

### Current risk

Without a structured trace contract, debugging can collapse multiple compiler boundaries into one vague failure point or encourage ad hoc log formats.

### Desired direction

Continue the [Debug Trace v2](/architecture/debug-trace-v2) direction. The first `wistc run --trace` artifact is implemented, but fine-grained lexer/parser, bytecode, AIR, SSA and backend artifact stages are not complete yet.

### Exit criteria

- `wistc run --trace trace.json` writes a deterministic structured trace.
- Trace-enabled execution is semantically equivalent to trace-disabled execution.
- Failed compilation can flush a partial trace with stage-local diagnostics.
- Default traces omit source text and runtime values unless explicitly enabled.
- A future viewer consumes real `trace.json`, not legacy sample logs.

## CI execution hygiene

### Problem

Current branch/PR workflows can accumulate superseded runs because `.NET CI` has both broad branch `push` and `pull_request` triggers and no workflow-level concurrency cancellation.

### Current risk

Rapid migration commits create duplicate queued runs, delaying exact-head evidence and making it easier to mistake superseded diagnostics for acceptance receipts.

### Desired direction

After the architecture migration is stable, evaluate a workflow concurrency key that cancels superseded runs without weakening required `master`/release evidence. Do not change CI semantics merely to make one migration stage finish faster.

### Exit criteria

- The required workflow set still runs unconditionally where release/aggregate policy requires it.
- Superseded PR/branch runs do not consume unnecessary runner capacity.
- Exact-head acceptance remains explicit and auditable.

## Long-term research ideas

- CIL optimizer roadmap: SSA-oriented passes, inlining strategy, and backend tuning.
- Broader frontend/parser strategy experiments: alternative parsing algorithms and extensibility models.
- Configuration format modernization where it improves determinism and tooling.
- Optional code generation for repetitive boilerplate where it preserves readability and correctness.

## Release-hardening debt ledger (legacy cycle 3)

| Debt | Canonical owner | Current control | Exit metric | Target decision |
|---|---|---|---|---|
| Physical NuGet closure remains broad | `UniversalToolchain.Wist.csproj` | Package check rejects test/benchmark assemblies and growth beyond 64 DLLs | Split optional SSA/tooling and reduce the default runtime closure without breaking an external consumer | Before stable 1.0 |
| No in-process execution timeout or memory quota | Host integration/security boundary | Restricted composition plus source/parameter preflight limits | Documented out-of-process runner or explicit decision that only trusted operators are supported | Before accepting arbitrary user-authored rules |
| Cross-solution concurrency is unsafe because solutions share projects and output directories | Repository build contract | `build.sh`/`build.ps1` keep solutions sequential but parallelize each project graph; CI uses canonical entrypoint | Clean parallel and isolated serial builds both pass; no concurrent writes to shared `bin`/`obj` paths | Re-evaluate only if solutions receive isolated output roots |

Owners must update this table with evidence, not only a status adjective. Package boundary work is complete only when the external-consumer smoke passes with the smaller closure; resource isolation is complete only when limits are enforced by a separate execution boundary or the product scope excludes untrusted authors.
