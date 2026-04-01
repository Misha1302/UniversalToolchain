using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class DialectRuntimeCatalogProviderAttribute : Attribute
{
    public DialectRuntimeCatalogProviderAttribute(Type providerType)
    {
        if (providerType == null)
            Thrower.ArgumentNull(nameof(providerType));

        ProviderType = providerType;
    }

    public Type ProviderType { get; }

    public Type GetProviderType() => ProviderType;
}
