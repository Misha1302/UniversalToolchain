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
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

## Change expectations

- Keep changes minimal, targeted, and deterministic.
- Prefer extending existing architecture over adding special-case paths.
- Avoid hardcoding dialect/module assumptions into framework-level contracts.
- If behavior changes, add or update tests in the same change.
- If structure/behavior meaningfully changes, update docs in the same change.

## Documentation quality

- Keep docs aligned with real implementation.
- Ensure commands in docs run from repository root.
- Remove stale or legacy wording instead of preserving contradictory notes.

## Pull requests

- Explain what changed and why.
- Include validation commands and outcomes.
- Keep PR scope coherent (avoid unrelated rewrites).
