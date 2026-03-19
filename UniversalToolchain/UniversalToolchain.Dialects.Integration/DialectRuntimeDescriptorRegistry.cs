using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Immutable runtime descriptor registry used for deterministic dialect resolution.
/// </summary>
public sealed class DialectRuntimeDescriptorRegistry
{
    private readonly ReadOnlyDictionary<string, RuntimeBackendDescriptor> _backendNameMap;
    private readonly ReadOnlyDictionary<DialectBackendId, RuntimeBackendDescriptor> _backends;
    private readonly ReadOnlyDictionary<string, string> _intrinsicCanonicalNames;
    private readonly ReadOnlyDictionary<(string CanonicalId, DialectBackendSelector Target), RuntimeIntrinsicDescriptor> _intrinsics;
    private readonly ReadOnlyDictionary<string, RuntimeModuleDescriptor> _moduleNameMap;
    private readonly ReadOnlyDictionary<string, RuntimeModuleDescriptor> _modules;
    private readonly ReadOnlyDictionary<string, RuntimeOptimizerDescriptor> _optimizerNameMap;
    private readonly ReadOnlyDictionary<string, RuntimeOptimizerDescriptor> _optimizers;

    public DialectRuntimeDescriptorRegistry(
        IDictionary<string, RuntimeModuleDescriptor> modules,
        IDictionary<string, RuntimeModuleDescriptor> moduleNameMap,
        IDictionary<string, RuntimeOptimizerDescriptor> optimizers,
        IDictionary<string, RuntimeOptimizerDescriptor> optimizerNameMap,
        IDictionary<DialectBackendId, RuntimeBackendDescriptor> backends,
        IDictionary<string, RuntimeBackendDescriptor> backendNameMap,
        IDictionary<(string CanonicalId, DialectBackendSelector Target), RuntimeIntrinsicDescriptor> intrinsics,
        IDictionary<string, string> intrinsicCanonicalNames)
    {
        if (modules == null)
            Thrower.ArgumentNull(nameof(modules));

        if (moduleNameMap == null)
            Thrower.ArgumentNull(nameof(moduleNameMap));

        if (optimizers == null)
            Thrower.ArgumentNull(nameof(optimizers));

        if (optimizerNameMap == null)
            Thrower.ArgumentNull(nameof(optimizerNameMap));

        if (backends == null)
            Thrower.ArgumentNull(nameof(backends));

        if (backendNameMap == null)
            Thrower.ArgumentNull(nameof(backendNameMap));

        if (intrinsics == null)
            Thrower.ArgumentNull(nameof(intrinsics));

        if (intrinsicCanonicalNames == null)
            Thrower.ArgumentNull(nameof(intrinsicCanonicalNames));

        _modules = new ReadOnlyDictionary<string, RuntimeModuleDescriptor>(new Dictionary<string, RuntimeModuleDescriptor>(modules, StringComparer.Ordinal));
        _moduleNameMap = new ReadOnlyDictionary<string, RuntimeModuleDescriptor>(new Dictionary<string, RuntimeModuleDescriptor>(moduleNameMap, StringComparer.Ordinal));
        _optimizers = new ReadOnlyDictionary<string, RuntimeOptimizerDescriptor>(new Dictionary<string, RuntimeOptimizerDescriptor>(optimizers, StringComparer.Ordinal));
        _optimizerNameMap = new ReadOnlyDictionary<string, RuntimeOptimizerDescriptor>(new Dictionary<string, RuntimeOptimizerDescriptor>(optimizerNameMap, StringComparer.Ordinal));
        _backends = new ReadOnlyDictionary<DialectBackendId, RuntimeBackendDescriptor>(new Dictionary<DialectBackendId, RuntimeBackendDescriptor>(backends));
        _backendNameMap = new ReadOnlyDictionary<string, RuntimeBackendDescriptor>(new Dictionary<string, RuntimeBackendDescriptor>(backendNameMap, StringComparer.Ordinal));
        _intrinsics = new ReadOnlyDictionary<(string CanonicalId, DialectBackendSelector Target), RuntimeIntrinsicDescriptor>(new Dictionary<(string CanonicalId, DialectBackendSelector Target), RuntimeIntrinsicDescriptor>(intrinsics));
        _intrinsicCanonicalNames = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(intrinsicCanonicalNames, StringComparer.Ordinal));
    }

    public IReadOnlyDictionary<string, RuntimeModuleDescriptor> Modules => _modules;

    public IReadOnlyDictionary<string, RuntimeOptimizerDescriptor> Optimizers => _optimizers;

    public IReadOnlyDictionary<DialectBackendId, RuntimeBackendDescriptor> Backends => _backends;

    public IReadOnlyDictionary<(string CanonicalId, DialectBackendSelector Target), RuntimeIntrinsicDescriptor> Intrinsics => _intrinsics;

    public bool TryResolveModule(string nameOrAlias, out RuntimeModuleDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
            Thrower.Argument(nameof(nameOrAlias), "Module lookup name must not be empty.");

        return _moduleNameMap.TryGetValue(nameOrAlias, out descriptor!);
    }

    public bool TryResolveOptimizer(string nameOrAlias, out RuntimeOptimizerDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
            Thrower.Argument(nameof(nameOrAlias), "Optimizer lookup name must not be empty.");

        return _optimizerNameMap.TryGetValue(nameOrAlias, out descriptor!);
    }

    public bool TryResolveBackend(DialectBackendId backendId, out RuntimeBackendDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(backendId.Value))
            Thrower.Argument(nameof(backendId), "Backend lookup identifier must not be empty.");

        return _backendNameMap.TryGetValue(backendId.Value, out descriptor!);
    }

    public bool TryResolveIntrinsicCanonicalId(string nameOrAlias, out string canonicalId)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
            Thrower.Argument(nameof(nameOrAlias), "Intrinsic lookup name must not be empty.");

        return _intrinsicCanonicalNames.TryGetValue(nameOrAlias, out canonicalId!);
    }

    public IReadOnlyList<RuntimeIntrinsicDescriptor> GetIntrinsicDescriptors(string nameOrAlias)
    {
        if (!TryResolveIntrinsicCanonicalId(nameOrAlias, out var canonicalId))
            return [];

        return _intrinsics
            .Where(x => x.Key.CanonicalId == canonicalId)
            .OrderBy(x => x.Key.Target)
            .Select(x => x.Value)
            .ToList();
    }
}
