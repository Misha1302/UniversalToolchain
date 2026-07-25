using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.FeatureSdk;

/// <summary>
/// Explicit, fail-closed package registry. Package registration is transactional and never scans loaded assemblies.
/// </summary>
public sealed class LanguagePackageRegistry
{
    private readonly Dictionary<LanguageFeatureId, (RegisteredPackage Registration, LanguageFeatureDescriptor Feature)> _features = [];
    private readonly Dictionary<LanguageContributionId, (RegisteredPackage Registration, LanguageContributionDescriptor Contribution)> _contributions = [];
    private readonly Dictionary<LanguageCapabilityId, List<LanguageContributionId>> _capabilityProviders = [];
    private readonly Dictionary<LanguageContributionId, List<LanguageFeatureId>> _contributionOwners = [];
    private readonly Dictionary<LanguagePackageId, RegisteredPackage> _packages = [];

    public IReadOnlyCollection<LanguagePackageDescriptor> Packages =>
        _packages.Values.Select(static registration => registration.Descriptor)
            .OrderBy(static descriptor => descriptor.Id.Value, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyCollection<LanguageContributionDescriptor> Contributions =>
        _contributions.Values.Select(static x => x.Contribution)
            .OrderBy(static x => x.Id.Value, StringComparer.Ordinal)
            .ToArray();

    public LanguagePackageRegistry AddPackage(ILanguageFeaturePackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        return AddPackage(package.Descriptor, package.GetType());
    }

    public LanguagePackageRegistry AddPackage(LanguagePackageDescriptor package)
    {
        ArgumentNullException.ThrowIfNull(package);
        return AddPackage(package, implementationType: null);
    }

    private LanguagePackageRegistry AddPackage(LanguagePackageDescriptor package, Type? implementationType)
    {
        if (_packages.TryGetValue(package.Id, out var existing))
        {
            if (existing.Descriptor.Version == package.Version &&
                ReferenceEquals(existing.Descriptor, package) &&
                existing.Identity.ImplementationType == implementationType)
            {
                return this;
            }

            throw new InvalidOperationException(
                $"Package '{package.Id.Value}' is already registered as version '{existing.Descriptor.Version.Value}' " +
                $"with implementation provenance '{existing.Identity.ImplementationType?.FullName ?? "descriptor-only"}'.");
        }

        var duplicateFeature = package.Features.FirstOrDefault(feature => _features.ContainsKey(feature.Id));
        if (duplicateFeature != null)
        {
            var owner = _features[duplicateFeature.Id].Registration.Descriptor;
            throw new InvalidOperationException(
                $"Feature '{duplicateFeature.Id.Value}' is already owned by package '{owner.Id.Value}' version '{owner.Version.Value}'.");
        }

        var duplicateContribution = package.Contributions.FirstOrDefault(contribution => _contributions.ContainsKey(contribution.Id));
        if (duplicateContribution != null)
        {
            var owner = _contributions[duplicateContribution.Id].Registration.Descriptor;
            throw new InvalidOperationException(
                $"Contribution '{duplicateContribution.Id.Value}' is already owned by package '{owner.Id.Value}' version '{owner.Version.Value}'.");
        }

        var registration = new RegisteredPackage(
            package,
            new LanguagePackageRegistrationIdentity(package, implementationType));
        _packages.Add(package.Id, registration);
        foreach (var feature in package.Features)
            _features.Add(feature.Id, (registration, feature));
        foreach (var contribution in package.Contributions)
        {
            _contributions.Add(contribution.Id, (registration, contribution));
            _contributionOwners.Add(
                contribution.Id,
                package.Features.Where(feature => feature.Contributions.Contains(contribution.Id))
                    .Select(static feature => feature.Id)
                    .OrderBy(static id => id.Value, StringComparer.Ordinal)
                    .ToList());
            foreach (var capability in contribution.ProvidesCapabilities)
            {
                if (!_capabilityProviders.TryGetValue(capability, out var providers))
                {
                    providers = [];
                    _capabilityProviders.Add(capability, providers);
                }
                providers.Add(contribution.Id);
                providers.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Value, right.Value));
            }
        }
        return this;
    }

    public bool TryGetFeature(
        LanguageFeatureId id,
        out LanguagePackageDescriptor package,
        out LanguageFeatureDescriptor feature)
    {
        if (TryGetFeatureRegistration(id, out package, out feature, out _))
            return true;
        package = null!;
        feature = null!;
        return false;
    }

    public bool TryGetFeatureRegistration(
        LanguageFeatureId id,
        out LanguagePackageDescriptor package,
        out LanguageFeatureDescriptor feature,
        out LanguagePackageRegistrationIdentity identity)
    {
        if (_features.TryGetValue(id, out var value))
        {
            package = value.Registration.Descriptor;
            feature = value.Feature;
            identity = value.Registration.Identity;
            return true;
        }
        package = null!;
        feature = null!;
        identity = null!;
        return false;
    }

    public bool TryGetContribution(
        LanguageContributionId id,
        out LanguagePackageDescriptor package,
        out LanguageContributionDescriptor contribution)
    {
        if (TryGetContributionRegistration(id, out package, out contribution, out _))
            return true;
        package = null!;
        contribution = null!;
        return false;
    }

    public bool TryGetContributionRegistration(
        LanguageContributionId id,
        out LanguagePackageDescriptor package,
        out LanguageContributionDescriptor contribution,
        out LanguagePackageRegistrationIdentity identity)
    {
        if (_contributions.TryGetValue(id, out var value))
        {
            package = value.Registration.Descriptor;
            contribution = value.Contribution;
            identity = value.Registration.Identity;
            return true;
        }
        package = null!;
        contribution = null!;
        identity = null!;
        return false;
    }

    public IReadOnlyList<LanguageFeatureId> GetContributionOwners(LanguageContributionId contributionId) =>
        _contributionOwners.TryGetValue(contributionId, out var owners) ? owners.ToArray() : [];

    public bool IsContributionEligible(
        LanguageContributionId contributionId,
        IReadOnlySet<LanguageFeatureId> selectedFeatures)
    {
        ArgumentNullException.ThrowIfNull(selectedFeatures);
        return !_contributionOwners.TryGetValue(contributionId, out var owners) ||
               owners.Count == 0 ||
               owners.Any(selectedFeatures.Contains);
    }

    public IReadOnlyList<LanguageContributionId> GetCapabilityProviders(LanguageCapabilityId capability) =>
        _capabilityProviders.TryGetValue(capability, out var providers)
            ? providers.ToArray()
            : [];

    public IReadOnlyList<(LanguagePackageDescriptor Package, LanguageContributionDescriptor Contribution)> GetRuntimeProviderContributions() =>
        GetRuntimeProviderRegistrations()
            .Select(static item => (item.Package, item.Contribution))
            .ToArray();

    public IReadOnlyList<(
        LanguagePackageDescriptor Package,
        LanguageContributionDescriptor Contribution,
        LanguagePackageRegistrationIdentity Identity)> GetRuntimeProviderRegistrations() =>
        _contributions.Values
            .Where(static x => x.Contribution.RuntimeProviderId != null)
            .OrderBy(static x => x.Contribution.RuntimeProviderId!.Value.Value, StringComparer.Ordinal)
            .ThenBy(static x => x.Contribution.Id.Value, StringComparer.Ordinal)
            .Select(static x => (x.Registration.Descriptor, x.Contribution, x.Registration.Identity))
            .ToArray();

    private sealed record RegisteredPackage(
        LanguagePackageDescriptor Descriptor,
        LanguagePackageRegistrationIdentity Identity);
}
