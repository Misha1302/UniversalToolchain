using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Immutable runtime descriptor registry used for deterministic dialect resolution.
/// </summary>
public sealed class DialectRuntimeDescriptorRegistry
{
    private readonly ReadOnlyDictionary<DialectBackendTarget, RuntimeBackendDescriptor> _backends;
    private readonly ReadOnlyDictionary<(string Name, DialectBackendTarget Target), RuntimeIntrinsicDescriptor> _intrinsics;
    private readonly ReadOnlyDictionary<string, RuntimeModuleDescriptor> _modules;
    private readonly ReadOnlyDictionary<string, RuntimeOptimizerDescriptor> _optimizers;

    public DialectRuntimeDescriptorRegistry(
        IDictionary<string, RuntimeModuleDescriptor> modules,
        IDictionary<string, RuntimeOptimizerDescriptor> optimizers,
        IDictionary<DialectBackendTarget, RuntimeBackendDescriptor> backends,
        IDictionary<(string Name, DialectBackendTarget Target), RuntimeIntrinsicDescriptor> intrinsics)
    {
        if (modules == null)
            Thrower.ArgumentNull(nameof(modules));

        if (optimizers == null)
            Thrower.ArgumentNull(nameof(optimizers));

        if (backends == null)
            Thrower.ArgumentNull(nameof(backends));

        if (intrinsics == null)
            Thrower.ArgumentNull(nameof(intrinsics));

        _modules = new ReadOnlyDictionary<string, RuntimeModuleDescriptor>(new Dictionary<string, RuntimeModuleDescriptor>(modules, StringComparer.Ordinal));
        _optimizers = new ReadOnlyDictionary<string, RuntimeOptimizerDescriptor>(new Dictionary<string, RuntimeOptimizerDescriptor>(optimizers, StringComparer.Ordinal));
        _backends = new ReadOnlyDictionary<DialectBackendTarget, RuntimeBackendDescriptor>(new Dictionary<DialectBackendTarget, RuntimeBackendDescriptor>(backends));
        _intrinsics = new ReadOnlyDictionary<(string Name, DialectBackendTarget Target), RuntimeIntrinsicDescriptor>(new Dictionary<(string Name, DialectBackendTarget Target), RuntimeIntrinsicDescriptor>(intrinsics));
    }

    public IReadOnlyDictionary<string, RuntimeModuleDescriptor> Modules => _modules;

    public IReadOnlyDictionary<string, RuntimeOptimizerDescriptor> Optimizers => _optimizers;

    public IReadOnlyDictionary<DialectBackendTarget, RuntimeBackendDescriptor> Backends => _backends;

    public IReadOnlyDictionary<(string Name, DialectBackendTarget Target), RuntimeIntrinsicDescriptor> Intrinsics => _intrinsics;
}