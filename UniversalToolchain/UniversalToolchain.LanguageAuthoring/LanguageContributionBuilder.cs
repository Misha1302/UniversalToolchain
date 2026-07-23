using System.Collections.ObjectModel;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageAuthoring;

/// <summary>
/// Configures graph requirements, conflicts, ordering, capabilities and metadata for one contribution.
/// </summary>
public sealed class LanguageContributionBuilder
{
    private readonly List<LanguageContributionId> _requiresContributions = [];
    private readonly List<LanguageCapabilityId> _providesCapabilities = [];
    private readonly List<LanguageCapabilityId> _requiresCapabilities = [];
    private readonly List<LanguageContributionId> _conflicts = [];
    private readonly List<LanguageCapabilityId> _conflictsCapabilities = [];
    private readonly List<LanguageContributionId> _beforeContributions = [];
    private readonly List<LanguageContributionId> _afterContributions = [];
    private readonly SortedDictionary<string, string> _metadata = new(StringComparer.Ordinal);

    public LanguageContributionBuilder RequiresContributions(params LanguageContributionId[] contributions)
    {
        _requiresContributions.AddRange(contributions ?? throw new ArgumentNullException(nameof(contributions)));
        return this;
    }

    public LanguageContributionBuilder ProvidesCapabilities(params LanguageCapabilityId[] capabilities)
    {
        _providesCapabilities.AddRange(capabilities ?? throw new ArgumentNullException(nameof(capabilities)));
        return this;
    }

    public LanguageContributionBuilder RequiresCapabilities(params LanguageCapabilityId[] capabilities)
    {
        _requiresCapabilities.AddRange(capabilities ?? throw new ArgumentNullException(nameof(capabilities)));
        return this;
    }

    public LanguageContributionBuilder ConflictsWith(params LanguageContributionId[] contributions)
    {
        _conflicts.AddRange(contributions ?? throw new ArgumentNullException(nameof(contributions)));
        return this;
    }

    public LanguageContributionBuilder ConflictsWithCapabilities(params LanguageCapabilityId[] capabilities)
    {
        _conflictsCapabilities.AddRange(capabilities ?? throw new ArgumentNullException(nameof(capabilities)));
        return this;
    }

    public LanguageContributionBuilder Before(params LanguageContributionId[] contributions)
    {
        _beforeContributions.AddRange(contributions ?? throw new ArgumentNullException(nameof(contributions)));
        return this;
    }

    public LanguageContributionBuilder After(params LanguageContributionId[] contributions)
    {
        _afterContributions.AddRange(contributions ?? throw new ArgumentNullException(nameof(contributions)));
        return this;
    }

    public LanguageContributionBuilder WithMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key must not be empty.", nameof(key));
        _metadata[key] = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    internal IReadOnlyList<LanguageContributionId> RequiresContributionIds => Snapshot(_requiresContributions);
    internal IReadOnlyList<LanguageCapabilityId> ProvidedCapabilities => Snapshot(_providesCapabilities);
    internal IReadOnlyList<LanguageCapabilityId> RequiredCapabilities => Snapshot(_requiresCapabilities);
    internal IReadOnlyList<LanguageContributionId> ConflictingContributions => Snapshot(_conflicts);
    internal IReadOnlyList<LanguageCapabilityId> ConflictingCapabilities => Snapshot(_conflictsCapabilities);
    internal IReadOnlyList<LanguageContributionId> BeforeContributionIds => Snapshot(_beforeContributions);
    internal IReadOnlyList<LanguageContributionId> AfterContributionIds => Snapshot(_afterContributions);
    internal IReadOnlyDictionary<string, string> Metadata =>
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(_metadata, StringComparer.Ordinal));

    private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> values) where T : notnull =>
        new ReadOnlyCollection<T>(values.Distinct().OrderBy(static item => item.ToString(), StringComparer.Ordinal).ToArray());
}
