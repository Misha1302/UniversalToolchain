using BasicCore.Contracts;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistDialectBackendRuntime
{
    public WistDialectBackendRuntime(RuntimeBackendDescriptor descriptor, ICoreRunnable core)
    {
        descriptor = descriptor.ArgNotNull();

        core = core.ArgNotNull();

        Descriptor = descriptor;
        Core = core;
    }

    public RuntimeBackendDescriptor Descriptor { get; }

    public ICoreRunnable Core { get; }
}