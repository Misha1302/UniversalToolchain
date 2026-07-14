using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Lazy backend registration whose cache is scoped to the resolving service provider.
/// </summary>
public sealed class ToolchainBackendRuntimeRegistration
{
    private readonly Func<IServiceProvider, ToolchainBackendRuntime> _factory;
    private readonly ConditionalWeakTable<IServiceProvider, RuntimeHolder> _runtimes = new();

    public ToolchainBackendRuntimeRegistration(
        RuntimeBackendDescriptor descriptor,
        Func<IServiceProvider, ToolchainBackendRuntime> factory)
    {
        Descriptor = descriptor.ArgNotNull();
        _factory = factory.ArgNotNull();
    }

    public RuntimeBackendDescriptor Descriptor { get; }

    public ToolchainBackendRuntime Resolve(IServiceProvider serviceProvider)
    {
        serviceProvider = serviceProvider.ArgNotNull();
        var holder = _runtimes.GetValue(serviceProvider, static _ => new RuntimeHolder());
        return holder.Resolve(Descriptor, _factory, serviceProvider);
    }

    private sealed class RuntimeHolder
    {
        private readonly object _sync = new();
        private ToolchainBackendRuntime? _runtime;

        public ToolchainBackendRuntime Resolve(
            RuntimeBackendDescriptor descriptor,
            Func<IServiceProvider, ToolchainBackendRuntime> factory,
            IServiceProvider serviceProvider)
        {
            if (_runtime is not null)
                return _runtime;

            lock (_sync)
            {
                if (_runtime is not null)
                    return _runtime;

                var candidate = factory(serviceProvider).ArgNotNull();
                if (candidate.Descriptor.BackendId != descriptor.BackendId)
                {
                    return Thrower.InvalidOpEx<ToolchainBackendRuntime>(
                        $"Backend runtime factory for '{descriptor.CanonicalId}' returned runtime " +
                        $"'{candidate.Descriptor.CanonicalId}'.");
                }

                _runtime = candidate;
                return _runtime;
            }
        }
    }
}
