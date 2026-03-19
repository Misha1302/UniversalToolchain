using System.Collections.ObjectModel;
using BasicCore.Contracts;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Describes one explicitly registered runtime module that can be resolved by canonical identifier or alias.
/// </summary>
public sealed class RuntimeModuleDescriptor
{
    private readonly ReadOnlyCollection<string> _aliases;

    public RuntimeModuleDescriptor(string canonicalId, Type implementationType)
        : this(implementationType, null, canonicalId)
    {
    }

    public RuntimeModuleDescriptor(string canonicalId, Type implementationType, IEnumerable<string>? aliases)
        : this(implementationType, aliases, canonicalId)
    {
    }


    public RuntimeModuleDescriptor(Type implementationType, IEnumerable<string>? aliases = null, string? canonicalId = null)
    {
        if (implementationType == null)
            Thrower.ArgumentNull(nameof(implementationType));

        if (!typeof(IFrontendCoreModule).IsAssignableFrom(implementationType) &&
            !typeof(IIRProcessingModule).IsAssignableFrom(implementationType))
            Thrower.Argument(nameof(implementationType), "Module type must implement IFrontendCoreModule or IIRProcessingModule.");

        var resolvedCanonicalId = canonicalId ?? implementationType.FullName;
        if (string.IsNullOrWhiteSpace(resolvedCanonicalId))
            Thrower.Argument(nameof(canonicalId), "Module canonical identifier must not be empty.");

        CanonicalId = resolvedCanonicalId;
        ImplementationType = implementationType;
        _aliases = new ReadOnlyCollection<string>(SnapshotAliases(aliases, nameof(aliases), CanonicalId));
    }

    public string CanonicalId { get; }

    public string Name => CanonicalId;

    public IReadOnlyList<string> Aliases => _aliases;

    public IReadOnlyList<string> AllNames => [CanonicalId, .. _aliases];

    public Type ImplementationType { get; }

    public bool IsFrontendModule => typeof(IFrontendCoreModule).IsAssignableFrom(ImplementationType);

    public bool IsIrProcessingModule => typeof(IIRProcessingModule).IsAssignableFrom(ImplementationType);

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
