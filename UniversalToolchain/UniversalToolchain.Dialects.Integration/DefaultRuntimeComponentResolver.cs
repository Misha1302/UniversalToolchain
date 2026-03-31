using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentResolver : IRuntimeComponentResolver
{
    private readonly IRuntimeAssemblyLoadStrategy _assemblyLoadStrategy;
    private readonly ConcurrentDictionary<string, Lazy<RuntimeComponentDescriptor>> _cache = new(StringComparer.Ordinal);

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

        var key = $"{entry.AssemblySimpleName}|{entry.ComponentId.Value}";
        var lazy = _cache.GetOrAdd(
            key,
            _ => new Lazy<RuntimeComponentDescriptor>(
                () => ResolveCore(entry),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return lazy.Value;
    }

    private RuntimeComponentDescriptor ResolveCore(RuntimeComponentManifestEntry entry)
    {
        var assembly = _assemblyLoadStrategy.LoadAssembly(entry.AssemblySimpleName);

        foreach (var type in assembly.GetTypes())
        {
            if (!TryGetRuntimeExport(type, out var kind, out var canonicalAlias))
                continue;

            var id = RuntimeComponentIdFactory.Create(kind, canonicalAlias);
            if (id != entry.ComponentId)
                continue;

            var aliases = GetRuntimeAliases(type);
            return new RuntimeComponentDescriptor(id, kind, canonicalAlias, aliases, type);
        }

        return Thrower.InvalidOpEx<RuntimeComponentDescriptor>(
            $"Runtime component '{entry.ComponentId}' was not found in assembly '{entry.AssemblySimpleName}'.");
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
