using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Immutable startup contract for intrinsic provider registration and coverage validation.
/// </summary>
public sealed class IntrinsicSemanticBootstrapPlan
{
    private readonly ReadOnlyCollection<Type> _registeredProviderTypes;
    private readonly ReadOnlyCollection<IntrinsicProviderRequirement> _requirements;

    public IntrinsicSemanticBootstrapPlan(
        IEnumerable<Type> registeredProviderTypes,
        IEnumerable<IntrinsicProviderRequirement> requirements)
    {
        _registeredProviderTypes = new ReadOnlyCollection<Type>(SnapshotTypes(registeredProviderTypes, nameof(registeredProviderTypes)));
        _requirements = new ReadOnlyCollection<IntrinsicProviderRequirement>(SnapshotRequirements(requirements, nameof(requirements)));
    }

    public IReadOnlyList<Type> RegisteredProviderTypes => _registeredProviderTypes;

    public IReadOnlyList<IntrinsicProviderRequirement> Requirements => _requirements;

    private static List<Type> SnapshotTypes(IEnumerable<Type> values, string paramName)
    {
        return values
            .Select(x => x.NotNull(paramName))
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static List<IntrinsicProviderRequirement> SnapshotRequirements(IEnumerable<IntrinsicProviderRequirement> values, string paramName)
    {
        return values
            .Select(x => x.NotNull(paramName))
            .Distinct()
            .OrderBy(x => x.ModuleType.FullName, StringComparer.Ordinal)
            .ThenBy(x => x.ProviderType.FullName, StringComparer.Ordinal)
            .ToList();
    }
}
