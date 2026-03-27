using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Runtime type-loading strategy. Current implementation uses the default load context,
/// but callers should depend on <see cref="IRuntimeComponentTypeLoader"/> rather than context-specific behavior.
/// </summary>
public sealed class DefaultRuntimeComponentTypeLoader : IRuntimeComponentTypeLoader
{
    private readonly ConcurrentDictionary<string, Lazy<Type>> _cache = new(StringComparer.Ordinal);
    private readonly IRuntimeAssemblyLocator _locator;

    public DefaultRuntimeComponentTypeLoader(IRuntimeAssemblyLocator locator)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
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
        var assembly = TryGetAlreadyLoadedAssembly(typeReference.AssemblySimpleName)
                       ?? TryLoadBySimpleName(typeReference.AssemblySimpleName)
                       ?? LoadAssemblyFromResolvedPath(typeReference.AssemblySimpleName);

        return ResolveTypeFromAssembly(assembly, typeReference);
    }

    private static Assembly? TryGetAlreadyLoadedAssembly(string assemblySimpleName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(x => string.Equals(x.GetName().Name, assemblySimpleName, StringComparison.Ordinal));
    }

    private static Assembly? TryLoadBySimpleName(string assemblySimpleName)
    {
        try
        {
            return Assembly.Load(new AssemblyName(assemblySimpleName));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    private Assembly LoadAssemblyFromResolvedPath(string assemblySimpleName)
    {
        if (!_locator.TryResolveAssemblyPath(assemblySimpleName, out var absolutePath) || string.IsNullOrWhiteSpace(absolutePath))
            Thrower.FileNotFound(Path.Combine(AppContext.BaseDirectory, assemblySimpleName + ".dll"));

        if (!Path.IsPathRooted(absolutePath))
            Thrower.Argument(nameof(absolutePath), $"Assembly locator returned non-absolute path '{absolutePath}'.");

        return AssemblyLoadContext.Default.LoadFromAssemblyPath(absolutePath);
    }

    private static Type ResolveTypeFromAssembly(Assembly assembly, RuntimeTypeReference typeReference)
    {
        return assembly.GetType(typeReference.TypeFullName, throwOnError: true, ignoreCase: false)
               ?? Thrower.InvalidOpEx<Type>($"Type '{typeReference.TypeFullName}' was not found in assembly '{typeReference.AssemblySimpleName}'.");
    }
}
