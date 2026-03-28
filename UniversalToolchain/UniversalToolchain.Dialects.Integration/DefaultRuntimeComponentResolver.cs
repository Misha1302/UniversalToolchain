using System.Collections.Concurrent;
using System.Reflection;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentResolver(IRuntimeAssemblyLoadStrategy assemblyLoadStrategy) : IRuntimeComponentResolver
{
    private readonly IRuntimeAssemblyLoadStrategy _assemblyLoadStrategy = assemblyLoadStrategy ?? throw new ArgumentNullException(nameof(assemblyLoadStrategy));
    private readonly ConcurrentDictionary<string, RuntimeComponentDescriptor> _cache = new(StringComparer.Ordinal);

    public RuntimeComponentDescriptor Resolve(RuntimeComponentManifestEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        var key = $"{entry.AssemblySimpleName}|{entry.ComponentId.Value}";
        return _cache.GetOrAdd(key, _ => ResolveCore(entry));
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

        throw new InvalidOperationException(
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