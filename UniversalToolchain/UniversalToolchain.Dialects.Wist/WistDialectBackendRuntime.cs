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

        var artifactCompiler = core as IArtifactCompiler;
        if (artifactCompiler is null)
        {
            Thrower.InvalidOpEx(
                $"Backend runtime '{descriptor.CanonicalId}' does not expose a backend-neutral artifact compiler.");
        }

        Descriptor = descriptor;
        Core = core;
        ArtifactCompiler = artifactCompiler.ArgNotNull();
    }

    public RuntimeBackendDescriptor Descriptor { get; }

    public ICoreRunnable Core { get; }

    public IArtifactCompiler ArtifactCompiler { get; }
}
