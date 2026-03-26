using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

public sealed class DialectRuntimeCatalog : IDialectRuntimeCatalog
{
    private readonly IReadOnlyDictionary<string, DialectRuntimeBackendDescriptor> _backendMap;
    private readonly IReadOnlyDictionary<string, DialectRuntimeModuleDescriptor> _moduleMap;
    private readonly IReadOnlyDictionary<string, DialectRuntimeOptimizerDescriptor> _optimizerMap;

    public DialectRuntimeCatalog(
        IReadOnlyDictionary<string, DialectRuntimeModuleDescriptor> moduleMap,
        IReadOnlyDictionary<string, DialectRuntimeOptimizerDescriptor> optimizerMap,
        IReadOnlyDictionary<string, DialectRuntimeBackendDescriptor> backendMap,
        IReadOnlyCollection<DialectRuntimeModuleDescriptor> modules,
        IReadOnlyCollection<DialectRuntimeOptimizerDescriptor> optimizers,
        IReadOnlyCollection<DialectRuntimeBackendDescriptor> backends)
    {
        if (moduleMap == null)
            Thrower.ArgumentNull(nameof(moduleMap));

        _moduleMap = moduleMap;
        if (optimizerMap == null)
            Thrower.ArgumentNull(nameof(optimizerMap));

        _optimizerMap = optimizerMap;
        if (backendMap == null)
            Thrower.ArgumentNull(nameof(backendMap));

        _backendMap = backendMap;
        if (modules == null)
            Thrower.ArgumentNull(nameof(modules));

        Modules = modules;
        if (optimizers == null)
            Thrower.ArgumentNull(nameof(optimizers));

        Optimizers = optimizers;
        if (backends == null)
            Thrower.ArgumentNull(nameof(backends));

        Backends = backends;
    }

    public IReadOnlyCollection<DialectRuntimeModuleDescriptor> Modules { get; }

    public IReadOnlyCollection<DialectRuntimeOptimizerDescriptor> Optimizers { get; }

    public IReadOnlyCollection<DialectRuntimeBackendDescriptor> Backends { get; }

    public bool TryResolveModule(string alias, out DialectRuntimeModuleDescriptor? descriptor) => _moduleMap.TryGetValue(alias, out descriptor);

    public bool TryResolveOptimizer(string alias, out DialectRuntimeOptimizerDescriptor? descriptor) => _optimizerMap.TryGetValue(alias, out descriptor);

    public bool TryResolveBackend(DialectBackendId id, out DialectRuntimeBackendDescriptor? descriptor) => _backendMap.TryGetValue(id.Value, out descriptor);
}
