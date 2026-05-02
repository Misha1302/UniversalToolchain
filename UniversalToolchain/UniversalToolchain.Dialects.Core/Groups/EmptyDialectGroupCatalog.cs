using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Groups;

public sealed class EmptyDialectGroupCatalog : IDialectGroupCatalog
{
    public bool TryResolveGroup(string alias, out DialectGroupDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(alias))
            Thrower.Argument(nameof(alias), "Dialect group alias must not be empty.");

        descriptor = null;
        return false;
    }

    public IReadOnlyList<DialectGroupDescriptor> GetGroups() => [];
}