# Changelog

## 0.1.0-preview.4

- added facade-owned `WistOptimizationOptions` and `WistSsaOptions` with explicit `Disabled`, `Prefer`, `Require`, and `Debug` policies;
- added observable SSA reports to validation, non-throwing compilation, and compiled-program metadata without exposing low-level SSA assemblies;
- added inline dialect text and the shipped `ssa-preview` example for first-contact integrations;
- made `Debug`, detailed diagnostics, target capabilities, extension-pack conflicts, and optimizer-pass conflicts operational rather than declarative no-ops;
- preserved exact execution-scoped managed members across `AIR -> SSA -> AIR`, removed production AppDomain fallback resolution, and made repeated equivalent bindings deterministic;
- projected Wist `int32` add/subtract/multiply operations to canonical SSA callables and preserved external parameter slots and constant-pool ownership;
- added stable public SSA failure diagnostics (`UTC-WIST-SSA-001`) and prevented unexpected optimizer defects from becoming silent `Prefer` fallback;
- added a reviewed `PublicAPI.Shipped.txt` facade baseline and a regression test that fails on accidental exported API changes;
- restricted normal consumer compile assets to the facade reference assembly under `ref/`, while retaining the complete 64-assembly runtime closure under `lib/` for build, run, and publish;
- expanded end-to-end and adversarial coverage for public SSA configuration, reports, managed calls, profile conflicts, and route failures.

## 0.1.0-preview.3

- made CLR interop fail closed: only shipped `BasicStdLib` plus host-supplied `AllowedAssemblies` are visible; dialect/runtime implementation assemblies are not implicit interop surfaces.
- replaced process-wide CLR discovery with an immutable, host-supplied assembly/type catalog.
- added deterministic ambiguity handling for type and method resolution.
- removed process-wide dynamic-method lifetime retention.
- added structured validation/compilation diagnostics and host-owned source/parameter preflight limits.
- deprecated misleading `CreateSafeFormulas`, `CreateBusinessRules`, `CreateTrusted`, and `CompileFunc` compatibility APIs.
- added canonical reproducible build wrappers and package-surface growth checks.
- made executable Markdown validation bounded and deterministic by running already-built Release assemblies directly and terminating whole process groups on timeout.

## 0.1.0-preview.2

Public facade release candidate for typed delegate compilation.

### Added

- Architecture/runtime provider policy guardrails for the preview.2 release candidate: provider allowlists are composition-owned, reusable modules no longer depend on Wist-owned facts, and auxiliary backend pipeline components are excluded from module-contract selection.
- Regression coverage for the `backend.runtime.provider.policy` contract-selection bug found after the v3 architecture-hardcode pass.
- `WistEngine.Compile<TDelegate>` for typed delegate-backed formula compilation without exposing backend-specific
  artifacts.
- `WistEngine.TryCompile<TDelegate>` for non-throwing typed compilation.
- `WistProgram<TDelegate>` and `WistProgramMetadata` for backend-neutral compiled program metadata.
- Public facade regression tests for delegate compilation, repeated invocation, invalid source, void delegates, and
  duplicate parameter names.

### Fixed

- Restored legacy-compatible `call C#` emission shape while keeping production optimizer/runtime planning on typed/legacy readers instead of raw display-string comparisons.

### Notes

- `CompileFunc` remains available as a compatibility convenience for one, two, and three arguments.
- The public facade still treats compiled execution as a performance feature, not as a sandbox boundary.

## 0.1.0-preview.1

Initial preview release of `UniversalToolchain.Wist`: a compiler-first Wist facade for .NET formula/rule execution, with
typed CIL-backed compiled functions for hot paths and interpreter support for diagnostics and parity.

### Added

- `WistEngine` facade for application-level Wist formula execution.
- `Evaluate<T>` for convenient one-off formula execution.
- Non-throwing validation API for checking formula shape before execution.
- Typed `CompileFunc` overloads for one, two, and three arguments as the primary hot-path API.
- Safe formulas, business rules, and trusted presets.
- NuGet packaging for the Wist facade, runtime manifests, and example dialect files.

### Notes

- This is a preview API.
- API shape may change before the first stable release.
- Third-party DSL authoring APIs and backend authoring contracts are evolving.
- `Evaluate<T>` is the convenience one-off path, not the hot-path performance API.
- Restricted presets limit selected language/runtime composition, but they are not hardened sandboxes for arbitrary untrusted code.
- Current platform baseline is .NET 10.
