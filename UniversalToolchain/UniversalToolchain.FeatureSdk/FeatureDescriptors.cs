using System.Collections.ObjectModel;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.FeatureSdk;

public enum LanguageSlotMultiplicity
{
    Single,
    Many
}

public enum ContributionMergePolicy
{
    Add,
    Replace,
    Decorate,
    RejectDuplicate
}

public sealed class ArtifactTransformationDescriptor
{
    public ArtifactTransformationDescriptor(
        LanguageArtifactContract source,
        LanguageArtifactContract target,
        int cost = 100)
    {
        if (cost < 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "Transformation cost must not be negative.");
        SourceContract = source;
        TargetContract = target;
        Cost = cost;
    }

    public LanguageArtifactKindId Source => SourceContract.Kind;
    public LanguageArtifactKindId Target => TargetContract.Kind;
    public LanguageArtifactContract SourceContract { get; }
    public LanguageArtifactContract TargetContract { get; }
    public int Cost { get; }
    public bool IsTyped => SourceContract.IsTyped && TargetContract.IsTyped;
    public bool IsPass => SourceContract == TargetContract;

    public static ArtifactTransformationDescriptor Create<TSource, TTarget>(
        LanguageArtifactKind<TSource> source,
        LanguageArtifactKind<TTarget> target,
        int cost = 100)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return new ArtifactTransformationDescriptor(source.Contract, target.Contract, cost);
    }
}

public sealed class LanguageContributionDescriptor
{
    public LanguageContributionDescriptor(
        LanguageContributionId id,
        LanguageSlotId slot,
        LanguageSlotMultiplicity multiplicity = LanguageSlotMultiplicity.Many,
        ContributionMergePolicy mergePolicy = ContributionMergePolicy.Add,
        IEnumerable<LanguageContributionId>? requiresContributions = null,
        IEnumerable<LanguageCapabilityId>? providesCapabilities = null,
        IEnumerable<LanguageCapabilityId>? requiresCapabilities = null,
        IEnumerable<LanguageContributionId>? conflicts = null,
        IEnumerable<LanguageCapabilityId>? conflictsCapabilities = null,
        IEnumerable<BackendId>? supportedBackends = null,
        ArtifactTransformationDescriptor? transformation = null,
        LanguageRuntimeProviderId? runtimeProviderId = null,
        LanguageVersion? runtimeProviderVersion = null,
        int order = 0,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyDictionary<BackendId, LanguageArtifactContract>? runtimeInputContracts = null,
        LanguageArtifactContract? backendInputContract = null,
        IEnumerable<LanguageContributionId>? beforeContributions = null,
        IEnumerable<LanguageContributionId>? afterContributions = null)
    {
        Id = id;
        Slot = slot;
        Multiplicity = multiplicity;
        MergePolicy = mergePolicy;
        RequiresContributions = Snapshot(requiresContributions);
        ProvidesCapabilities = Snapshot(providesCapabilities);
        RequiresCapabilities = Snapshot(requiresCapabilities);
        Conflicts = Snapshot(conflicts);
        ConflictsCapabilities = Snapshot(conflictsCapabilities);
        SupportedBackends = Snapshot(supportedBackends);
        Transformation = transformation;
        RuntimeProviderId = runtimeProviderId;
        RuntimeProviderVersion = runtimeProviderVersion;
        RuntimeInputContracts = SnapshotRuntimeInputContracts(runtimeInputContracts);
        BackendInputContract = backendInputContract;
        BeforeContributions = Snapshot(beforeContributions);
        AfterContributions = Snapshot(afterContributions);
        Order = order;
        Metadata = SnapshotDictionary(metadata);

        if (multiplicity == LanguageSlotMultiplicity.Single && mergePolicy is ContributionMergePolicy.Add or ContributionMergePolicy.Decorate)
            throw new ArgumentException("A single-owner slot must reject duplicates or use an explicit replacement policy.", nameof(mergePolicy));
        if (runtimeProviderId.HasValue != runtimeProviderVersion.HasValue)
            throw new ArgumentException("Runtime provider ID and version must be declared together.", nameof(runtimeProviderVersion));
        if (runtimeProviderId == null && RuntimeInputContracts.Count != 0)
            throw new ArgumentException("Runtime inputs may be declared only by a runtime-provider contribution.", nameof(runtimeInputContracts));
        if (backendInputContract != null && slot != LanguageSlots.Backends)
            throw new ArgumentException("A backend input contract may be declared only by a backend contribution.", nameof(backendInputContract));
        if (mergePolicy == ContributionMergePolicy.Decorate && transformation?.IsPass != true)
            throw new ArgumentException("Decorate contributions must be same-contract artifact passes.", nameof(mergePolicy));
        if (BeforeContributions.Contains(id) || AfterContributions.Contains(id))
            throw new ArgumentException("A contribution cannot order itself before or after itself.", nameof(beforeContributions));
    }

    public LanguageContributionId Id { get; }
    public LanguageSlotId Slot { get; }
    public LanguageSlotMultiplicity Multiplicity { get; }
    public ContributionMergePolicy MergePolicy { get; }
    public IReadOnlyList<LanguageContributionId> RequiresContributions { get; }
    public IReadOnlyList<LanguageCapabilityId> ProvidesCapabilities { get; }
    public IReadOnlyList<LanguageCapabilityId> RequiresCapabilities { get; }
    public IReadOnlyList<LanguageContributionId> Conflicts { get; }
    public IReadOnlyList<LanguageCapabilityId> ConflictsCapabilities { get; }
    public IReadOnlyList<BackendId> SupportedBackends { get; }
    public ArtifactTransformationDescriptor? Transformation { get; }
    public LanguageRuntimeProviderId? RuntimeProviderId { get; }
    public LanguageVersion? RuntimeProviderVersion { get; }
    public IReadOnlyDictionary<BackendId, LanguageArtifactContract> RuntimeInputContracts { get; }
    public LanguageArtifactContract? BackendInputContract { get; }
    public IReadOnlyList<LanguageContributionId> BeforeContributions { get; }
    public IReadOnlyList<LanguageContributionId> AfterContributions { get; }
    public int Order { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T>? values) where T : notnull =>
        new ReadOnlyCollection<T>((values ?? []).Distinct().OrderBy(static x => x.ToString(), StringComparer.Ordinal).ToList());

    private static IReadOnlyDictionary<BackendId, LanguageArtifactContract> SnapshotRuntimeInputContracts(
        IReadOnlyDictionary<BackendId, LanguageArtifactContract>? typedInputs)
    {
        var result = new Dictionary<BackendId, LanguageArtifactContract>();
        foreach (var pair in typedInputs ?? new Dictionary<BackendId, LanguageArtifactContract>())
            result.Add(pair.Key, pair.Value);
        return new ReadOnlyDictionary<BackendId, LanguageArtifactContract>(result);
    }

    private static IReadOnlyDictionary<TKey, TValue> SnapshotDictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue>? values)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        foreach (var pair in values ?? new Dictionary<TKey, TValue>())
            result.Add(pair.Key, pair.Value);
        return new ReadOnlyDictionary<TKey, TValue>(result);
    }
}

public sealed class LanguageFeatureDescriptor
{
    public LanguageFeatureDescriptor(
        LanguageFeatureId id,
        IEnumerable<LanguageFeatureId>? requires = null,
        IEnumerable<LanguageFeatureId>? conflicts = null,
        IEnumerable<BackendId>? supportedBackends = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        IEnumerable<LanguageContributionId>? contributions = null)
    {
        Id = id;
        Requires = Snapshot(requires);
        Conflicts = Snapshot(conflicts);
        SupportedBackends = Snapshot(supportedBackends);
        Contributions = Snapshot(contributions);
        Metadata = SnapshotDictionary(metadata);
    }

    public LanguageFeatureId Id { get; }
    public IReadOnlyList<LanguageFeatureId> Requires { get; }
    public IReadOnlyList<LanguageFeatureId> Conflicts { get; }
    public IReadOnlyList<BackendId> SupportedBackends { get; }
    public IReadOnlyList<LanguageContributionId> Contributions { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T>? values) where T : notnull =>
        new ReadOnlyCollection<T>((values ?? []).Distinct().OrderBy(static x => x.ToString(), StringComparer.Ordinal).ToList());

    private static IReadOnlyDictionary<string, string> SnapshotDictionary(IReadOnlyDictionary<string, string>? values)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in values ?? new Dictionary<string, string>())
            result.Add(pair.Key, pair.Value);
        return new ReadOnlyDictionary<string, string>(result);
    }
}

public sealed class LanguagePackageDescriptor
{
    public LanguagePackageDescriptor(
        LanguagePackageId id,
        LanguageVersion version,
        ToolchainApiVersion toolchainApiVersion,
        IEnumerable<LanguageFeatureDescriptor> features,
        IReadOnlyDictionary<string, string>? metadata = null,
        IEnumerable<LanguageContributionDescriptor>? contributions = null)
    {
        Id = id;
        Version = version;
        ToolchainApiVersion = toolchainApiVersion;
        Features = new ReadOnlyCollection<LanguageFeatureDescriptor>(
            features?.OrderBy(static x => x.Id.Value, StringComparer.Ordinal).ToList()
            ?? throw new ArgumentNullException(nameof(features)));
        Contributions = new ReadOnlyCollection<LanguageContributionDescriptor>(
            (contributions ?? []).OrderBy(static x => x.Id.Value, StringComparer.Ordinal).ToList());
        if (Features.Count == 0 && Contributions.Count == 0)
            throw new ArgumentException("A language package must declare at least one feature or contribution.", nameof(features));
        if (Features.Select(static x => x.Id).Distinct().Count() != Features.Count)
            throw new ArgumentException("A feature package contains duplicate feature IDs.", nameof(features));
        if (Contributions.Select(static x => x.Id).Distinct().Count() != Contributions.Count)
            throw new ArgumentException("A language package contains duplicate contribution IDs.", nameof(contributions));

        var knownContributions = Contributions.Select(static x => x.Id).ToHashSet();
        var unknownFeatureContribution = Features
            .SelectMany(static x => x.Contributions)
            .FirstOrDefault(id => !knownContributions.Contains(id));
        if (unknownFeatureContribution != default)
            throw new ArgumentException(
                $"Feature refers to contribution '{unknownFeatureContribution.Value}' that is not declared by its package.",
                nameof(contributions));

        var metadataSnapshot = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in metadata ?? new Dictionary<string, string>())
            metadataSnapshot.Add(pair.Key, pair.Value);
        Metadata = new ReadOnlyDictionary<string, string>(metadataSnapshot);
    }

    public LanguagePackageId Id { get; }
    public LanguageVersion Version { get; }
    public ToolchainApiVersion ToolchainApiVersion { get; }
    public IReadOnlyList<LanguageFeatureDescriptor> Features { get; }
    public IReadOnlyList<LanguageContributionDescriptor> Contributions { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }
}

public interface ILanguageFeaturePackage
{
    LanguagePackageDescriptor Descriptor { get; }
}

public interface ILanguageExtensionPackage : ILanguageFeaturePackage
{
}
