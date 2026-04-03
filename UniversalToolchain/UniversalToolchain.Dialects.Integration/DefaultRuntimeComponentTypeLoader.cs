using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentTypeLoader : IRuntimeComponentTypeLoader
{
    private readonly IRuntimeComponentResolver _resolver;

    public DefaultRuntimeComponentTypeLoader(IRuntimeComponentResolver resolver)
    {
        if (resolver == null)
            Thrower.ArgumentNull(nameof(resolver));

        _resolver = resolver;
    }


    public Type LoadType(RuntimeComponentManifestEntry entry)
    {
        if (entry == null)
            Thrower.ArgumentNull(nameof(entry));

        return _resolver.Resolve(entry).ActivationType;
    }
}