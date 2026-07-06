using BasicCore.Compilation;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Default artifact executor used by the neutral runtime host.
/// </summary>
public sealed class DefaultToolchainArtifactExecutor : IToolchainArtifactExecutor
{
    public static DefaultToolchainArtifactExecutor Instance { get; } = new();

    private DefaultToolchainArtifactExecutor()
    {
    }

    public object? Run(ICompiledArtifact artifact, IReadOnlyDictionary<string, object?> arguments)
    {
        artifact = artifact.ArgNotNull();
        arguments = arguments.ArgNotNull();

        var session = artifact.CreateSession();
        foreach (var argument in arguments)
            session.SetArgument(argument.Key, argument.Value);

        return session.Run();
    }
}
