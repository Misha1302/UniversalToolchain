using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Represents a validated and normalized dialect build plan ready for later runtime resolution.
/// </summary>
public sealed class DialectBuildPlan
{
    private readonly ReadOnlyDictionary<string, bool> _capabilities;
    private readonly ReadOnlyCollection<DialectBackendId> _disabledBackends;
    private readonly ReadOnlyCollection<DialectBackendId> _enabledBackends;
    private readonly ReadOnlyCollection<IntrinsicBuildDirective> _intrinsicDirectives;
    private readonly ReadOnlyCollection<OptimizerBuildDirective> _optimizerDirectives;
    private readonly ReadOnlyCollection<string> _orderedModules;

    public DialectBuildPlan(
        string name,
        string? version,
        IEnumerable<string> orderedModules,
        IEnumerable<DialectBackendId> enabledBackends,
        IEnumerable<DialectBackendId> disabledBackends,
        IEnumerable<IntrinsicBuildDirective> intrinsicDirectives,
        IEnumerable<OptimizerBuildDirective> optimizerDirectives,
        SecurityProfile? securityProfile,
        IEnumerable<KeyValuePair<string, bool>> capabilities,
        DialectValidationResult validationResult)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Dialect name must not be empty.");

        if (validationResult == null)
            Thrower.ArgumentNull(nameof(validationResult));

        Name = name;
        Version = version;
        _orderedModules = new ReadOnlyCollection<string>(Snapshot(orderedModules, nameof(orderedModules)));
        _enabledBackends = new ReadOnlyCollection<DialectBackendId>(Snapshot(enabledBackends, nameof(enabledBackends)));
        _disabledBackends = new ReadOnlyCollection<DialectBackendId>(Snapshot(disabledBackends, nameof(disabledBackends)));
        _intrinsicDirectives = new ReadOnlyCollection<IntrinsicBuildDirective>(Snapshot(intrinsicDirectives, nameof(intrinsicDirectives)));
        _optimizerDirectives = new ReadOnlyCollection<OptimizerBuildDirective>(Snapshot(optimizerDirectives, nameof(optimizerDirectives)));
        _capabilities = new ReadOnlyDictionary<string, bool>(SnapshotDictionary(capabilities));
        SecurityProfile = securityProfile;
        ValidationResult = validationResult;
    }

    public string Name { get; }

    public string? Version { get; }

    public IReadOnlyList<string> OrderedModules => _orderedModules;

    public IReadOnlyList<DialectBackendId> EnabledBackends => _enabledBackends;

    public IReadOnlyList<DialectBackendId> DisabledBackends => _disabledBackends;

    public IReadOnlyList<IntrinsicBuildDirective> IntrinsicDirectives => _intrinsicDirectives;

    public IReadOnlyList<OptimizerBuildDirective> OptimizerDirectives => _optimizerDirectives;

    public SecurityProfile? SecurityProfile { get; }

    public IReadOnlyDictionary<string, bool> Capabilities => _capabilities;

    public DialectValidationResult ValidationResult { get; }

    public bool CanBuild => ValidationResult.IsValid;

    private static List<T> Snapshot<T>(IEnumerable<T> source, string paramName)
    {
        if (source == null)
            Thrower.ArgumentNull(paramName);

        var list = new List<T>();
        foreach (var item in source)
        {
            if (item == null)
                Thrower.Argument(paramName, "Collection must not contain null entries.");

            list.Add(item);
        }

        return list;
    }

    private static Dictionary<string, bool> SnapshotDictionary(IEnumerable<KeyValuePair<string, bool>> source)
    {
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        var dictionary = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
                Thrower.Argument(nameof(source), "Capability key must not be empty.");

            dictionary[item.Key] = item.Value;
        }

        return dictionary;
    }
}