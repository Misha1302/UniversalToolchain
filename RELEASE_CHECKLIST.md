# Release Checklist

## Required
- master is green
- dotnet restore passed
- dotnet build passed
- dotnet test passed
- package smoke passed
- NuGet package metadata and contents are validated
- docs build passed
- markdown bash checks passed
- README examples match actual APIs
- site examples match README
- SECURITY limitations are visible
- version updated

## Required commands

```bash ci-run=false
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Wist.sln -c Release --no-build
dotnet pack UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj -c Release -o artifacts/packages /p:WarningsAsErrors=NU5118
ls -la artifacts/packages
unzip -l artifacts/packages/UniversalToolchain.Wist.0.1.0-preview.1.nupkg
npm install --no-audit --no-fund
npm run docs:build
python3 .github/scripts/run-markdown-bash-blocks.py
```

## Package checks
- package metadata uses Apache-2.0 license expression
- package metadata includes repository URL
- package metadata includes project URL
- package includes README.md
- package includes CHANGELOG.md
- package includes runtime manifests
- package includes Wist example dialect content files
- symbols package is produced

## Manual smoke
- run simple formula through WistEngine
- run compiled formula through WistEngine.CompileFunc
- run CLI compiler backend
- run CLI interpreter backend
- run restricted dialect rejection case
