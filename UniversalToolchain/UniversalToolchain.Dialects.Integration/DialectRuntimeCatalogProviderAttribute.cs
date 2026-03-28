namespace UniversalToolchain.Dialects.Integration;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class DialectRuntimeCatalogProviderAttribute : Attribute
{
    public DialectRuntimeCatalogProviderAttribute(Type providerType)
    {
        ProviderType = providerType ?? throw new ArgumentNullException(nameof(providerType));
    }

    public Type ProviderType { get; }

    public Type GetProviderType() => ProviderType;
}
