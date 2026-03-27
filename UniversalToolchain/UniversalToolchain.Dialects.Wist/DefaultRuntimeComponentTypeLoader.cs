using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

public sealed class DefaultRuntimeComponentTypeLoader : IRuntimeComponentTypeLoader
{
    private readonly ConcurrentDictionary<string, Lazy<Type>> _cache = new(StringComparer.Ordinal);

    public Type LoadType(RuntimeComponentManifestEntry entry)
    {
        if (entry == null)
            Thrower.ArgumentNull(nameof(entry));

        if (string.IsNullOrWhiteSpace(entry.AssemblySimpleName))
            Thrower.Argument(nameof(entry), "Assembly simple name must not be empty.");

        if (string.IsNullOrWhiteSpace(entry.TypeFullName))
            Thrower.Argument(nameof(entry), "Type full name must not be empty.");

        var key = $"{entry.AssemblySimpleName}|{entry.TypeFullName}";
        var lazy = _cache.GetOrAdd(key, _ => new Lazy<Type>(() => ResolveType(entry), LazyThreadSafetyMode.ExecutionAndPublication));
        return lazy.Value;
    }

    private static Type ResolveType(RuntimeComponentManifestEntry entry)
    {
        var assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(x => string.Equals(x.GetName().Name, entry.AssemblySimpleName, StringComparison.Ordinal));

        if (assembly == null)
        {
            assembly = TryLoadByName(entry.AssemblySimpleName) ?? LoadFromBaseDirectory(entry.AssemblySimpleName);
        }

        return assembly.GetType(entry.TypeFullName, throwOnError: true, ignoreCase: false)
               ?? Thrower.InvalidOpEx<Type>($"Type '{entry.TypeFullName}' was not found in assembly '{entry.AssemblySimpleName}'.");
    }

    private static Assembly? TryLoadByName(string assemblySimpleName)
    {
        try
        {
            return Assembly.Load(new AssemblyName(assemblySimpleName));
        }
        catch
        {
            return null;
        }
    }

    private static Assembly LoadFromBaseDirectory(string assemblySimpleName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, assemblySimpleName + ".dll");
        if (!File.Exists(path))
            Thrower.FileNotFound(path);

        return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    }
}
