namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeManifestFileLocator : IRuntimeManifestFileLocator
{
    private readonly IReadOnlyList<string> _searchRoots;

    public DefaultRuntimeManifestFileLocator(RuntimeArtifactLocatorOptions? options = null)
    {
        options ??= new RuntimeArtifactLocatorOptions();

        _searchRoots = new[] { AppContext.BaseDirectory }
            .Concat(options.AdditionalSearchDirectories ?? [])
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<string> GetManifestFilePaths()
    {
        var paths = new List<string>();

        foreach (var root in _searchRoots)
        {
            if (!Directory.Exists(root))
                continue;

            paths.AddRange(Directory.EnumerateFiles(root, "*.dialect.runtime.json", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFullPath));
        }

        return paths
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }
}
