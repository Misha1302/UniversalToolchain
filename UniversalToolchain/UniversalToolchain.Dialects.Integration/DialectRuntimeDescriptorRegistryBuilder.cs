using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Explicit builder for runtime descriptor registration used by dialect resolution.
/// </summary>
public sealed class DialectRuntimeDescriptorRegistryBuilder
{
    private readonly Dictionary<string, RuntimeModuleDescriptor> _modules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RuntimeOptimizerDescriptor> _optimizers = new(StringComparer.Ordinal);
    private readonly Dictionary<DialectBackendTarget, RuntimeBackendDescriptor> _backends = [];
    private readonly Dictionary<(string Name, DialectBackendTarget Target), RuntimeIntrinsicDescriptor> _intrinsics = [];

    public DialectRuntimeDescriptorRegistryBuilder RegisterModule(RuntimeModuleDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        if (_modules.ContainsKey(descriptor.Name))
            Thrower.Argument(nameof(descriptor), $"Module descriptor '{descriptor.Name}' is already registered.");

        _modules.Add(descriptor.Name, descriptor);
        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterOptimizer(RuntimeOptimizerDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        if (_optimizers.ContainsKey(descriptor.Name))
            Thrower.Argument(nameof(descriptor), $"Optimizer descriptor '{descriptor.Name}' is already registered.");

        _optimizers.Add(descriptor.Name, descriptor);
        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterBackend(RuntimeBackendDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        if (_backends.ContainsKey(descriptor.BackendTarget))
            Thrower.Argument(nameof(descriptor), $"Backend descriptor for '{descriptor.BackendTarget}' is already registered.");

        _backends.Add(descriptor.BackendTarget, descriptor);
        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterIntrinsic(RuntimeIntrinsicDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        var key = (descriptor.Name, descriptor.Target);
        if (_intrinsics.ContainsKey(key))
            Thrower.Argument(nameof(descriptor), $"Intrinsic descriptor '{descriptor.Name}' for '{descriptor.Target}' is already registered.");

        _intrinsics.Add(key, descriptor);
        return this;
    }

    public DialectRuntimeDescriptorRegistry Build()
    {
        return new DialectRuntimeDescriptorRegistry(_modules, _optimizers, _backends, _intrinsics);
    }
}
