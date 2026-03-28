namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeArtifactLocatorOptions
{
    public IReadOnlyList<string> SearchRoots { get; init; } = [];

    public bool IncludeAppContextBaseDirectory { get; init; } = true;

    public string ManifestSearchPattern { get; init; } = "*.dialect.runtime.json";

    public SearchOption ManifestSearchOption { get; init; } = SearchOption.TopDirectoryOnly;

    public string AssemblyFileExtension { get; init; } = ".dll";
}