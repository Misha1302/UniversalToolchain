using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistKnownBackendsProvider : IWistKnownBackendsProvider
{
    private readonly IReadOnlyList<RuntimeBackendDescriptor> _knownBackends;

    public WistKnownBackendsProvider(
        IRuntimeComponentCatalog catalog,
        IEnumerable<IWistDialectBackendServiceProvider> backendProviders)
    {
        if (catalog == null)
            Thrower.ArgumentNull(nameof(catalog));

        if (backendProviders == null)
            Thrower.ArgumentNull(nameof(backendProviders));

        var providersById = CreateProviderMap(backendProviders);
        _knownBackends = BuildKnownBackends(catalog, providersById);
    }

    public IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends() => _knownBackends;

    private static IReadOnlyDictionary<DialectBackendId, IWistDialectBackendServiceProvider> CreateProviderMap(
        IEnumerable<IWistDialectBackendServiceProvider> backendProviders)
    {
        var map = new SortedDictionary<DialectBackendId, IWistDialectBackendServiceProvider>();
        foreach (var backendProvider in backendProviders
                     .Select(x => x.NotNull(nameof(backendProviders)))
                     .OrderBy(x => x.BackendId))
        {
            if (!map.TryAdd(backendProvider.BackendId, backendProvider))
                Thrower.InvalidOpEx($"Duplicate Wist backend provider registration for backend '{backendProvider.BackendId.Value}'.");
        }

        return map;
    }

    private static IReadOnlyList<RuntimeBackendDescriptor> BuildKnownBackends(
        IRuntimeComponentCatalog catalog,
        IReadOnlyDictionary<DialectBackendId, IWistDialectBackendServiceProvider> providersById)
    {
        var descriptors = new List<RuntimeBackendDescriptor>();
        foreach (var backendId in providersById.Keys)
        {
            if (!catalog.TryResolveBackend(backendId.Value, out var entry) || entry == null)
                Thrower.InvalidOpEx($"Wist backend provider '{backendId.Value}' is registered, but no matching runtime backend metadata entry exists.");

            var aliases = entry.Aliases
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToArray();

            descriptors.Add(new RuntimeBackendDescriptor(new DialectBackendId(entry.CanonicalAlias), aliases));
        }

        return descriptors
            .OrderBy(x => x.BackendId)
            .ThenBy(x => string.Join("|", x.Aliases), StringComparer.Ordinal)
            .ToList();
    }
}
