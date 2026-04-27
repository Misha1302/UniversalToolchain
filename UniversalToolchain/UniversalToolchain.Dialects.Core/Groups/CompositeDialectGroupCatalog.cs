using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Groups;

public sealed class CompositeDialectGroupCatalog : IDialectGroupCatalog
{
    private readonly IReadOnlyDictionary<string, DialectGroupDescriptor> _groupsByAlias;
    private readonly IReadOnlyList<DialectGroupDescriptor> _groups;

    public CompositeDialectGroupCatalog(IEnumerable<IDialectGroupProvider> providers)
    {
        providers = providers.ArgNotNull();

        var groups = providers
            .SelectMany(provider => provider.GetGroups())
            .Select(DialectGroupDescriptorValidator.ValidateAndNormalize)
            .OrderBy(group => group.Alias, StringComparer.Ordinal)
            .ToList();

        var groupsByAlias = new Dictionary<string, DialectGroupDescriptor>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            if (!groupsByAlias.TryAdd(group.Alias, group))
                Thrower.InvalidOpEx($"Duplicate dialect group alias '{group.Alias}'.");
        }

        _groups = groups;
        _groupsByAlias = groupsByAlias;
    }

    public bool TryResolveGroup(string alias, out DialectGroupDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(alias))
            Thrower.Argument(nameof(alias), "Dialect group alias must not be empty.");

        return _groupsByAlias.TryGetValue(alias.Trim(), out descriptor);
    }

    public IReadOnlyList<DialectGroupDescriptor> GetGroups() => _groups;
}
