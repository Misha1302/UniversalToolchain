# Release Checklist

## Required
- master is green
- dotnet restore passed
- dotnet build passed
- dotnet test passed
- package smoke passed
- NuGet package metadata and contents are validated
- docs build passed
- markdown bash checks are optional/non-blocking for this release candidate
- README examples match actual APIs
- site examples match README
- SECURITY limitations are visible
- version updated
- root README first public API example uses CompileFunc, not Evaluate
- NuGet/package README presents fast execution before one-off execution
- performance model doc exists
- preview stability doc exists
- docs distinguish cold path and hot path
- docs do not advertise Evaluate as the performance path
- docs do not claim hardened sandboxing
- security note is visible near compiled/restricted execution docs
- package smoke validates CompileFunc, Evaluate, and Validate
- CLI smoke validates compiler backend, interpreter backend, and restricted dialect rejection
- benchmark dry smoke genuinely executes at least one benchmark or is replaced by a meaningful benchmark contract smoke

## Required commands

```bash ci-run=false
unset PLATFORM
dotnet restore UniversalToolchain/Wist.sln -p:Platform="Any CPU"
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore -p:Platform="Any CPU"
dotnet test UniversalToolchain/Wist.sln -c Release --no-build -p:Platform="Any CPU"
dotnet pack UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj -c Release -o artifacts/packages -p:Platform="Any CPU" /p:WarningsAsErrors=NU5118
ls -la artifacts/packages
unzip -l artifacts/packages/UniversalToolchain.Wist.0.1.0-preview.2.nupkg
npm install --no-audit --no-fund
npm run docs:build
```

## Optional documentation smoke

Markdown bash block execution is useful before publishing docs, but it is not a blocking gate for this release candidate.

```bash ci-run=false
python3 .github/scripts/run-markdown-bash-blocks.py
```

## Package checks
- package metadata uses Apache-2.0 license expression
- package metadata includes repository URL
- package metadata includes project URL
- package includes README.md
- package includes `UniversalToolchain/UniversalToolchain.Wist/CHANGELOG.md` as package-root `CHANGELOG.md`
- package includes runtime manifests
- package includes Wist example dialect content files
- symbols package is produced

## Manual smoke
- run simple formula through WistEngine
- run compiled formula through WistEngine.CompileFunc
- validate a simple formula through WistEngine.Validate
- run CLI compiler backend
- run CLI interpreter backend
- run restricted dialect rejection case
- run benchmark smoke or benchmark contract tests and confirm the command does not report zero executed benchmarks as success
