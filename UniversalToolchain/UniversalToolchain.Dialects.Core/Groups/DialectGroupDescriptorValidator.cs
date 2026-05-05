using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Groups;

internal static class DialectGroupDescriptorValidator
{
    public static DialectGroupDescriptor ValidateAndNormalize(DialectGroupDescriptor descriptor)
    {
        descriptor = descriptor.ArgNotNull();

        var alias = descriptor.Alias.Trim();
        if (string.IsNullOrWhiteSpace(alias))
            Thrower.Argument(nameof(descriptor), "Dialect group alias must not be empty.");

        var modules = NormalizeModules(descriptor.IncludedModules, alias);
        var capabilities = NormalizeCapabilities(descriptor.Capabilities, alias);

        return descriptor with
        {
            Alias = alias,
            IncludedModules = modules,
            Capabilities = capabilities
        };
    }

    private static IReadOnlyList<string> NormalizeModules(IReadOnlyList<string> modules, string groupAlias)
    {
        modules = modules.ArgNotNull();

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var module in modules)
        {
            var normalized = module.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                Thrower.Argument(nameof(modules), $"Dialect group '{groupAlias}' contains an empty module alias.");

            if (seen.Add(normalized))
                result.Add(normalized);
        }

        return result;
    }

    private static IReadOnlyList<KeyValuePair<string, bool>> NormalizeCapabilities(
        IReadOnlyList<KeyValuePair<string, bool>> capabilities,
        string groupAlias)
    {
        capabilities = capabilities.ArgNotNull();

        var result = new SortedDictionary<string, bool>(StringComparer.Ordinal);
        foreach (var capability in capabilities)
        {
            var name = capability.Key.Trim();
            if (string.IsNullOrWhiteSpace(name))
                Thrower.Argument(nameof(capabilities), $"Dialect group '{groupAlias}' contains an empty capability name.");

            if (result.TryGetValue(name, out var existing) && existing != capability.Value)
                Thrower.Argument(nameof(capabilities), $"Dialect group '{groupAlias}' contains conflicting values for capability '{name}'.");

            result[name] = capability.Value;
        }

        return result.ToList();
    }
}
