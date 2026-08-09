---
audience: framework-contributor
status: current
lastVerifiedAgainst: languageplan-single-runtime-s11
---

# Contributing

## Before you start

- Use .NET SDK `10.0.103` with the roll-forward policy in `UniversalToolchain/global.json`.
- Read canonical repo docs:
    - `readme.md` for project overview and scope;
    - [Current Architecture Status](/CURRENT_ARCHITECTURE_STATUS) and [Project Map](/architecture/project-map) for public boundaries;
    - [Current Canonical Runtime Pipeline](/current-canonical-runtime-pipeline) before changing Wist planning/runtime code;
    - `internal-docs/policies-and-reports/project-positioning.md` for repository-only positioning policy;
    - `internal-docs/policies-and-reports/PROJECT_RULES.md` for repository-only coding standards;
    - `internal-docs/policies-and-reports/ARCHITECTURE_RULES.md` for repository-only architecture guardrails;
    - `AGENTS.md` when making AI-assisted or agent-driven changes.

## Build and test

Run from repository root:

```bash ci-run=false
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Wist.sln -c Release --no-build
```

The canonical repository gate is stricter than this convenience block: `build.sh`/`build.ps1` also enforce the exact test manifest, architecture guards and related repository contracts. Historical or quarantined tests must not be hidden from the active contract through project-file exclusions.

## Change expectations

- Keep changes minimal, targeted and deterministic.
- Prefer extending existing generic contracts over adding Wist-only semantic owners.
- Avoid hardcoding dialect/module assumptions into framework-level contracts.
- Preserve universality, layering and composition boundaries.
- Do not make convenience facades easy to grow into hidden sources of framework truth.
- If behavior changes, add or update tests in the same change.
- If structure or behavior meaningfully changes, update canonical docs in the same change.
- When changing `LanguageDefinition`, feature/contribution descriptors, `LanguageCompiler`, `LanguagePlan`, route assembly or runtime materialization, update the canonical runtime and language-authoring docs that describe that owner.
- Runtime-manifest emitter/serializer docs describe that artifact format only. Do not treat them as authority for Wist semantic selection or execution.
- When changing module authoring patterns, aliases, parser priorities, AST visitors, bytecode tags, AIR shape, backend artifacts or parity assumptions, update the matching contract docs in the same change.

## Runtime composition expectations

The canonical Wist path is:

```text
.wistdialect/preset
  → LanguageDefinition
  → LanguageCompiler
  → LanguagePlan
  → LanguageRuntime
  → exact planned components
```

Contributor rules:

- `LanguageCompiler` is the only semantic planner. Do not add a second build-plan, selected-runtime-plan or backend-selection owner behind a Wist helper.
- The Wist configuration frontend may translate aliases/policy into typed generic contracts, but it must not perform dependency closure or runtime selection itself.
- Feature dependency closure, contribution/provider resolution, exclusions, ordering and backend routes belong to `LanguageCompiler`.
- `LanguageRuntime` materializes the exact graph captured by `LanguagePlan`; materialization must not add features, change order or choose another backend.
- Runtime component sources must match exact package provenance for materialized components.
- Tooling-only planned contributions must not become runtime dependencies merely because they are present in the plan.
- `UniversalToolchain.Dialects.Wist` is compatibility-only and non-packable; canonical Wist production projects must not depend on it.
- Treat shipped example dialects as runnable references and keep their source/configuration semantics aligned with the canonical planner.
- Add or update architecture guardrails when changing canonical owner boundaries.

## Wist configuration expectations

- `use` requests typed features/modules; do not require callers to duplicate transitive feature dependencies that the planner owns.
- `exclude` must reach `LanguageDefinition.ExcludedContributions`; required excluded contributions must fail closed in planning.
- Security and intrinsic directives must translate to typed policy before planning.
- Unknown aliases/capabilities must fail explicitly rather than becoming hidden activation switches.
- Source identity may affect provenance/hash identity, but semantic parity tests across different source names must compare typed semantic projections rather than `PlanHash` equality.

## Module and pipeline expectations

- Read `docs/guides/module-authoring.md` and [Create Your First Module](/write-modules/create-your-first-module) before adding or reshaping a Wist module.
- Read [Module Contracts](/reference/module-contracts) before changing token names, parser creator priorities, AST visitors, shared state, bytecode tags or backend capability behavior.
- Read `docs/architecture/bytecode-and-air.md` before changing bytecode, AIR, translation or optimizer semantics.
- Read `docs/architecture/backends-and-parity.md` before changing backend behavior, backend artifacts, intrinsics or parity tests.
- Register built-in Wist features/contributions with the typed Wist LanguagePack/catalog owner; attributes or generated runtime-manifest metadata are not a substitute for the canonical `LanguagePlan` registration path.
- Do not add convention-only hidden contracts when a documented contract, verifier, shared constant or test can make the rule explicit.

## Test suite rules

- New tests must not inherit from `LegacyTestBase` or other historical/quarantine fixture bases.
- Backend-specific checks belong in `Tests/Backends`.
- Public behavior tests belong in `Tests/Core`.
- Implementation-detail checks belong in `Tests/Internal`.
- Reusable helpers belong in `Tests/Infrastructure`.
- Repetition and parallel stability checks belong in `Tests/Stress`.
- New explicit single-backend tests should use canonical runtime test infrastructure rather than reconstructing retired Wist composition workflows.
- Backend parity must execute both backends from one canonical plan when that is the contract under test.
- Prefer explicit internal contracts via `InternalsVisibleTo("Tests")` over reflection with type-name strings.
- Avoid hidden backend result casts inside fixtures.
- Avoid mixing unrelated concerns in one fixture.
- Add architecture guardrail tests when introducing new framework-adjacent catalogs, registries or optional facade layers.

## Documentation quality

- Keep docs aligned with real implementation.
- Ensure commands in docs run from repository root.
- Keep example paths and CLI flags aligned with real shipped dialect files.
- Remove stale or legacy wording instead of preserving contradictory current-state descriptions.
- Do not describe `DialectBuildPlan`, `SelectedRuntimePlan`, manifest-backed Wist selection or `WistDialectExecutionWorkflow` as the current production path.
- Every tracked markdown bash fence is executed in CI from repository root unless explicitly marked `ci-run=false` with an explanation.
- Long-running bash fences may declare `ci-timeout` plus `ci-allowed-exit-codes`.
- Interactive or documentation-only bash fences may declare `ci-run=false`.
- Command blocks that intentionally mix successful and failing commands may annotate the next command with `# ci: expect-exit=1` or a comma-separated list of accepted codes.

## Pull requests

- Explain what changed and why.
- Include exact validation commands/results or provider-backed workflow receipts.
- Distinguish historical/superseded diagnostic evidence from exact-head acceptance evidence.
- Keep PR scope coherent.
- Call out architectural boundaries added to preserve universality or prevent a retired owner from returning.
- Do not update exact test counts speculatively; reconcile them only from a semantically clean observed test run.
