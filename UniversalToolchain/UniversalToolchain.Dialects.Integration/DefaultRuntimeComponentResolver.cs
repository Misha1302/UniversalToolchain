using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentResolver : IRuntimeComponentResolver
{
    private readonly IRuntimeAssemblyLoadStrategy _assemblyLoadStrategy;
    private readonly ConcurrentDictionary<string, Lazy<Assembly>> _assemblyCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<IReadOnlyDictionary<RuntimeComponentId, RuntimeComponentDescriptor>>> _assemblyComponentIndexCache = new(StringComparer.Ordinal);

    public DefaultRuntimeComponentResolver(IRuntimeAssemblyLoadStrategy assemblyLoadStrategy)
    {
        if (assemblyLoadStrategy == null)
            Thrower.ArgumentNull(nameof(assemblyLoadStrategy));

        _assemblyLoadStrategy = assemblyLoadStrategy;
    }

    public RuntimeComponentDescriptor Resolve(RuntimeComponentManifestEntry entry)
    {
        if (entry == null)
            Thrower.ArgumentNull(nameof(entry));

        var index = GetAssemblyComponentIndex(entry.AssemblySimpleName);
        if (index.TryGetValue(entry.ComponentId, out var descriptor))
            return descriptor;

        return Thrower.InvalidOpEx<RuntimeComponentDescriptor>(
            $"Runtime component '{entry.ComponentId}' was not found in assembly '{entry.AssemblySimpleName}'.");
    }

    private IReadOnlyDictionary<RuntimeComponentId, RuntimeComponentDescriptor> GetAssemblyComponentIndex(string assemblySimpleName)
    {
        var lazy = _assemblyComponentIndexCache.GetOrAdd(
            assemblySimpleName,
            static (name, resolver) => new Lazy<IReadOnlyDictionary<RuntimeComponentId, RuntimeComponentDescriptor>>(
                () => resolver.BuildAssemblyComponentIndex(name),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        return lazy.Value;
    }

    private IReadOnlyDictionary<RuntimeComponentId, RuntimeComponentDescriptor> BuildAssemblyComponentIndex(string assemblySimpleName)
    {
        var assembly = GetAssembly(assemblySimpleName);
        var descriptors = new Dictionary<RuntimeComponentId, RuntimeComponentDescriptor>();

        foreach (var type in GetLoadableTypes(assembly))
        {
            if (!TryGetRuntimeExport(type, out var kind, out var canonicalAlias))
                continue;

            var id = RuntimeComponentIdFactory.Create(kind, canonicalAlias);
            var aliases = GetRuntimeAliases(type);
            descriptors[id] = new RuntimeComponentDescriptor(id, kind, canonicalAlias, aliases, type);
        }

        return descriptors;
    }

    private Assembly GetAssembly(string assemblySimpleName)
    {
        var lazy = _assemblyCache.GetOrAdd(
            assemblySimpleName,
            static (name, resolver) => new Lazy<Assembly>(
                () => resolver._assemblyLoadStrategy.LoadAssembly(name),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        return lazy.Value;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(static type => type != null).Cast<Type>();
        }
    }

    private static bool TryGetRuntimeExport(Type type, out RuntimeComponentKind kind, out string canonicalAlias)
    {
        var export = type.GetCustomAttribute<DialectRuntimeExportAttribute>(false);
        if (export == null)
        {
            kind = default;
            canonicalAlias = string.Empty;
            return false;
        }

        kind = RuntimeComponentKindCodec.Parse(export.ComponentKind, type.AssemblyQualifiedName ?? type.Name);
        canonicalAlias = export.CanonicalAlias;
        return true;
    }

    private static IReadOnlyList<string> GetRuntimeAliases(MemberInfo type)
    {
        return type
            .GetCustomAttributes<DialectRuntimeAliasAttribute>(false)
            .Select(x => x.Alias?.Trim())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }
}
