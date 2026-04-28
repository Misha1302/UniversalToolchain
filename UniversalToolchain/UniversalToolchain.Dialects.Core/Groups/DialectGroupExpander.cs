using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;

namespace UniversalToolchain.Dialects.Core.Groups;

public sealed class DialectGroupExpander
{
    private readonly IDialectGroupCatalog _catalog;

    public DialectGroupExpander(IDialectGroupCatalog catalog)
    {
        _catalog = catalog.ArgNotNull();
    }

    internal IDialectBindingSource Expand(IDialectBindingSource source, List<DialectDiagnostic> diagnostics)
    {
        source = source.ArgNotNull();
        diagnostics = diagnostics.ArgNotNull();

        var expandedModules = new List<string>();
        var expandedCapabilities = new List<KeyValuePair<string, bool>>(source.Capabilities);
        var capabilityMap = source.Capabilities.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

        foreach (var alias in source.UseModules)
        {
            if (!_catalog.TryResolveGroup(alias, out var group) || group == null)
            {
                expandedModules.Add(alias);
                continue;
            }

            expandedModules.AddRange(group.IncludedModules);
            AddCapabilities(expandedCapabilities, capabilityMap, group, diagnostics);
        }

        return new ExpandedDialectBindingSource(source, expandedModules, expandedCapabilities);
    }

    private static void AddCapabilities(
        ICollection<KeyValuePair<string, bool>> capabilities,
        IDictionary<string, bool> capabilityMap,
        DialectGroupDescriptor group,
        ICollection<DialectDiagnostic> diagnostics)
    {
        foreach (var capability in group.Capabilities)
        {
            if (capabilityMap.TryGetValue(capability.Key, out var existing))
            {
                if (existing != capability.Value)
                {
                    diagnostics.Add(new DialectDiagnostic(
                        "G001",
                        $"Dialect group '{group.Alias}' conflicts with capability '{capability.Key}'.",
                        DialectDiagnosticSeverity.Error));
                }

                continue;
            }

            capabilityMap.Add(capability.Key, capability.Value);
            capabilities.Add(capability);
        }
    }
}
