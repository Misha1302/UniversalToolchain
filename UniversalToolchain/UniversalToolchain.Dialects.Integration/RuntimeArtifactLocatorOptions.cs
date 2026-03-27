namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeArtifactLocatorOptions
{
    public IReadOnlyList<string> AdditionalSearchDirectories { get; init; } = [];
}
