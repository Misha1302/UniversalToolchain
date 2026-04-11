using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeKnownBackendsProvider : IRuntimeKnownBackendsProvider
{
    private readonly IRuntimeComponentCatalog _catalog;
    private readonly IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> _providersById;
    private readonly IRuntimeComponentTypeLoader _typeLoader;
    private readonly Lazy<IReadOnlyList<RuntimeBackendDescriptor>> _knownBackends;

    public RuntimeKnownBackendsProvider(
        IRuntimeComponentCatalog catalog,
        IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars,
        IRuntimeComponentTypeLoader typeLoader)
    {
        if (catalog == null)
            Thrower.ArgumentNull(nameof(catalog));

        if (backendRegistrars == null)
            Thrower.ArgumentNull(nameof(backendRegistrars));

        if (typeLoader == null)
            Thrower.ArgumentNull(nameof(typeLoader));

        _catalog = catalog;
        _providersById = CreateProviderMap(backendRegistrars);
        _typeLoader = typeLoader;
        _knownBackends = new Lazy<IReadOnlyList<RuntimeBackendDescriptor>>(BuildKnownBackends);
    }

    public IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends() => _knownBackends.Value;

    private static IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> CreateProviderMap(
        IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars)
    {
        var map = new SortedDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar>();
        foreach (var backendRegistrar in backendRegistrars
                     .Select(x => x.NotNull(nameof(backendRegistrars)))
                     .OrderBy(x => x.BackendId))
        {
            if (!map.TryAdd(backendRegistrar.BackendId, backendRegistrar))
                Thrower.InvalidOpEx($"Duplicate backend provider registration for backend '{backendRegistrar.BackendId.Value}'.");
        }

        return map;
    }

    private IReadOnlyList<RuntimeBackendDescriptor> BuildKnownBackends()
    {
        var descriptors = new List<RuntimeBackendDescriptor>();
        foreach (var backendId in _providersById.Keys)
        {
            if (!_catalog.TryResolveBackend(backendId.Value, out var entry) || entry == null)
                Thrower.InvalidOpEx($"Backend provider '{backendId.Value}' is registered, but no matching runtime backend metadata entry exists.");

            var metadataOwnerType = _typeLoader.LoadType(entry).NotNull();
            var aliases = entry.Aliases
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToArray();

            descriptors.Add(new RuntimeBackendDescriptor(
                new DialectBackendId(entry.CanonicalAlias),
                metadataOwnerType,
                aliases));
        }

        return descriptors
            .OrderBy(x => x.BackendId)
            .ThenBy(x => string.Join("|", x.Aliases), StringComparer.Ordinal)
            .ToList();
    }
}
