using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

[Obsolete("Use RuntimeKnownBackendsProvider from UniversalToolchain.Dialects.Integration.")]
public sealed class WistKnownBackendsProvider : IWistKnownBackendsProvider
{
    private readonly RuntimeKnownBackendsProvider _inner;

    public WistKnownBackendsProvider(IRuntimeComponentCatalog catalog, IEnumerable<IWistDialectBackendServiceProvider> backendProviders)
    {
        _inner = new RuntimeKnownBackendsProvider(catalog, backendProviders.Cast<IDialectBackendRuntimeRegistrar>());
    }

    public IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends() => _inner.GetKnownBackends();
}
