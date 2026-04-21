using System.Collections.Concurrent;
using System.Reflection;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentResolver : IRuntimeComponentResolver
{
    private readonly ConcurrentDictionary<string, Lazy<Assembly>> _assemblyCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<IReadOnlyDictionary<RuntimeComponentId, RuntimeComponentExportDescriptor>>>
        _assemblyComponentIndexCache = new(StringComparer.Ordinal);
    private readonly IRuntimeAssemblyLoadStrategy _assemblyLoadStrategy;

    public DefaultRuntimeComponentResolver(IRuntimeAssemblyLoadStrategy assemblyLoadStrategy)
    {
        assemblyLoadStrategy = assemblyLoadStrategy.ArgNotNull();

        _assemblyLoadStrategy = assemblyLoadStrategy;
    }

    public RuntimeComponentDescriptor Resolve(RuntimeComponentManifestEntry entry)
    {
        entry = entry.ArgNotNull();

        var index = GetAssemblyComponentIndex(entry.AssemblySimpleName);
        if (index.TryGetValue(entry.ComponentId, out var descriptor))
        {
            ValidateResolvedComponent(entry, descriptor);
            return CreateResolvedDescriptor(entry, descriptor);
        }

        return Thrower.InvalidOpEx<RuntimeComponentDescriptor>(
            $"Runtime component '{entry.ComponentId}' was not found in assembly '{entry.AssemblySimpleName}'.");
    }

    private IReadOnlyDictionary<RuntimeComponentId, RuntimeComponentExportDescriptor> GetAssemblyComponentIndex(string assemblySimpleName)
    {
        var lazy = _assemblyComponentIndexCache.GetOrAdd(
            assemblySimpleName,
            static (name, resolver) => new Lazy<IReadOnlyDictionary<RuntimeComponentId, RuntimeComponentExportDescriptor>>(
                () => resolver.BuildAssemblyComponentIndex(name),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        return lazy.Value;
    }

    private IReadOnlyDictionary<RuntimeComponentId, RuntimeComponentExportDescriptor> BuildAssemblyComponentIndex(string assemblySimpleName)
    {
        var assembly = GetAssembly(assemblySimpleName);
        var descriptors = new Dictionary<RuntimeComponentId, RuntimeComponentExportDescriptor>();

        foreach (var type in GetLoadableTypes(assembly))
        {
            if (!TryGetRuntimeExport(type, out var kind, out var canonicalAlias))
                continue;

            var id = RuntimeComponentIdFactory.Create(kind, canonicalAlias);
            var aliases = GetRuntimeAliases(type);
            var descriptor = new RuntimeComponentExportDescriptor(id, kind, canonicalAlias, aliases, type);
            if (!descriptors.TryAdd(id, descriptor))
                Thrower.InvalidOpEx("Duplicate runtime component id detected during assembly component indexing.");
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

    private static void ValidateResolvedComponent(RuntimeComponentManifestEntry entry, RuntimeComponentExportDescriptor descriptor)
    {
        if (descriptor.Kind != entry.Kind)
            Thrower.InvalidOpEx(
                $"Runtime manifest entry '{entry.ComponentId}' resolves to type '{GetTypeName(descriptor.ActivationType)}', but the exported component kind is '{RuntimeComponentKindCodec.Format(descriptor.Kind)}' " +
                $"instead of '{RuntimeComponentKindCodec.Format(entry.Kind)}'.");

        if (!string.Equals(descriptor.CanonicalAlias, entry.CanonicalAlias, StringComparison.Ordinal))
            Thrower.InvalidOpEx(
                $"Runtime manifest entry '{entry.ComponentId}' resolves to type '{GetTypeName(descriptor.ActivationType)}', but the exported canonical alias is '{descriptor.CanonicalAlias}' " +
                $"instead of '{entry.CanonicalAlias}'.");
    }

    private static RuntimeComponentDescriptor CreateResolvedDescriptor(
        RuntimeComponentManifestEntry entry,
        RuntimeComponentExportDescriptor descriptor)
    {
        return new RuntimeComponentDescriptor(
            entry.ComponentId,
            entry.Kind,
            entry.CanonicalAlias,
            entry.Aliases,
            descriptor.ActivationType);
    }

    private static string GetTypeName(Type type) => type.FullName ?? type.Name;

    private sealed record RuntimeComponentExportDescriptor(
        RuntimeComponentId Id,
        RuntimeComponentKind Kind,
        string CanonicalAlias,
        IReadOnlyList<string> Aliases,
        Type ActivationType);
}
