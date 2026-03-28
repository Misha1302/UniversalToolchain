using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeAssemblyLocator : IRuntimeAssemblyLocator
{
    private readonly string _assemblyFileSuffix;
    private readonly IReadOnlyList<string> _searchRoots;

    public DefaultRuntimeAssemblyLocator(RuntimeArtifactLocatorOptions options)
    {
        if (options == null)
            Thrower.ArgumentNull(nameof(options));

        _searchRoots = GetSearchRoots(options);
        _assemblyFileSuffix = string.IsNullOrWhiteSpace(options.AssemblyFileSuffix) ? ".dll" : options.AssemblyFileSuffix.Trim();
    }

    public bool TryResolveAssemblyPath(string assemblySimpleName, out string? absolutePath)
    {
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            Thrower.Argument(nameof(assemblySimpleName), "Assembly simple name must not be empty.");

        var fileName = assemblySimpleName.Trim() + _assemblyFileSuffix;
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

    private static IReadOnlyList<string> GetSearchRoots(RuntimeArtifactLocatorOptions options)
    {
        var roots = Enumerable.Empty<string>();
        if (options.IncludeAppContextBaseDirectory)
            roots = roots.Append(AppContext.BaseDirectory);

        return roots
            .Concat(options.AdditionalSearchDirectories ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }
}
