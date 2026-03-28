using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeKnownBackendsProvider : IRuntimeKnownBackendsProvider
{
    private readonly IReadOnlyList<RuntimeBackendDescriptor> _knownBackends;

    public RuntimeKnownBackendsProvider(
        IRuntimeComponentCatalog catalog,
        IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars)
    {
        if (catalog == null)
            Thrower.ArgumentNull(nameof(catalog));

        if (backendRegistrars == null)
            Thrower.ArgumentNull(nameof(backendRegistrars));

        var providersById = CreateProviderMap(backendRegistrars);
        _knownBackends = BuildKnownBackends(catalog, providersById);
    }

    public IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends() => _knownBackends;

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

    private static IReadOnlyList<RuntimeBackendDescriptor> BuildKnownBackends(
        IRuntimeComponentCatalog catalog,
        IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> providersById)
    {
        var descriptors = new List<RuntimeBackendDescriptor>();
        foreach (var backendId in providersById.Keys)
        {
            if (!catalog.TryResolveBackend(backendId.Value, out var entry) || entry == null)
                Thrower.InvalidOpEx($"Backend provider '{backendId.Value}' is registered, but no matching runtime backend metadata entry exists.");

            var aliases = entry.Aliases
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToArray();

            descriptors.Add(new RuntimeBackendDescriptor(
                new DialectBackendId(entry.CanonicalAlias),
                typeof(RuntimeComponentManifestEntry),
                aliases));
        }

        return descriptors
            .OrderBy(x => x.BackendId)
            .ThenBy(x => string.Join("|", x.Aliases), StringComparer.Ordinal)
            .ToList();
    }
}