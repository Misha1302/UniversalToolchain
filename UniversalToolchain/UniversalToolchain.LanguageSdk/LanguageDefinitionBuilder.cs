using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

public sealed class LanguageDefinitionBuilder
{
    private readonly SortedSet<BackendId> _backends = new(Comparer<BackendId>.Create(static (a, b) => StringComparer.Ordinal.Compare(a.Value, b.Value)));
    private readonly SortedSet<LanguageFeatureId> _features = new(Comparer<LanguageFeatureId>.Create(static (a, b) => StringComparer.Ordinal.Compare(a.Value, b.Value)));
    private readonly SortedDictionary<string, string> _metadata = new(StringComparer.Ordinal);
    private readonly SortedDictionary<LanguageSlotId, LanguageSlotOverride> _slotOverrides = new(Comparer<LanguageSlotId>.Create(static (a, b) => StringComparer.Ordinal.Compare(a.Value, b.Value)));
    private readonly SortedDictionary<LanguageCapabilityId, LanguageContributionId> _capabilityProviders = new(Comparer<LanguageCapabilityId>.Create(static (a, b) => StringComparer.Ordinal.Compare(a.Value, b.Value)));
    private readonly SortedSet<LanguageContributionId> _excludedContributions = new(Comparer<LanguageContributionId>.Create(static (a, b) => StringComparer.Ordinal.Compare(a.Value, b.Value)));
    private readonly LanguageId _id;
    private readonly LanguageVersion _version;
    private LanguageRuntimeProviderReference? _runtimeProvider;
    private LanguageRuntimePolicy _runtimePolicy = LanguageRuntimePolicy.Default;
    private ToolchainApiVersion _toolchainApi = ToolchainApi.Current;
    private LanguageArtifactContract _entryArtifact = StandardLanguageArtifactKinds.SourceText.Contract;

    private LanguageDefinitionBuilder(LanguageId id, LanguageVersion version)
    {
        _id = id;
        _version = version;
    }

    public static LanguageDefinitionBuilder Create(string id, string version) =>
        new(new LanguageId(id), new LanguageVersion(version));

    public LanguageDefinitionBuilder UseFeature(string id) => UseFeature(new LanguageFeatureId(id));

    public LanguageDefinitionBuilder UseFeature(LanguageFeatureId id)
    {
        _features.Add(id);
        return this;
    }

    public LanguageDefinitionBuilder WithEntryArtifact<T>(LanguageArtifactKind<T> artifactKind)
    {
        ArgumentNullException.ThrowIfNull(artifactKind);
        _entryArtifact = artifactKind.Contract;
        return this;
    }

    public LanguageDefinitionBuilder WithEntryArtifact(LanguageArtifactContract artifactContract)
    {
        _entryArtifact = artifactContract;
        return this;
    }

    public LanguageDefinitionBuilder EnableBackend(string id) => EnableBackend(new BackendId(id));

    public LanguageDefinitionBuilder EnableBackend(BackendId id)
    {
        _backends.Add(id);
        return this;
    }

    public LanguageDefinitionBuilder UseRuntimeProvider(string providerId, string version) =>
        UseRuntimeProvider(new LanguageRuntimeProviderId(providerId), new LanguageVersion(version));

    public LanguageDefinitionBuilder UseRuntimeProvider(LanguageRuntimeProviderId providerId, LanguageVersion version)
    {
        _runtimeProvider = new LanguageRuntimeProviderReference(providerId, version);
        return this;
    }

    public LanguageDefinitionBuilder PreferCapabilityProvider(
        LanguageCapabilityId capability,
        LanguageContributionId contribution)
    {
        _capabilityProviders[capability] = contribution;
        return this;
    }

    public LanguageDefinitionBuilder ReplaceSlot(
        LanguageSlotId slot,
        LanguageContributionId contribution,
        LanguageContributionId? expectedCurrentOwner = null)
    {
        _slotOverrides[slot] = new LanguageSlotOverride(slot, contribution, expectedCurrentOwner);
        return this;
    }

    public LanguageDefinitionBuilder ExcludeContribution(LanguageContributionId contribution)
    {
        _excludedContributions.Add(contribution);
        return this;
    }

    public LanguageDefinitionBuilder TargetToolchainApi(int major)
    {
        _toolchainApi = new ToolchainApiVersion(major);
        return this;
    }

    public LanguageDefinitionBuilder WithRuntimePolicy(LanguageRuntimePolicy policy)
    {
        _runtimePolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        return this;
    }

    public LanguageDefinitionBuilder WithMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key must not be empty.", nameof(key));
        _metadata[key] = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    public LanguageDefinition Build() => new(
        _id,
        _version,
        _toolchainApi,
        _features,
        _backends,
        _runtimeProvider,
        _runtimePolicy,
        _metadata,
        _slotOverrides.Values,
        _capabilityProviders,
        _excludedContributions,
        _entryArtifact);
}
