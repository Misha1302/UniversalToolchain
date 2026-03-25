# Contributing

## Prerequisites
- Requires .NET SDK `10.0.103` (`rollForward: latestMajor`, `allowPrerelease: true`).
- Current build and test projects target `net10.0` (.NET 10).

## Build and test
Run from repository root:

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

## Documentation changes
- Treat code as source of truth; verify docs against current implementation.
- Ensure any command/example in docs is runnable from repository root.
- Remove stale historical wording instead of preserving it.
- Do not combine doc sync with unrelated refactors.

## Coding rules
- Follow mandatory rules in `PROJECT_RULES.md`.
- Keep changes focused and deterministic.

## Pull requests
- Include what changed and why.
- Include executed validation commands and outcomes.
- Keep PR scope coherent.
