# Release Checklist

## Required
- master is green
- dotnet restore passed
- dotnet build passed
- dotnet test passed
- package smoke passed
- NuGet package metadata and contents are validated
- docs build passed
- markdown bash checks passed, with historical/heavy examples explicitly marked `ci-run=false`
- README examples match actual APIs
- site examples match README
- SECURITY limitations are visible
- version updated
- root README first public API example uses `Compile<TDelegate>`, not `Evaluate`
- `CompileFunc` is documented only as a compatibility convenience for small examples
- NuGet/package README presents fast execution before one-off execution
- performance model doc exists
- preview stability doc exists
- docs distinguish cold path and hot path
- docs do not advertise Evaluate as the performance path
- docs do not claim hardened sandboxing
- security note is visible near compiled/restricted execution docs
- package smoke validates `Compile<TDelegate>`, `CompileFunc`, `Evaluate`, and `Validate`
- CLI smoke validates compiler backend, interpreter backend, and restricted dialect rejection
- benchmark dry smoke genuinely executes at least one benchmark or is replaced by a meaningful benchmark contract smoke

## Evidence log

Fill this before tagging/publishing. Do not mark a gate complete without the observed command/status.

| Gate | Required evidence | Status / observed result |
|---|---|---|
| Canonical build/test | `./build.sh --skip-docs --skip-pack` | PASS — Release build, 0 warnings/errors |
| Restore/build details | serial restore/build performed by `build.sh` | PASS — offline serial restore and `-m:1` build |
| Release tests | three explicit test projects executed by `build.sh` | PASS — 429 + 287 + 585 = 1,301 |
| Package | `dotnet pack ...UniversalToolchain.Wist.csproj -c Release` and package-surface check | PASS — 1 ref facade DLL, 64 runtime DLLs |
| Consumer smoke | clean external `net10.0` app consuming the local package | PASS — restore/build/run/publish/published run |
| CLI smoke | compiler, interpreter, restricted rejection | PASS — covered by canonical dialect/CLI regression suite |
| Docs status | `python3 Tools/check_documentation_status.py` | PASS — 142 Markdown files |
| Markdown smoke | `python3 .github/scripts/run-markdown-bash-blocks.py` | PASS — 20 executed, 32 explicit skips |
| Docs build | `npm run docs:build` | PASS — VitePress production build |
| Benchmark self-test | restore/build `UniversalToolchain.Benchmarks` and run `--self-test` | BLOCKED IN FINAL OFFLINE ENVIRONMENT — BenchmarkDotNet/NCalc packages absent from supplied feeds; no performance claim |

## Required commands

```bash ci-run=false
./build.sh
ls -la artifacts/packages
unzip -l artifacts/packages/UniversalToolchain.Wist.0.1.0-preview.4.nupkg
```

## Documentation smoke

Executable Markdown blocks are a blocking release gate inside `build.sh`. Historical or heavyweight benchmark examples stay visible with `ci-run=false` and must be covered by their dedicated/manual gate rather than silently timing out the documentation runner.

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
- run compiled formula through `WistEngine.Compile<TDelegate>`
- run compatibility compiled formula through `WistEngine.CompileFunc`
- validate a simple formula through WistEngine.Validate
- run CLI compiler backend
- run CLI interpreter backend
- run restricted dialect rejection case
- run benchmark smoke or benchmark contract tests and confirm the command does not report zero executed benchmarks as success
