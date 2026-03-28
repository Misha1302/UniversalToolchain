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

        var registrarsById = CreateRegistrarMap(backendRegistrars);
        _knownBackends = BuildKnownBackends(catalog, registrarsById);
    }

    public IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends() => _knownBackends;

    private static IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> CreateRegistrarMap(
        IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars)
    {
        var map = new SortedDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar>();
        foreach (var registrar in backendRegistrars
                     .Select(x => x.NotNull(nameof(backendRegistrars)))
                     .OrderBy(x => x.BackendId))
        {
            if (!map.TryAdd(registrar.BackendId, registrar))
                Thrower.InvalidOpEx($"Duplicate runtime backend registrar registration for backend '{registrar.BackendId.Value}'.");
        }

        return map;
    }

    private static IReadOnlyList<RuntimeBackendDescriptor> BuildKnownBackends(
        IRuntimeComponentCatalog catalog,
        IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> registrarsById)
    {
        var descriptors = new List<RuntimeBackendDescriptor>();
        foreach (var backendId in registrarsById.Keys)
        {
            if (!catalog.TryResolveBackend(backendId.Value, out var entry) || entry == null)
                Thrower.InvalidOpEx($"Runtime backend registrar '{backendId.Value}' is registered, but no matching runtime backend metadata entry exists.");

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
