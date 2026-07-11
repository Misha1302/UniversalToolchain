# Wist2 preview.4 verification record

## Scope and provenance

- Validation date: 2026-07-11.
- Baseline: `Wist2-ea428d9-preview3-release-hardening-20260710.tar.gz`.
- Baseline SHA-256: `badd5cc3b84aaa3f6de9152c29aea61d25142be88af057cc0b3bddb12e462e03`.
- SDK: .NET SDK `10.0.301` from the supplied Linux x64 sidecar.
- Restore mode: offline and serial, using the supplied SDK/feed sidecars and the repository package feed.
- The baseline archive contains no `.git`, so commit, branch, clean status, tag, remote provenance, and history were not independently verified.

## Canonical Release build and tests

The final source state was built serially after removing the unrelated environment variable `PLATFORM=linux/amd64`, which otherwise redirected MSBuild output paths. MSBuild server reuse was disabled for deterministic isolation.

Observed result:

- Release solution build: succeeded.
- Compiler warnings: 0.
- Compiler errors: 0.
- Core tests: 429 passed, 0 failed, 0 skipped.
- Module tests: 287 passed, 0 failed, 0 skipped.
- Dialect/facade tests: 585 passed, 0 failed, 0 skipped.
- Total: 1,301 passed, 0 failed, 0 skipped.

The suite includes focused regressions for public SSA policies and reports, inline dialect composition, immutable option snapshots, external argument slots, exact managed-call bindings, constant-pool lifetime, fail-fast profile conflicts, target capabilities, debug tracing, facade isolation, and real Wist arithmetic folding.

## NuGet package

Produced artifacts:

- `UniversalToolchain.Wist.0.1.0-preview.4.nupkg`
- `UniversalToolchain.Wist.0.1.0-preview.4.snupkg`

Observed package checks:

- Package-surface checker: passed.
- Compile assets: 1 DLL, `ref/net10.0/UniversalToolchain.Wist.dll`.
- Runtime assets: 64 DLLs.
- Forbidden test, benchmark, and manifest-emitter assemblies: absent.
- License expression: Apache-2.0.
- Repository and project URLs: present.
- Package README, changelog, runtime manifests, example dialects, and symbols package: present.
- Package README and changelog are byte-identical to the corresponding source files.
- NuGet SHA-256: `313143a24cc3febb3360ad9c91071d08d123199f6132de33d2af944a237d26e4`.
- Symbols SHA-256: `39d0b300fbd35107dfaa90862db4e7f9f330b553072c3ffcc6a953d57885653b`.

## Clean external consumer

A separate `net10.0` console project used only the produced preview.4 package and a local empty package cache. Restore, Release build, execution, publish, and execution from the published output all succeeded with 0 warnings and 0 errors.

Observed public-facade results:

```text
arithmetic=43
folded=14
division=4
ssa=True
fallback=False
passes=4
trace=9
```

The scenarios prove:

- parameterized Wist arithmetic reaches SSA without fallback;
- constant folding produces an executable result;
- a managed division call survives AIR-to-SSA-to-AIR through its exact execution-scoped binding;
- public route metadata reflects actual execution.

A second consumer intentionally imported `UniversalToolchain.Ssa.Abstractions`. Its build failed with expected compiler error `CS0234`, confirming that low-level SSA assemblies are runtime implementation assets rather than ordinary compile assets of the Wist facade package.

## Documentation

Observed final checks:

- `npm ci --no-audit --no-fund --prefer-offline`: succeeded from the lockfile.
- Documentation status: passed for 142 Markdown files.
- VitePress production build: succeeded.
- Markdown Bash blocks discovered: 50.
- All blocks eligible for execution completed successfully.
- Historical, release-only, and heavyweight benchmark blocks remain explicitly marked `ci-run=false` rather than silently timing out.

VitePress emitted non-blocking warnings that `wist` syntax highlighting falls back to plain text. This is a documentation presentation follow-up, not a build failure.

## Adversarial/static review

- No production SSA/Wist path contains `AppDomain.CurrentDomain`, `Type.GetType`, output-directory scanning, `Assembly.LoadFrom`, or `Assembly.LoadFile` fallback discovery.
- Changed production C# files contain no new `TODO`, `NotImplementedException`, or generic `throw new Exception(...)` placeholders.
- Active first-contact documentation uses preview.4 terminology.
- The unsupported `SsaPreview` optimizer alias is documented only as explicitly unsupported; the runtime alias is `Ssa`.

## Benchmark boundary

The final offline feeds do not contain `BenchmarkDotNet`, `NCalcSync`, or `NCalc.LambdaCompilation`, so the separate benchmark project could not be restored in the final environment. The restore failed with `NU1101` for those three packages before benchmark execution.

This does not affect the canonical solution build/test or the package consumer checks, but numerical performance is not verified. No speedup, throughput, latency, or comparison claim is made by this release record.

## Claims intentionally not made

- SSA is experimental and is not a stable 1.0 contract.
- The backend is not SSA-native; the current route returns to AIR before backend execution.
- Restricted arithmetic is not a hostile-code sandbox.
- Source/parameter budgets do not provide CPU, memory, elapsed-time, filesystem, network, process, or tenant isolation.
- The 64-DLL runtime closure has not been consolidated.
- Production workload behavior, customer demand, and numerical performance have not been established.

## Evidence status

`VERIFIED` for source changes, Release build, the three test suites, package structure, package consumer behavior, facade compile boundary, documentation status/build, and executable Markdown checks.

`PARTIAL` for performance, Git provenance, production workload behavior, and external adoption.

## Clean source archive and patch validation

The deliverable source archive is assembled from a dedicated clean staging tree rather than from build outputs. The staging policy excludes `.git`, `bin`, `obj`, `node_modules`, VitePress output/cache, package artifacts, test results, Python caches, IDE state, and local verification configuration.

Observed clean-unpack properties:

- payload files covered by `MANIFEST.sha256`: 1,445;
- total files including the manifest: 1,446;
- C# project files: 75;
- Markdown files: 142;
- recursive `sha256sum -c MANIFEST.sha256`: passed;
- documentation-status check from the unpacked archive: passed;
- forbidden build/cache directories: none;
- `.mal`, common key/certificate artifacts, IDE user files, and environment files: none;
- absolute `/mnt/data` verification paths: none.

The generated patch was applied to a fresh copy of the preview.3 baseline. The patched tree was then compared recursively with the clean preview.4 staging tree and matched exactly, including the regenerated manifest. Archive and patch SHA-256 values are reported alongside the external deliverables because an artifact cannot contain its own stable hash.
