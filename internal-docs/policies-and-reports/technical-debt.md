# Technical debt

This document tracks active architectural and repository hygiene debt for UniversalToolchain.
It replaces the old `internal-docs/policies-and-reports/technical-debt.md` file so debt is documented with the rest of the project docs instead of being hidden inside the source tree.

## Runtime discovery and activation

### Problem

Broad reflection-heavy discovery must stay out of the canonical runtime path. The canonical path should keep moving toward manifest-backed selected-component activation.

### Current risk

If eager discovery becomes a hidden decision-maker, unrelated assemblies or test-only exports can affect runtime composition and make repeated runs non-deterministic.

### Desired direction

Keep exact selected-component activation canonical. Scope eager discovery to compatibility, bootstrapping, or explicitly documented tooling scenarios.

### Exit criteria

- Runtime selection is deterministic and manifest-backed.
- Test-only exports cannot leak into canonical Wist runtime catalogs.
- Runtime docs clearly distinguish selected activation from compatibility discovery.

## BasicCore stage boundaries

### Problem

`BasicCoreImpl` still carries abstraction leakage around pipeline stage boundaries and extension contracts.

### Current risk

Framework-level code can accumulate dialect/module assumptions and become harder to reuse for non-Wist DSLs.

### Desired direction

Clarify stage contracts and make extension points explicit rather than convention-only.

### Exit criteria

- Frontend, bytecode, AIR, optimization, and backend responsibilities are documented and test-protected.
- New modules do not need hidden knowledge of internal stage coupling.

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

`ModuleContractEnforcementPolicy.AllowUndeclared` is retained only for the Wist observation profile while shipped modules finish declaring complete bytecode and AIR contracts. It emits diagnostics instead of silently accepting unknown operations. The removal gate is full strict-profile coverage for every shipped module and example.

No other production compatibility adapter or legacy payload decoder is supported.

## Compiler/interpreter parity

### Problem

Compiler and interpreter behavior parity is central to the project, but supported divergences need to be explicit and tested.

### Current risk

Optimizations, native arithmetic, external calls, or local-variable handling can create backend-specific behavior that is not visible until integration tests fail.

### Desired direction

Keep parity tests around public behavior and add explicit tests for intentional backend differences.

### Exit criteria

- Public language behavior has parity coverage for interpreter and compiled execution.
- Intentional divergences are documented and tested.

## Global mutable state

### Problem

Global mutable state remains a risk for repeated runs, long-lived hosts, tests, and dynamically composed dialects.

### Current risk

Shared registries, caches, or static mutable collections can make runtime behavior order-dependent.

### Desired direction

Keep mutable static state guarded, scoped, or replaced with injected/deterministic runtime state.

### Exit criteria

- Static mutable state is covered by guardrail tests.
- Known exceptions have documented rationale and containment boundaries.

## Dialect subsystem maturity

### Problem

The dialect subsystem exists across parsing, core, integration, frontend, and Wist projects, but composition ergonomics and policy depth are still evolving.

### Current risk

The framework may expose too many internal concepts before the external authoring flow is stable.

### Desired direction

Continue improving dialect authoring, runtime profiles, selected runtime plans, and diagnostics without hardcoding Wist-specific assumptions into framework layers.

### Exit criteria

- Dialect authoring docs match real APIs and examples.
- Runtime profiles and selected plans are understandable from documentation alone.
- Module/backend authors have clear extension contracts.

## Module grouping and dependency ordering

### Problem

Module grouping and dependency-order concepts are only partially represented.

### Current risk

Composition order can become implicit, fragile, or test-only rather than part of the framework contract.

### Desired direction

Represent module dependencies and ordering as first-class deterministic contracts.

### Exit criteria

- Module order is deterministic.
- Dependency violations are diagnosed clearly.
- Tests protect representative module combinations.

## Repository hygiene

### Problem

Generated artifacts, one-off repair scripts, and stale examples can accumulate in source control.

### Current risk

The repository becomes harder to understand and new contributors cannot distinguish canonical code from temporary tooling.

### Desired direction

Keep source control focused on maintained runtime code, tests, docs, examples, and intentionally supported tools.

### Exit criteria

- Obsolete generated artifacts and one-off scripts are removed.
- Internal tools are documented as internal tools.
- Examples remain runnable from repository root.

## Structured debug traces

### Problem

The previous text-log debugging surface represented only a partial legacy
pipeline and could mislead users about current AIR, SSA, verifier and backend
boundaries.

### Current risk

Without a structured trace contract, debugging can collapse multiple compiler
boundaries into one vague failure point or encourage ad hoc log formats.

### Desired direction

Continue the [Debug Trace v2](/architecture/debug-trace-v2) direction. The first
`wistc run --trace` artifact is implemented, but fine-grained lexer/parser,
bytecode, AIR, SSA and backend artifact stages are not complete yet.

### Exit criteria

- `wistc run --trace trace.json` writes a deterministic structured trace.
- Trace-enabled execution is semantically equivalent to trace-disabled execution.
- Failed compilation can flush a partial trace with stage-local diagnostics.
- Default traces omit source text and runtime values unless explicitly enabled.
- A future viewer consumes real `trace.json`, not legacy sample logs.

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
| Serial solution build is slow | Repository build contract | `build.sh`/`build.ps1`, CI uses canonical entrypoint | Measured clean build time and either fixed project graph or accepted documented threshold | Re-evaluate after SDK feature-band upgrade |

Owners must update this table with evidence, not only a status adjective. Package boundary work is complete only when the external-consumer smoke passes with the smaller closure; resource isolation is complete only when limits are enforced by a separate execution boundary or the product scope excludes untrusted authors.
