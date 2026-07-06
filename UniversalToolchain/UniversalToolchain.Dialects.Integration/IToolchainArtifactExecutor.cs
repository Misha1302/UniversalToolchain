using BasicCore.Compilation;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Executes compiled artifacts without exposing backend-specific artifact internals.
/// </summary>
public interface IToolchainArtifactExecutor
{
    object? Run(ICompiledArtifact artifact, IReadOnlyDictionary<string, object?> arguments);
}
