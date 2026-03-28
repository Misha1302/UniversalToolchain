namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeComponentResolver
{
    RuntimeComponentDescriptor Resolve(RuntimeComponentManifestEntry entry);
}