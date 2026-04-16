# ProjectReference setup

The current easiest path for embedding UniversalToolchain/Wist in another .NET application is a project reference. The
public package story is not the primary integration path yet, so this is the recommended current method for consumer
solutions.

## Consumer project reference

Add a project reference from your consumer `.csproj` to the Wist facade project:

```xml
<ItemGroup>
  <ProjectReference Include="..\Wist2\UniversalToolchain\UniversalToolchain.Dialects.Wist\UniversalToolchain.Dialects.Wist.csproj" />
</ItemGroup>
```

Adjust the relative path to match where this repository lives next to your consumer solution.

## SDK and target framework

The repository currently targets `net10.0`. The consumer project should be compatible with that baseline.

Check `UniversalToolchain/global.json` for the current SDK expectation before restoring or building a consumer solution
that references this repository.

## Next step

After the project reference is in place, create a small host through the Wist facade. See
[Minimal facade host](minimal-facade-host.md).
