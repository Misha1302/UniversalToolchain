using System.Collections.Concurrent;
using System.Reflection;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentResolver : IRuntimeComponentResolver
{
    private readonly ConcurrentDictionary<string, Lazy<IReadOnlyDictionary<RuntimeComponentId, RuntimeComponentExportDescriptor>>>
        _assemblyComponentIndexCache = new(StringComparer.Ordinal);
    private readonly IRuntimeAssemblyTypeLoader _assemblyTypeLoader;

    public DefaultRuntimeComponentResolver(IRuntimeAssemblyLoadStrategy assemblyLoadStrategy)
        : this(new DefaultRuntimeAssemblyTypeLoader(assemblyLoadStrategy))
    {
    }

    public DefaultRuntimeComponentResolver(IRuntimeAssemblyTypeLoader assemblyTypeLoader)
    {
        assemblyTypeLoader = assemblyTypeLoader.ArgNotNull();

        _assemblyTypeLoader = assemblyTypeLoader;
    }

    public RuntimeComponentDescriptor Resolve(RuntimeComponentManifestEntry entry)
    {
        entry = entry.ArgNotNull();

        if (entry.Activation != null)
            return ResolveExactActivation(entry);

        return ResolveLegacyScannedActivation(entry);
    }

    private RuntimeComponentDescriptor ResolveExactActivation(RuntimeComponentManifestEntry entry)
    {
        var type = _assemblyTypeLoader.LoadType(entry.AssemblySimpleName, entry.Activation!.ActivationTypeFullName);
        var export = type.GetCustomAttribute<DialectRuntimeExportAttribute>(false);
        if (export == null)
            Thrower.InvalidOpEx(
                $"Runtime activation type '{GetTypeName(type)}' for manifest entry '{entry.ComponentId}' does not declare DialectRuntimeExportAttribute.");

        var descriptor = CreateExportDescriptor(type, export!);
        ValidateResolvedComponent(entry, descriptor);

        return CreateResolvedDescriptor(entry, descriptor);
    }

    private RuntimeComponentDescriptor ResolveLegacyScannedActivation(RuntimeComponentManifestEntry entry)
    {
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
        var assembly = _assemblyTypeLoader.LoadAssembly(assemblySimpleName);
        var descriptors = new Dictionary<RuntimeComponentId, RuntimeComponentExportDescriptor>();

        foreach (var type in GetLoadableTypes(assembly))
        {
            var export = type.GetCustomAttribute<DialectRuntimeExportAttribute>(false);
            if (export == null)
                continue;

            var descriptor = CreateExportDescriptor(type, export);
            if (!descriptors.TryAdd(descriptor.Id, descriptor))
                Thrower.InvalidOpEx("Duplicate runtime component id detected during assembly component indexing.");
        }

        return descriptors;
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

    private static RuntimeComponentExportDescriptor CreateExportDescriptor(Type type, DialectRuntimeExportAttribute export)
    {
        var kind = RuntimeComponentKindCodec.Parse(export.ComponentKind, type.AssemblyQualifiedName ?? type.Name);
        var canonicalAlias = export.CanonicalAlias;
        var id = RuntimeComponentIdFactory.Create(kind, canonicalAlias);
        var aliases = GetRuntimeAliases(type);

        return new RuntimeComponentExportDescriptor(id, kind, canonicalAlias, aliases, type);
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
