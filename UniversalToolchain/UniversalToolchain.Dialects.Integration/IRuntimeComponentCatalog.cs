namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeComponentCatalog
{
    bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry);

    bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry);

    bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry);

    IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder();

    IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder();

    IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder();
}
