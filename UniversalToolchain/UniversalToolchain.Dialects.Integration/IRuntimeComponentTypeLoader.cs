namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeComponentTypeLoader
{
    Type LoadType(RuntimeComponentManifestEntry entry);
}