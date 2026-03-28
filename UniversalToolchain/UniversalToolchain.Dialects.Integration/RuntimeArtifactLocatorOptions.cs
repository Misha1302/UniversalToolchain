namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeArtifactLocatorOptions
{
    public IReadOnlyList<string> SearchRoots { get; init; } = [];

    public IReadOnlyList<string> AdditionalSearchDirectories { get; init; } = [];

    public string ManifestSearchPattern { get; init; } = "*.dialect.runtime.json";

    public SearchOption ManifestSearchOption { get; init; } = SearchOption.TopDirectoryOnly;

    public string AssemblyFileExtension { get; init; } = ".dll";
}
