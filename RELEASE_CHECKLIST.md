# Release Checklist

## Required
- master is green
- dotnet restore passed
- dotnet build passed
- dotnet test passed
- package smoke passed
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
dotnet pack UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj -c Release
npm install --no-audit --no-fund
npm run docs:build
python3 .github/scripts/run-markdown-bash-blocks.py
```

## Manual smoke
- run simple formula through WistEngine
- run compiled formula through WistEngine.CompileFunc
- run CLI compiler backend
- run CLI interpreter backend
- run restricted dialect rejection case
