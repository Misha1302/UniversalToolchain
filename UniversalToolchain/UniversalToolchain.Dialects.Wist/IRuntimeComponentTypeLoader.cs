namespace UniversalToolchain.Dialects.Wist;

public interface IRuntimeComponentTypeLoader
{
    Type LoadType(RuntimeComponentManifestEntry entry);
}
