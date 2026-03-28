namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeArtifactLocatorOptions
{
    public bool IncludeAppContextBaseDirectory { get; init; } = true;

    public IReadOnlyList<string> AdditionalSearchDirectories { get; init; } = [];

    public string ManifestFilePattern { get; init; } = "*.dialect.runtime.json";

    public SearchOption ManifestSearchOption { get; init; } = SearchOption.TopDirectoryOnly;

    public string AssemblyFileSuffix { get; init; } = ".dll";
}
