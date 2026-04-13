using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeManifestFileLocator : IRuntimeManifestFileLocator
{
    private readonly SearchOption _manifestSearchOption;
    private readonly string _manifestSearchPattern;
    private readonly IReadOnlyList<string> _searchRoots;

    public DefaultRuntimeManifestFileLocator(RuntimeArtifactLocatorOptions options)
    {
        options = options.ArgNotNull();

        _manifestSearchPattern = string.IsNullOrWhiteSpace(options.ManifestSearchPattern)
            ? "*.dialect.runtime.json"
            : options.ManifestSearchPattern;

        _manifestSearchOption = options.ManifestSearchOption;
        _searchRoots = ResolveSearchRoots(options);
    }

    public IReadOnlyList<string> GetManifestFilePaths()
    {
        var paths = new List<string>();

        foreach (var root in _searchRoots)
        {
            if (!Directory.Exists(root))
                continue;

            paths.AddRange(Directory.EnumerateFiles(root, _manifestSearchPattern, _manifestSearchOption)
                .Select(Path.GetFullPath));
        }

        return paths
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<string> ResolveSearchRoots(RuntimeArtifactLocatorOptions options)
    {
        var searchRoots = options.SearchRoots.AsEnumerable();
        if (options.IncludeAppContextBaseDirectory)
            searchRoots = searchRoots.Append(AppContext.BaseDirectory);

        return searchRoots
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }
}
