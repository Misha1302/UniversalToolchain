namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeManifestFileLocator : IRuntimeManifestFileLocator
{
    private readonly string _manifestFilePattern;
    private readonly SearchOption _manifestSearchOption;
    private readonly IReadOnlyList<string> _searchRoots;

    public DefaultRuntimeManifestFileLocator(RuntimeArtifactLocatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _searchRoots = GetSearchRoots(options);
        _manifestFilePattern = string.IsNullOrWhiteSpace(options.ManifestFilePattern) ? "*.dialect.runtime.json" : options.ManifestFilePattern.Trim();
        _manifestSearchOption = options.ManifestSearchOption;
    }

    public IReadOnlyList<string> GetManifestFilePaths()
    {
        var paths = new List<string>();

        foreach (var root in _searchRoots)
        {
            if (!Directory.Exists(root))
                continue;

            paths.AddRange(Directory.EnumerateFiles(root, _manifestFilePattern, _manifestSearchOption)
                .Select(Path.GetFullPath));
        }

        return paths
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<string> GetSearchRoots(RuntimeArtifactLocatorOptions options)
    {
        var roots = Enumerable.Empty<string>();
        if (options.IncludeAppContextBaseDirectory)
            roots = roots.Append(AppContext.BaseDirectory);

        return roots
            .Concat(options.AdditionalSearchDirectories ?? [])
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }
}
