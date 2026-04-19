# Contributing

## Before you start

- Use .NET SDK `10.0.103` (see `UniversalToolchain/global.json`).
- Read canonical repo docs:
    - `readme.md` (project overview and scope)
    - `PROJECT_RULES.md` (coding standards)
    - `AGENTS.md` when making AI-assisted or agent-driven changes

## Build and test

Run from repository root:

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

`UniversalToolchain/Tests.Legacy/Tests.Legacy.csproj` is a quarantine suite for disabled historical tests and is not part of default validation.

## Change expectations

- Keep changes minimal, targeted, and deterministic.
- Prefer extending existing architecture over adding special-case paths.
- Avoid hardcoding dialect/module assumptions into framework-level contracts.
- New tests must not inherit from `Tests.Legacy.LegacyTestBase`; use `Tests.Infrastructure.DialectTestHostInfrastructure` with `Tests.Infrastructure.BackendParityInfrastructure`.
- If behavior changes, add or update tests in the same change.
- If structure/behavior meaningfully changes, update docs in the same change.

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

This cleanup did not tighten negative assertions. It focused on structure, isolation, and maintainability so future test changes stay easier to reason about and review.

## Documentation quality

- Keep docs aligned with real implementation.
- Ensure commands in docs run from repository root.
- Remove stale or legacy wording instead of preserving contradictory notes.
- Every tracked markdown bash fence is executed in CI from repository root.
- Long-running bash fences should be isolated in their own block and may declare `ci-timeout` plus `ci-allowed-exit-codes` in the fence header.

## Pull requests

- Explain what changed and why.
- Include validation commands and outcomes.
- Keep PR scope coherent (avoid unrelated rewrites).
