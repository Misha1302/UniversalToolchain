using BasicCore.Contracts;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Backend-neutral runtime wrapper for a selected dialect backend.
/// </summary>
public sealed class ToolchainBackendRuntime
{
    public ToolchainBackendRuntime(RuntimeBackendDescriptor descriptor, ICoreRunnable core)
    {
        descriptor = descriptor.ArgNotNull();
        core = core.ArgNotNull();

        var artifactCompiler = core as IArtifactCompiler;
        if (artifactCompiler is null)
            Thrower.InvalidOpEx(
                $"Backend runtime '{descriptor.CanonicalId}' does not expose a backend-neutral artifact compiler.");

        Descriptor = descriptor;
        Core = core;
        ArtifactCompiler = artifactCompiler.ArgNotNull();
    }

    public RuntimeBackendDescriptor Descriptor { get; }

    public ICoreRunnable Core { get; }

    public IArtifactCompiler ArtifactCompiler { get; }
}
