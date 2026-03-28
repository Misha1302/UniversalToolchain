using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

[Obsolete("Use CatalogBackedDialectRuntimeDescriptorProvider.")]
public sealed class WistDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    private readonly CatalogBackedDialectRuntimeDescriptorProvider _inner;

    public WistDialectRuntimeDescriptorProvider(
        IRuntimeComponentCatalog catalog,
        IRuntimeComponentTypeLoader typeLoader,
        IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars)
    {
        _inner = new CatalogBackedDialectRuntimeDescriptorProvider(catalog, typeLoader, backendRegistrars);
    }

    public decimal Order => _inner.Order;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder) => _inner.Register(builder);
}
