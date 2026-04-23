# Contributing

## Before you start

- Use .NET SDK `10.0.103` (see `UniversalToolchain/global.json`).
- Read canonical repo docs:
    - `readme.md` (project overview and scope)
    - `docs/PROJECT_RULES.md` (coding standards)
    - `AGENTS.md` when making AI-assisted or agent-driven changes

## Build and test

Run from repository root:

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Wist.sln -c Release --no-build
```

This validation path runs all test projects currently included in `UniversalToolchain/Wist.sln`, including
`UniversalToolchain/Tests.Legacy/Tests.Legacy.csproj`.

## Change expectations

- Keep changes minimal, targeted, and deterministic.
- Prefer extending existing architecture over adding special-case paths.
- Avoid hardcoding dialect/module assumptions into framework-level contracts.
- Preserve the project's existing universality, layering, and composition principles.
- Do not make convenience entities easy to grow into hidden sources of framework truth.
- Prefer designs where reducing universality requires an explicit architectural change rather than a tiny local patch.
- New tests must not inherit from `Tests.Legacy.LegacyTestBase`; use `Tests.Infrastructure.DialectTestHostInfrastructure` with `Tests.Infrastructure.BackendParityInfrastructure`.
- If behavior changes, add or update tests in the same change.
- If structure/behavior meaningfully changes, update docs in the same change.
- When changing runtime manifests, runtime catalogs, exact activation, backend registrar resolution, or canonical bootstrap behavior, update canonical docs in the same change: `readme.md`, `docs/current-canonical-runtime-pipeline.md`, `docs/runtime-manifest-activation-model.md`, and `docs/runtime-manifest-format.md` when manifest shape/semantics change.

## Runtime composition expectations

- Keep the canonical dialect path deterministic: dialect compilation, build-plan construction, manifest-backed runtime
  selection, then host creation.
- Keep runtime selection and host creation separate. Selection resolves modules, optimizers, and backends; host creation
  instantiates the selected runtime surface.
- Treat shipped example dialects as runnable references. Keep commands and paths in their READMEs valid from repository
  root.
- Add or update architecture guardrail tests when changing catalogs, resolvers, runtime manifests, backend registrars, or
  optional facade layers.
- Do not turn compatibility/eager discovery helpers into hidden decision-makers for framework-level dialect composition.

## Test suite rules

- New tests must not inherit from `LegacyTestBase`.
- Backend-specific checks belong in `Tests/Backends`.
- Public behavior tests belong in `Tests/Core`.
- Implementation-detail checks belong in `Tests/Internal`.
- Reusable helpers belong in `Tests/Infrastructure`.
- Repetition and parallel stability checks belong in `Tests/Stress`.
- Historical or quarantine tests must live in an explicit legacy project or `Tests/Legacy`.
- New explicit single-backend tests must use `DialectTestHostInfrastructure`.
- Backend parity checks must use `BackendParityInfrastructure`.
- Do not leave inactive tests hidden behind `<Compile Remove=...>` in the active project.
- Prefer explicit internal contracts via `InternalsVisibleTo("Tests")` over reflection with type-name strings.
- Avoid hidden backend result casts inside fixtures.
- Avoid mixing unrelated concerns in one fixture.
- Add architecture guardrail tests when introducing new framework-adjacent catalogs, registries, profile loaders, or optional facade layers.

This cleanup did not tighten negative assertions. It focused on structure, isolation, and maintainability so future test changes stay easier to reason about and review.

## Documentation quality

- Keep docs aligned with real implementation.
- Ensure commands in docs run from repository root.
- Keep example paths and CLI flags aligned with real shipped dialect files.
- Remove stale or legacy wording instead of preserving contradictory notes.
- Every tracked markdown bash fence is executed in CI from repository root.
- Long-running bash fences should be isolated in their own block and may declare `ci-timeout` plus `ci-allowed-exit-codes` in the fence header.
- Interactive or documentation-only bash fences may declare `ci-run=false` to stay visible in docs without being executed in CI.
- Command blocks that intentionally mix successful and failing commands may annotate the next command with `# ci: expect-exit=1` (or a comma-separated list of exit codes).
- Line-level `# ci:` directives currently support single-line commands only and must not be mixed with fence-level `ci-allowed-exit-codes`.

## Pull requests

- Explain what changed and why.
- Include validation commands and outcomes.
- Keep PR scope coherent (avoid unrelated rewrites).
- Call out any architectural boundary added to preserve universality or reduce future technical debt.
