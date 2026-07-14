# Wist2 0.1.0-alpha.1 second-cycle verification record

## Environment

- Validation date: 2026-07-14.
- Target environment: Linux x64.
- SDK used: `.NET SDK 10.0.301` from the supplied sidecar.
- Repository SDK policy: `10.0.103`, `rollForward: latestFeature`, `allowPrerelease: true`.
- Package mode: offline local NuGet caches; NuGet audit disabled because network access was unavailable.
- Source baseline: `Wist2-0.1.0-alpha.1-abstraction-fixes-20260714.tar.gz` for the adversarial second cycle.

The supplied "complete" sidecar does not contain every direct package used by the repository,
including `Microsoft.Extensions.DependencyInjection 10.0.2`. Restore therefore also used the
supplied minimal Wist2 package cache.

## Block-by-block verification

### Archive and source baseline

- Input archive SHA-256 checked before extraction.
- Archive paths inspected for traversal and unsafe entries.
- Existing recursive manifest verified before editing.
- Source diff reviewed against the first-cycle archive after implementation.

### AIR, verifier and SSA boundary

- Final conditional fallthrough represented by a synthetic terminal block.
- `AirStackAnalyzer` checks terminal cardinality and return-shape compatibility.
- Typed-null declared types survive interpreter execution and generic resolution.
- Neutral managed-call descriptors pass analysis, simulation, interpreter and CIL execution.
- Execution-scoped provider methods with arguments have interpreter/CIL parity.
- SSA and optimizer projects rebuilt after the shared analysis change.

### Lifecycle, configuration and composition

- Parser/lexer modules remain instance-stateless.
- Lexer snapshot replacement is atomic for malformed input and ownership collisions.
- Prepared-artifact invalidation remains covered.
- Backend runtime caching is isolated per `IServiceProvider` and validate-before-publish remains intact.
- Public `InterpreterState.ValueStack` API compatibility is covered by regression test.

## Build verification

Final command shape:

```bash ci-run=false
dotnet build UniversalToolchain/Wist.sln \
  --no-restore \
  -c Debug \
  -m:1 \
  -p:BuildInParallel=false \
  -p:UseSharedCompilation=false \
  -p:NuGetAudit=false \
  --disable-build-servers
```

Final result:

- **74/74 projects in `Wist.sln` built**;
- the standalone benchmark project outside the solution also built successfully;
- **75/75 repository `.csproj` projects compile**;
- **0 warnings**;
- **0 errors**;
- all **21** manifest-emitting assemblies generated their runtime manifests through the repository's
  `UniversalToolchain.Dialects.ManifestEmitter` path.

## Final test results

The following suites were run sequentially against the final solution build:

| Assembly | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests.dll` | 452 | 0 | 0 |
| `UniversalToolchain.Modules.Tests.dll` | 288 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests.dll` | 585 | 0 | 0 |
| **Total** | **1,325** | **0** | **0** |

Representative command:

```bash ci-run=false
dotnet test <test-project>.csproj \
  --no-build \
  --no-restore \
  -c Debug \
  --logger "console;verbosity=minimal"
```

## Adversarial findings during the second cycle

The repeated review found issues that the first green suite did not prove:

1. typed null lost its declared type inside the interpreter stack;
2. backend layers still contained concrete-descriptor assumptions;
3. execution-scoped CIL calls did not support parameters;
4. final conditional fallthrough was absent from the CFG;
5. structural stack analysis did not own terminal-shape validation;
6. lexer loader uniqueness rules were weaker than the live configuration rules;
7. lazy runtime cache leaked across DI containers;
8. an intermediate fix accidentally changed the public `ValueStack` property type.

Each item was reproduced or protected by a focused regression before final full-suite execution.

## Final artifact checks

Before handoff, the release process performs:

- whitespace/error-marker review of changed source files;
- removal of `bin`, `obj`, Git metadata, caches, logs and temporary files;
- rejection of `CHANGELOG.md`, secret-like and runtime/build artifacts;
- recursive regeneration of `MANIFEST.sha256` after cleanup;
- deterministic archive creation;
- clean extraction into a new directory;
- verification of every recursive manifest entry and archive SHA-256.

## Evidence boundary

Verified here: requested source corrections, final Debug solution build, all three repository test
assemblies, generated runtime manifests and final source-archive integrity.

Not revalidated in this cycle: NuGet pack/package surface, external consumer restore/publish smoke,
documentation-site build, benchmark performance and production or hostile-code workloads.
