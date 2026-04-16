# Project Reference Setup

This repository currently integrates UniversalToolchain from source with project references. There is no package-based
setup documented here because the current public entry point is validated from the in-repo solution.

## Prerequisites

- .NET SDK compatible with `UniversalToolchain/global.json`
- Current repository root as the working directory for the commands below

`UniversalToolchain/global.json` pins the SDK policy to `10.0.103`, allows prerelease SDKs, and rolls forward to the
latest installed major version when needed.

## Add the Wist facade project

For a host application inside this repository, reference the Wist facade project:

```xml
<ItemGroup>
  <ProjectReference Include="..\UniversalToolchain.Dialects.Wist\UniversalToolchain.Dialects.Wist.csproj" />
</ItemGroup>
```

The facade project owns the Wist-specific runtime composition and references the modules it needs. A host should prefer
this single Wist entry point unless it intentionally needs lower-level dialect infrastructure.

## Build from source

```bash
cd UniversalToolchain
dotnet restore Wist.sln -m:1
dotnet build Wist.sln -c Release --no-restore -m:1
```

The root bootstrap scripts run the same source build path:

```bash
./scripts/bootstrap.sh
```

```powershell
./scripts/bootstrap.ps1
```

## Facade profile choice

Use `WistRuntimeFacadeBuilder.CreateDefault()` for normal embedded formula execution. It builds the safe default profile
and excludes unsafe interop from the default composition.

Use `WistRuntimeFacadeBuilder.CreateTrustedDefault()` only when the host explicitly opts into the broader trusted profile
with unsafe interop enabled.

Use `WithDialectFile(path)` when the host needs a concrete `.wistdialect` file instead of either built-in facade profile.
For example, the repository includes `UniversalToolchain/Dialects/examples/wist/pricing-restricted/dialect.wistdialect`
for a narrow pricing formula surface.

Safe composition is not hardened sandboxing. Use process and environment isolation for untrusted code.
