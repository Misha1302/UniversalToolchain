using System.Collections.ObjectModel;

namespace UniversalToolchain.Language.Abstractions;

public enum LanguageDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record LanguageDiagnostic(
    string Code,
    LanguageDiagnosticSeverity Severity,
    string Stage,
    string Message,
    string? Owner = null,
    string? Hint = null);

public sealed record LanguageRuntimePolicy
{
    public LanguageRuntimePolicy(
        bool RequireDeterminism = false,
        bool AllowHostInterop = false,
        int? MaximumSourceLength = null,
        int? MaximumExternalParameters = null)
    {
        if (MaximumSourceLength < 0)
            throw new ArgumentOutOfRangeException(nameof(MaximumSourceLength), "Maximum source length must not be negative.");
        if (MaximumExternalParameters < 0)
            throw new ArgumentOutOfRangeException(nameof(MaximumExternalParameters), "Maximum external parameter count must not be negative.");

        this.RequireDeterminism = RequireDeterminism;
        this.AllowHostInterop = AllowHostInterop;
        this.MaximumSourceLength = MaximumSourceLength;
        this.MaximumExternalParameters = MaximumExternalParameters;
    }

    public bool RequireDeterminism { get; }
    public bool AllowHostInterop { get; }
    public int? MaximumSourceLength { get; }
    public int? MaximumExternalParameters { get; }
    public static LanguageRuntimePolicy Default { get; } = new();
}

[Obsolete("Use LanguageRuntimeProviderReference. Runtime packs remain a compatibility surface for the alpha.")]
public sealed record LanguageRuntimePackReference(LanguagePackageId PackageId, LanguageVersion Version);

public sealed record LanguageRuntimeProviderReference(
    LanguageRuntimeProviderId ProviderId,
    LanguageVersion Version);

public sealed record LanguageSlotOverride(
    LanguageSlotId Slot,
    LanguageContributionId Contribution,
    LanguageContributionId? ExpectedCurrentOwner = null);

public sealed class LanguageDefinition
{
    public LanguageDefinition(
        LanguageId id,
        LanguageVersion version,
        ToolchainApiVersion toolchainApiVersion,
        IEnumerable<LanguageFeatureId> selectedFeatures,
        IEnumerable<BackendId> backends,
        LanguageRuntimeProviderReference? runtimeProvider = null,
        LanguageRuntimePolicy? runtimePolicy = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        IEnumerable<LanguageSlotOverride>? slotOverrides = null,
        IReadOnlyDictionary<LanguageCapabilityId, LanguageContributionId>? capabilityProviders = null,
        IEnumerable<LanguageContributionId>? excludedContributions = null,
        LanguageArtifactContract? entryArtifact = null)
    {
        Id = id;
        Version = version;
        ToolchainApiVersion = toolchainApiVersion;
        RuntimeProvider = runtimeProvider;
        SelectedFeatures = SnapshotDistinct(selectedFeatures, nameof(selectedFeatures));
        Backends = SnapshotDistinct(backends, nameof(backends));
        RuntimePolicy = runtimePolicy ?? LanguageRuntimePolicy.Default;
        Metadata = SnapshotDictionary(metadata);
        SlotOverrides = SnapshotOverrides(slotOverrides);
        CapabilityProviders = SnapshotDictionary(capabilityProviders);
        ExcludedContributions = SnapshotDistinct(excludedContributions ?? [], nameof(excludedContributions));
        EntryArtifact = entryArtifact ?? StandardLanguageArtifactKinds.SourceText.Contract;

        if (SelectedFeatures.Count == 0 && Backends.Count == 0)
            throw new ArgumentException(
                "A language definition must select at least one feature or one executable backend.",
                nameof(selectedFeatures));
        if (Backends.Count == 0 && RuntimeProvider != null)
            throw new ArgumentException("A runtime provider cannot be selected for a planning-only language definition.", nameof(runtimeProvider));
    }

#pragma warning disable CS0618
    public LanguageDefinition(
        LanguageId id,
        LanguageVersion version,
        ToolchainApiVersion toolchainApiVersion,
        IEnumerable<LanguageFeatureId> selectedFeatures,
        IEnumerable<BackendId> backends,
        LanguageRuntimePackReference runtimePack,
        LanguageRuntimePolicy? runtimePolicy = null,
        IReadOnlyDictionary<string, string>? metadata = null)
        : this(
            id,
            version,
            toolchainApiVersion,
            selectedFeatures,
            backends,
            new LanguageRuntimeProviderReference(new LanguageRuntimeProviderId(runtimePack.PackageId.Value), runtimePack.Version),
            runtimePolicy,
            metadata)
    {
    }
#pragma warning restore CS0618

    public LanguageId Id { get; }
    public LanguageVersion Version { get; }
    public ToolchainApiVersion ToolchainApiVersion { get; }
    public IReadOnlyList<LanguageFeatureId> SelectedFeatures { get; }
    public IReadOnlyList<BackendId> Backends { get; }
    public LanguageRuntimeProviderReference? RuntimeProvider { get; }
    public LanguageRuntimePolicy RuntimePolicy { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }
    public IReadOnlyList<LanguageSlotOverride> SlotOverrides { get; }
    public IReadOnlyDictionary<LanguageCapabilityId, LanguageContributionId> CapabilityProviders { get; }
    public IReadOnlyList<LanguageContributionId> ExcludedContributions { get; }
    public LanguageArtifactContract EntryArtifact { get; }
    public bool IsExecutable => Backends.Count != 0;

    private static IReadOnlyList<T> SnapshotDistinct<T>(IEnumerable<T> values, string paramName)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(values, paramName);
        var result = new List<T>();
        var seen = new HashSet<T>();
        foreach (var value in values)
        {
            if (seen.Add(value))
                result.Add(value);
        }
        return new ReadOnlyCollection<T>(result);
    }

    private static IReadOnlyDictionary<TKey, TValue> SnapshotDictionary<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue>? values)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        foreach (var pair in values ?? new Dictionary<TKey, TValue>())
            result.Add(pair.Key, pair.Value);
        return new ReadOnlyDictionary<TKey, TValue>(result);
    }

    private static IReadOnlyList<LanguageSlotOverride> SnapshotOverrides(IEnumerable<LanguageSlotOverride>? values)
    {
        var result = (values ?? [])
            .OrderBy(static x => x.Slot.Value, StringComparer.Ordinal)
            .ToArray();
        if (result.Select(static x => x.Slot).Distinct().Count() != result.Length)
            throw new ArgumentException("Only one explicit override may be declared per language slot.", nameof(values));
        return new ReadOnlyCollection<LanguageSlotOverride>(result);
    }
}

public sealed record LanguagePlanSummary(
    LanguageId LanguageId,
    LanguageVersion LanguageVersion,
    string PlanHash,
    IReadOnlyList<LanguageFeatureId> Features,
    IReadOnlyList<BackendId> Backends,
    LanguageRuntimeProviderReference? RuntimeProvider,
    LanguageArtifactContract EntryArtifact);
