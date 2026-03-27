namespace UniversalToolchain.Dialects.Wist;

public sealed class RuntimeArtifactLocatorOptions
{
    public IReadOnlyList<string> AdditionalSearchDirectories { get; init; } = [];
}
