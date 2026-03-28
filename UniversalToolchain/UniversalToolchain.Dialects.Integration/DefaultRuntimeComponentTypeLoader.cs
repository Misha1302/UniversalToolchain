using System.Collections.Concurrent;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentTypeLoader : IRuntimeComponentTypeLoader
{
    private readonly ConcurrentDictionary<string, Lazy<Type>> _cache = new(StringComparer.Ordinal);
    private readonly IRuntimeAssemblyLoadStrategy _assemblyLoadStrategy;

    public DefaultRuntimeComponentTypeLoader(IRuntimeAssemblyLoadStrategy assemblyLoadStrategy)
    {
        _assemblyLoadStrategy = assemblyLoadStrategy ?? throw new ArgumentNullException(nameof(assemblyLoadStrategy));
    }

    public Type LoadType(RuntimeComponentManifestEntry entry)
    {
        if (entry == null)
            Thrower.ArgumentNull(nameof(entry));

        var typeReference = entry.TypeReference;
        if (string.IsNullOrWhiteSpace(typeReference.AssemblySimpleName))
            Thrower.Argument(nameof(entry), "Assembly simple name must not be empty.");

        if (string.IsNullOrWhiteSpace(typeReference.TypeFullName))
            Thrower.Argument(nameof(entry), "Type full name must not be empty.");

        var key = $"{typeReference.AssemblySimpleName}|{typeReference.TypeFullName}";
        var lazy = _cache.GetOrAdd(key, _ => new Lazy<Type>(() => ResolveType(typeReference), LazyThreadSafetyMode.ExecutionAndPublication));
        return lazy.Value;
    }

    private Type ResolveType(RuntimeTypeReference typeReference)
    {
        var assembly = _assemblyLoadStrategy.LoadAssembly(typeReference.AssemblySimpleName);
        return assembly.GetType(typeReference.TypeFullName, throwOnError: true, ignoreCase: false)
               ?? Thrower.InvalidOpEx<Type>($"Type '{typeReference.TypeFullName}' was not found in assembly '{typeReference.AssemblySimpleName}'.");
    }
}
