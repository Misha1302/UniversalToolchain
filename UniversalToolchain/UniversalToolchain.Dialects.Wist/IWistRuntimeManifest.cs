namespace UniversalToolchain.Dialects.Wist;

public interface IWistRuntimeManifest
{
    bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry);
    bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry);
    bool TryResolveBackend(string backendId, out RuntimeComponentManifestEntry? entry);

    IReadOnlyCollection<RuntimeComponentManifestEntry> Modules { get; }
    IReadOnlyCollection<RuntimeComponentManifestEntry> Optimizers { get; }
    IReadOnlyCollection<RuntimeComponentManifestEntry> Backends { get; }

    IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder();
}
