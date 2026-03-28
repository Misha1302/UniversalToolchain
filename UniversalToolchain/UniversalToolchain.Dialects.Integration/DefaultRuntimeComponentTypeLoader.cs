using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentTypeLoader : IRuntimeComponentTypeLoader
{
    private readonly IRuntimeComponentResolver _resolver;

    public DefaultRuntimeComponentTypeLoader(IRuntimeComponentResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }


    public Type LoadType(RuntimeComponentManifestEntry entry)
    {
        if (entry == null)
            Thrower.ArgumentNull(nameof(entry));

        return _resolver.Resolve(entry).ActivationType;
    }
}
