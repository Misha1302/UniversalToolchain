using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeKnownBackendsProvider : IRuntimeKnownBackendsProvider
{
    private readonly IRuntimeComponentCatalog _catalog;
    private readonly Lazy<IReadOnlyList<RuntimeBackendDescriptor>> _knownBackends;
    private readonly IRuntimeComponentTypeLoader _typeLoader;

    public RuntimeKnownBackendsProvider(
        IRuntimeComponentCatalog catalog,
        IRuntimeComponentTypeLoader typeLoader)
    {
        _catalog = catalog.ArgNotNull();
        _typeLoader = typeLoader.ArgNotNull();
        _knownBackends = new Lazy<IReadOnlyList<RuntimeBackendDescriptor>>(BuildKnownBackends);
    }

    public IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends() => _knownBackends.Value;

    private IReadOnlyList<RuntimeBackendDescriptor> BuildKnownBackends()
    {
        var descriptors = new List<RuntimeBackendDescriptor>();
        foreach (var entry in _catalog.GetBackendsInDeterministicOrder())
        {
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
