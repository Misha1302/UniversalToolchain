using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.FeatureSdk;

/// <summary>
/// Explicit, fail-closed package registry. Package registration is transactional and never scans loaded assemblies.
/// </summary>
public sealed class LanguagePackageRegistry
{
    private readonly Dictionary<LanguageFeatureId, (LanguagePackageDescriptor Package, LanguageFeatureDescriptor Feature)> _features = [];
    private readonly Dictionary<LanguageContributionId, (LanguagePackageDescriptor Package, LanguageContributionDescriptor Contribution)> _contributions = [];
    private readonly Dictionary<LanguageCapabilityId, List<LanguageContributionId>> _capabilityProviders = [];
    private readonly Dictionary<LanguageContributionId, List<LanguageFeatureId>> _contributionOwners = [];
    private readonly Dictionary<LanguagePackageId, LanguagePackageDescriptor> _packages = [];

    public IReadOnlyCollection<LanguagePackageDescriptor> Packages =>
        _packages.Values.OrderBy(static x => x.Id.Value, StringComparer.Ordinal).ToArray();

    public IReadOnlyCollection<LanguageContributionDescriptor> Contributions =>
        _contributions.Values.Select(static x => x.Contribution)
            .OrderBy(static x => x.Id.Value, StringComparer.Ordinal)
            .ToArray();

    public LanguagePackageRegistry AddPackage(ILanguageFeaturePackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        return AddPackage(package.Descriptor);
    }

    public LanguagePackageRegistry AddPackage(LanguagePackageDescriptor package)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (_packages.TryGetValue(package.Id, out var existing))
        {
            if (existing.Version == package.Version && ReferenceEquals(existing, package))
                return this;
            throw new InvalidOperationException($"Package '{package.Id.Value}' is already registered as version '{existing.Version.Value}'.");
        }

        var duplicateFeature = package.Features.FirstOrDefault(feature => _features.ContainsKey(feature.Id));
        if (duplicateFeature != null)
        {
            var owner = _features[duplicateFeature.Id].Package;
            throw new InvalidOperationException(
                $"Feature '{duplicateFeature.Id.Value}' is already owned by package '{owner.Id.Value}' version '{owner.Version.Value}'.");
        }

        var duplicateContribution = package.Contributions.FirstOrDefault(contribution => _contributions.ContainsKey(contribution.Id));
        if (duplicateContribution != null)
        {
            var owner = _contributions[duplicateContribution.Id].Package;
            throw new InvalidOperationException(
                $"Contribution '{duplicateContribution.Id.Value}' is already owned by package '{owner.Id.Value}' version '{owner.Version.Value}'.");
        }

        _packages.Add(package.Id, package);
        foreach (var feature in package.Features)
            _features.Add(feature.Id, (package, feature));
        foreach (var contribution in package.Contributions)
        {
            _contributions.Add(contribution.Id, (package, contribution));
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
        if (_features.TryGetValue(id, out var value))
        {
            package = value.Package;
            feature = value.Feature;
            return true;
        }
        package = null!;
        feature = null!;
        return false;
    }

    public bool TryGetContribution(
        LanguageContributionId id,
        out LanguagePackageDescriptor package,
        out LanguageContributionDescriptor contribution)
    {
        if (_contributions.TryGetValue(id, out var value))
        {
            package = value.Package;
            contribution = value.Contribution;
            return true;
        }
        package = null!;
        contribution = null!;
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
        _contributions.Values
            .Where(static x => x.Contribution.RuntimeProviderId != null)
            .OrderBy(static x => x.Contribution.RuntimeProviderId!.Value.Value, StringComparer.Ordinal)
            .ThenBy(static x => x.Contribution.Id.Value, StringComparer.Ordinal)
            .ToArray();
}
