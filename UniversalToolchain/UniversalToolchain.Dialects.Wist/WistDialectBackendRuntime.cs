namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistDialectBackendRuntime
{
    public WistDialectBackendRuntime(RuntimeBackendDescriptor descriptor, ICoreRunnable core)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        if (core == null)
            Thrower.ArgumentNull(nameof(core));

        Descriptor = descriptor;
        Core = core;
    }

    public RuntimeBackendDescriptor Descriptor { get; }

    public ICoreRunnable Core { get; }
}
