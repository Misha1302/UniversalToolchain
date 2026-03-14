# Contributing

## Prerequisites
- .NET SDK 10.0.x (see `UniversalToolchain/global.json`).

## Build and test
From repository root:

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
```

## Coding rules
- Follow the mandatory project rules in `PROJECT_RULES.md`.
- Keep changes focused, behavior-preserving, and deterministic.

## Pull requests
- Include a concise summary of what changed and why.
- Include validation commands and results.
- Avoid unrelated refactors in the same PR.
