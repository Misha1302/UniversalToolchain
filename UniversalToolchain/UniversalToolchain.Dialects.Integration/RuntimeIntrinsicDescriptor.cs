using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Describes one explicitly available intrinsic capability.
/// </summary>
public sealed class RuntimeIntrinsicDescriptor
{
    public RuntimeIntrinsicDescriptor(string canonicalId, DialectBackendId target, IEnumerable<string>? aliases = null)
        : this(canonicalId, DialectBackendSelector.For(target), aliases)
    {
    }

    private readonly ReadOnlyCollection<string> _aliases;

    public RuntimeIntrinsicDescriptor(string canonicalId, DialectBackendSelector target, IEnumerable<string>? aliases = null)
    {
        if (string.IsNullOrWhiteSpace(canonicalId))
            Thrower.Argument(nameof(canonicalId), "Intrinsic descriptor canonical identifier must not be empty.");

        CanonicalId = canonicalId;
        Target = target;
        _aliases = new ReadOnlyCollection<string>(SnapshotAliases(aliases, nameof(aliases), canonicalId));
    }

    public string CanonicalId { get; }

    public string Name => CanonicalId;

    public IReadOnlyList<string> Aliases => _aliases;

    public IReadOnlyList<string> AllNames => [CanonicalId, .. _aliases];

    public DialectBackendSelector Target { get; }

    public bool AppliesTo(DialectBackendId backendId) => Target.Matches(backendId);

    private static List<string> SnapshotAliases(IEnumerable<string>? aliases, string paramName, string canonicalId)
    {
        if (aliases == null)
            return [];

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal)
        {
            canonicalId
        };

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
                Thrower.Argument(paramName, "Alias list must not contain empty values.");

            if (seen.Add(alias))
                result.Add(alias);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }
}
