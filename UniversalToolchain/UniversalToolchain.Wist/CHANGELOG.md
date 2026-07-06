# Changelog

## 0.1.0-preview.2

Public facade release candidate for typed delegate compilation.

### Added

- `WistEngine.Compile<TDelegate>` for typed delegate-backed formula compilation without exposing backend-specific
  artifacts.
- `WistEngine.TryCompile<TDelegate>` for non-throwing typed compilation.
- `WistProgram<TDelegate>` and `WistProgramMetadata` for backend-neutral compiled program metadata.
- Public facade regression tests for delegate compilation, repeated invocation, invalid source, void delegates, and
  duplicate parameter names.

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
