using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Describes one explicitly registered runtime backend.
/// </summary>
public sealed class RuntimeBackendDescriptor
{
    private readonly ReadOnlyCollection<string> _aliases;

    public RuntimeBackendDescriptor(DialectBackendId backendId, string alias)
        : this(backendId, [alias])
    {
    }

    public RuntimeBackendDescriptor(DialectBackendId backendId, Type metadataOwnerType, IEnumerable<string>? aliases = null)
    {
        metadataOwnerType = metadataOwnerType.ArgNotNull();

        if (string.IsNullOrWhiteSpace(backendId.Value))
            Thrower.Argument(nameof(backendId), "Runtime backend descriptor must contain a canonical backend identifier.");

        BackendId = backendId;
        MetadataOwnerType = metadataOwnerType;
        _aliases = new ReadOnlyCollection<string>(SnapshotAliases(aliases, nameof(aliases), backendId.Value));
    }

    public RuntimeBackendDescriptor(DialectBackendId backendId, IEnumerable<string>? aliases = null)
        : this(backendId, typeof(RuntimeBackendDescriptor), aliases)
    {
    }

    public DialectBackendId BackendId { get; }

    public string CanonicalId => BackendId.Value;

    public string RuntimeName => _aliases.Count > 0 ? _aliases[0] : CanonicalId;

    public string Name => CanonicalId;

    public IReadOnlyList<string> Aliases => _aliases;

    public IReadOnlyList<string> AllNames => [CanonicalId, .. _aliases];

    public Type MetadataOwnerType { get; }

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