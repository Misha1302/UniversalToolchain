using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DialectBuildPlanExplanation
{
    private readonly ReadOnlyDictionary<string, bool> _capabilities;
    private readonly ReadOnlyCollection<DialectBackendId> _disabledBackends;
    private readonly ReadOnlyCollection<DialectBackendId> _enabledBackends;
    private readonly ReadOnlyCollection<IntrinsicBuildDirective> _intrinsicDirectives;
    private readonly ReadOnlyCollection<OptimizerBuildDirective> _optimizerDirectives;
    private readonly ReadOnlyCollection<string> _orderedModules;

    public DialectBuildPlanExplanation(
        bool canBuild,
        IEnumerable<string> orderedModules,
        IEnumerable<DialectBackendId> enabledBackends,
        IEnumerable<DialectBackendId> disabledBackends,
        IEnumerable<IntrinsicBuildDirective> intrinsicDirectives,
        IEnumerable<OptimizerBuildDirective> optimizerDirectives,
        SecurityProfile? securityProfile,
        IEnumerable<KeyValuePair<string, bool>> capabilities)
    {
        CanBuild = canBuild;
        _orderedModules = new ReadOnlyCollection<string>(Snapshot(orderedModules, nameof(orderedModules)));
        _enabledBackends = new ReadOnlyCollection<DialectBackendId>(Snapshot(enabledBackends, nameof(enabledBackends)));
        _disabledBackends = new ReadOnlyCollection<DialectBackendId>(Snapshot(disabledBackends, nameof(disabledBackends)));
        _intrinsicDirectives = new ReadOnlyCollection<IntrinsicBuildDirective>(Snapshot(intrinsicDirectives, nameof(intrinsicDirectives)));
        _optimizerDirectives = new ReadOnlyCollection<OptimizerBuildDirective>(Snapshot(optimizerDirectives, nameof(optimizerDirectives)));
        _capabilities = new ReadOnlyDictionary<string, bool>(SnapshotDictionary(capabilities));
        SecurityProfile = securityProfile;
    }

    public bool CanBuild { get; }

    public IReadOnlyList<string> OrderedModules => _orderedModules;

    public IReadOnlyList<DialectBackendId> EnabledBackends => _enabledBackends;

    public IReadOnlyList<DialectBackendId> DisabledBackends => _disabledBackends;

    public IReadOnlyList<IntrinsicBuildDirective> IntrinsicDirectives => _intrinsicDirectives;

    public IReadOnlyList<OptimizerBuildDirective> OptimizerDirectives => _optimizerDirectives;

    public SecurityProfile? SecurityProfile { get; }

    public IReadOnlyDictionary<string, bool> Capabilities => _capabilities;

    private static List<T> Snapshot<T>(IEnumerable<T> source, [CallerArgumentExpression(nameof(source))] string? paramName = null)
    {
        source = source.ArgNotNull();
        return source.Select(item => item.NotNull()).ToList();
    }

    private static Dictionary<string, bool> SnapshotDictionary(IEnumerable<KeyValuePair<string, bool>> source)
    {
        source = source.ArgNotNull();

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
