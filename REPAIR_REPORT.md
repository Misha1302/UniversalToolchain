# Wist2 preview.4 SSA public-API hardening report

## Scope

This repair turns the existing SSA preview from an internal dialect-only route into an observable, opt-in public Wist feature while preserving the fail-closed discovery, structured diagnostics, resource limits, and package boundaries established in preview.3.

The SSA layer remains experimental. This report does not claim an SSA-native backend, sandboxing, production workload validation, or a measured performance advantage.

## Public facade

- Added `WistEngineOptions.Optimization.Ssa` with explicit `Disabled`, `Prefer`, `Require`, and `Debug` policies.
- Added `Summary` and `Detailed` SSA diagnostic levels.
- Added immutable public optimization reports to validation and compilation results. Reports expose actual route use, fallback, profile, executed passes, input/output AIR instruction counts, diagnostics, and debug trace.
- Added inline dialect sources through `WistDialectSource.FromText` and `WistEngineOptions.FromDialectText`.
- Added the shipped `ssa-preview` Wist dialect example.
- Added a public API baseline for the supported `UniversalToolchain.Wist` facade.

## SSA execution contract

- Implemented real behavior for debug traces, diagnostic detail, and target-capability requirements instead of retaining no-op settings.
- Made conflicting callable descriptors, lowering targets, extension packs, and optimizer pass identifiers fail fast.
- Carries exact immutable `MethodInfo`/`ConstructorInfo` bindings through AIR-to-SSA, optimization, and SSA-to-AIR. Production emission no longer rediscovers managed calls through `Type.GetType`, `AppDomain`, assembly scanning, or the filesystem.
- Added stable diagnostics for missing managed-call bindings and unsupported external slots.
- Added Wist-specific projections from native `int32` add/subtract/multiply calls to canonical SSA callables, enabling real constant folding rather than merely round-tripping through SSA.
- Preserved exact execution-scoped managed bindings for operations that do not yet have canonical SSA semantics, including division and unary negation.
- Fixed external parameter-slot handling and constant-pool ownership so the compiled delegate remains executable after SSA rewrites.
- Prevented host callbacks from silently widening the CLR allowlist or replacing the canonical type catalog.

## Package boundary

Preview.4 still ships 64 runtime DLLs because the current runtime is physically modular. The ordinary package consumer compiles only against:

```text
ref/net10.0/UniversalToolchain.Wist.dll
```

A negative consumer build verifies that low-level `UniversalToolchain.Ssa.*` namespaces are not accidentally available as compile assets. Runtime assembly consolidation remains a separate compatibility-sensitive task.

## Verification summary

- Release build: 0 warnings, 0 errors.
- Core tests: 429 passed.
- Module tests: 287 passed.
- Dialect/facade tests: 585 passed.
- Total: 1,301 passed, 0 failed, 0 skipped.
- Package surface: 1 compile facade DLL, 64 runtime DLLs.
- Clean consumer: parameter arithmetic `43`, folded expression `14`, division `4`, SSA used, no fallback, 4 passes, 9 trace entries.
- Documentation status: 142 Markdown files.
- VitePress production build: passed.
- Executable Markdown blocks: all runnable blocks passed; heavyweight/historical blocks remain explicitly excluded.

See `VERIFICATION.md` for evidence boundaries, commands, hashes, and remaining gaps.

## Deliverable integrity

The final source tree is delivered with a recursive manifest and a separately verified patch from the preview.3 release-hardening baseline. Both are validated from fresh copies; generated build/cache content and local verification paths are excluded.
