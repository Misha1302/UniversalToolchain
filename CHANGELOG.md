# Changelog

## 0.1.0-preview.1

Initial preview release of `UniversalToolchain.Wist`.

### Added

- `WistEngine` facade for application-level Wist formula execution.
- `Evaluate<T>` for convenient one-off formula execution.
- Non-throwing validation API for checking formula shape before execution.
- Typed `CompileFunc` overloads for one, two, and three arguments.
- Safe formulas, business rules, and trusted presets.
- NuGet packaging for the Wist facade, runtime manifests, and example dialect files.

### Notes

- This is a preview API.
- API shape may change before the first stable release.
- Restricted presets limit selected language/runtime composition, but they are not hardened sandboxes for arbitrary untrusted code.
- Current platform baseline is .NET 10.
