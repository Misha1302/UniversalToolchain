using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Deterministic in-memory runtime profile catalog.
/// </summary>
public sealed class RuntimeProfileCatalog : IRuntimeProfileCatalog
{
    private readonly ReadOnlyCollection<RuntimeProfileDefinition> _profiles;

    public RuntimeProfileCatalog(IEnumerable<RuntimeProfileDefinition> profiles)
    {
        profiles = profiles.ArgNotNull();

        var snapshot = profiles.Select(static x => x.NotNull()).ToArray();
        var duplicate = snapshot
            .GroupBy(static x => x.Name, StringComparer.Ordinal)
            .FirstOrDefault(static x => x.Count() > 1);
        if (duplicate != null)
            Thrower.InvalidOpEx($"Duplicate runtime profile '{duplicate.Key}'.");

        _profiles = new ReadOnlyCollection<RuntimeProfileDefinition>(
            snapshot.OrderBy(static x => x.Name, StringComparer.Ordinal).ToList());
    }

    public IReadOnlyList<RuntimeProfileDefinition> Profiles => _profiles;

    public bool TryGet(string name, [MaybeNullWhen(false)] out RuntimeProfileDefinition profile)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            profile = null;
            return false;
        }

        profile = _profiles.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));
        return profile != null;
    }
}
