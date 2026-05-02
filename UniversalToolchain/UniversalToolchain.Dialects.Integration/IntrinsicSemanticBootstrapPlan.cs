using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Immutable startup contract for intrinsic provider registration and coverage validation.
/// </summary>
public sealed class IntrinsicSemanticBootstrapPlan
{
    private readonly ReadOnlyCollection<IntrinsicDescriptorProviderRegistration> _providerRegistrations;
    private readonly ReadOnlyCollection<IntrinsicProviderRequirement> _requirements;

    public IntrinsicSemanticBootstrapPlan(
        IEnumerable<IntrinsicDescriptorProviderRegistration> providerRegistrations,
        IEnumerable<IntrinsicProviderRequirement> requirements)
    {
        _providerRegistrations = new ReadOnlyCollection<IntrinsicDescriptorProviderRegistration>(
            SnapshotRegistrations(providerRegistrations, nameof(providerRegistrations)));
        _requirements = new ReadOnlyCollection<IntrinsicProviderRequirement>(SnapshotRequirements(requirements, nameof(requirements)));
    }

    public IReadOnlyList<IntrinsicDescriptorProviderRegistration> ProviderRegistrations => _providerRegistrations;

    public IReadOnlyList<IntrinsicProviderRequirement> Requirements => _requirements;

    public IReadOnlyList<Type> GetPreBuildResolvableProviderTypes()
    {
        return _providerRegistrations
            .Where(static x => x.CanValidateBeforeProviderBuild)
            .Select(static x => x.ProviderType)
            .Where(static x => x != null)
            .Cast<Type>()
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static List<IntrinsicDescriptorProviderRegistration> SnapshotRegistrations(
        IEnumerable<IntrinsicDescriptorProviderRegistration> values,
        string paramName)
    {
        return values
            .Select(x => x.NotNull(paramName))
            .OrderBy(x => x.RegistrationIndex)
            .ThenBy(x => x.Kind)
            .ThenBy(x => x.ProviderType?.FullName, StringComparer.Ordinal)
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