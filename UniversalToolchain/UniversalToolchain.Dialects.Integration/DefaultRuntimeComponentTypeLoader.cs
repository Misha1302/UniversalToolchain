using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentTypeLoader : IRuntimeComponentTypeLoader
{
    private readonly IRuntimeComponentResolver _resolver;

    public DefaultRuntimeComponentTypeLoader(IRuntimeComponentResolver resolver)
    {
        resolver = resolver.ArgNotNull();

        _resolver = resolver;
    }


    public Type LoadType(RuntimeComponentManifestEntry entry)
    {
        entry = entry.ArgNotNull();

        return _resolver.Resolve(entry).ActivationType;
    }
}