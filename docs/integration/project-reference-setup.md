# Project Reference Setup

The current easiest integration path is a `ProjectReference` from the consumer solution to the Wist dialect facade
project.

```xml
<ItemGroup>
  <ProjectReference Include="UniversalToolchain/UniversalToolchain.Dialects.Wist/UniversalToolchain.Dialects.Wist.csproj" />
</ItemGroup>
```

Adapt the relative path to match the consumer solution layout. The repository currently targets `net10.0`; consumer
projects should use a compatible .NET baseline.

SDK policy for this repository is defined in `UniversalToolchain/global.json`. Check that file when aligning local or CI
SDK installation rules.

There is no documented NuGet package integration path yet.
