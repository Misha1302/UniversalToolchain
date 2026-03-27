using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeAssemblyLocator : IRuntimeAssemblyLocator
{
    private readonly IReadOnlyList<string> _searchRoots;

    public DefaultRuntimeAssemblyLocator(RuntimeArtifactLocatorOptions? options = null)
    {
        options ??= new RuntimeArtifactLocatorOptions();

        _searchRoots = new[] { AppContext.BaseDirectory }
            .Concat(options.AdditionalSearchDirectories ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public bool TryResolveAssemblyPath(string assemblySimpleName, out string? absolutePath)
    {
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            Thrower.Argument(nameof(assemblySimpleName), "Assembly simple name must not be empty.");

        var fileName = assemblySimpleName.Trim() + ".dll";
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
