using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeAssemblyLocator : IRuntimeAssemblyLocator
{
    private readonly string _assemblyFileExtension;
    private readonly IReadOnlyList<string> _searchRoots;

    public DefaultRuntimeAssemblyLocator(RuntimeArtifactLocatorOptions options)
    {
        if (options == null)
            Thrower.ArgumentNull(nameof(options));

        _assemblyFileExtension = string.IsNullOrWhiteSpace(options.AssemblyFileExtension)
            ? ".dll"
            : options.AssemblyFileExtension;

        var searchRoots = options.SearchRoots.AsEnumerable();
        if (options.IncludeAppContextBaseDirectory)
            searchRoots = searchRoots.Append(AppContext.BaseDirectory);

        _searchRoots = searchRoots
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }

    public bool TryResolveAssemblyPath(string assemblySimpleName, out string? absolutePath)
    {
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            Thrower.Argument(nameof(assemblySimpleName), "Assembly simple name must not be empty.");

        var fileName = assemblySimpleName.Trim() + _assemblyFileExtension;
        foreach (var root in _searchRoots)
        {
            var candidate = Path.Combine(root, fileName);
            if (!File.Exists(candidate))
                continue;

            absolutePath = Path.GetFullPath(candidate);
            return true;
        }

        absolutePath = null;
        return false;
    }
}