using System.Collections.Concurrent;
using System.Reflection;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Loads runtime assemblies and exact activation types through the configured assembly load strategy.
/// </summary>
public sealed class DefaultRuntimeAssemblyTypeLoader : IRuntimeAssemblyTypeLoader
{
    private readonly ConcurrentDictionary<string, Lazy<Assembly>> _assemblyCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<RuntimeAssemblyTypeKey, Lazy<Type>> _typeCache = new();
    private readonly IRuntimeAssemblyLoadStrategy _assemblyLoadStrategy;

    public DefaultRuntimeAssemblyTypeLoader(IRuntimeAssemblyLoadStrategy assemblyLoadStrategy)
    {
        assemblyLoadStrategy = assemblyLoadStrategy.ArgNotNull();

        _assemblyLoadStrategy = assemblyLoadStrategy;
    }

    public Assembly LoadAssembly(string assemblySimpleName)
    {
        assemblySimpleName = assemblySimpleName.ArgNotNull();

        var lazy = _assemblyCache.GetOrAdd(
            assemblySimpleName,
            static (name, loader) => new Lazy<Assembly>(
                () => loader._assemblyLoadStrategy.LoadAssembly(name),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        return lazy.Value;
    }

    public Type LoadType(string assemblySimpleName, string activationTypeFullName)
    {
        assemblySimpleName = assemblySimpleName.ArgNotNull();
        activationTypeFullName = activationTypeFullName.ArgNotNull();

        var key = new RuntimeAssemblyTypeKey(assemblySimpleName, activationTypeFullName);
        var lazy = _typeCache.GetOrAdd(
            key,
            static (cacheKey, loader) => new Lazy<Type>(
                () => loader.LoadTypeUncached(cacheKey),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        return lazy.Value;
    }

    private Type LoadTypeUncached(RuntimeAssemblyTypeKey key)
    {
        var assembly = LoadAssembly(key.AssemblySimpleName);
        var type = assembly.GetType(key.ActivationTypeFullName, throwOnError: false, ignoreCase: false);

        return type ?? Thrower.InvalidOpEx<Type>(
            $"Runtime activation type '{key.ActivationTypeFullName}' was not found in assembly '{key.AssemblySimpleName}'.");
    }

    private readonly record struct RuntimeAssemblyTypeKey(
        string AssemblySimpleName,
        string ActivationTypeFullName);
}
