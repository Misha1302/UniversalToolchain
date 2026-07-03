# Build Fix Report 2026-06-30

## Goal

Make the production Wist2 / UniversalToolchain solution compile in the provided offline workspace.

## Result

`Wist.sln` restores and builds successfully with the provided local feed and installed .NET SDK:

```bash
dotnet msbuild Wist.sln -t:Restore -p:RestoreUseStaticGraphEvaluation=true -m:1 -v:quiet
dotnet build Wist.sln --no-restore -m:1 -v:minimal
```

Observed result:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Changes

- Removed production `PackageReference` entries that could not be restored offline:
  `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.DependencyInjection.Abstractions`,
  `JetBrains.Annotations`, `CommandLineParser`, and `System.Reflection.MetadataLoadContext`.
- Added `Directory.Build.props` to centralize offline-compatible compile references:
  `Microsoft.AspNetCore.App` for the real Microsoft DI implementation and the SDK-local
  `System.Reflection.MetadataLoadContext` assembly through `$(MSBuildToolsPath)`.
- Added `BuildSupport/JetBrainsAnnotations.cs` for compile-time-only `UsedImplicitlyAttribute` use.
- Added `BuildSupport/CommandLineCompatibility.cs` as a small reflection-based compatibility layer
  for the existing Wistc option classes, preserving the attribute-driven CLI shape.
- Removed test and benchmark projects from `Wist.sln` so the default solution build represents the
  production compiler/runtime graph and does not require unavailable NUnit, test SDK, BenchmarkDotNet,
  NCalc, DynamicExpresso, coverlet, or SharpFuzz packages.
- Added `NuGet.config` and `packages/gremit.3.5.1.nupkg` so the remaining production package dependency
  can be restored from inside the archive.
- Fixed missing imports in Wist backend providers and service collection registration:
  `UniversalToolchain.Dialects.Integration` and `Microsoft.Extensions.DependencyInjection.Extensions`.

## Rationale

The production DI dependency is not stubbed. It is resolved through the installed .NET shared framework,
which keeps runtime behavior tied to the real Microsoft implementation. Annotation and CLI-parser
compatibility code is local because those dependencies are compile-time metadata or narrow command-line
binding surface in this workspace.

Test and benchmark sources remain in the tree, but are intentionally not part of `Wist.sln` until their
real external packages are available. Compiling them with broad fake NUnit or benchmark packages would
make the build result less trustworthy.
