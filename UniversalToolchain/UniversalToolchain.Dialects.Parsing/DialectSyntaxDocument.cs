using System.Collections.ObjectModel;
using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
/// Represents parsed DSL directives as immutable syntax data.
/// </summary>
public sealed class DialectSyntaxDocument
{
    private readonly ReadOnlyCollection<string> _useModules;
    private readonly ReadOnlyCollection<string> _excludeModules;
    private readonly ReadOnlyCollection<OrderRule> _orderRules;
    private readonly ReadOnlyCollection<BackendDirectiveSyntax> _backendDirectives;
    private readonly ReadOnlyCollection<IntrinsicDirectiveSyntax> _intrinsicDirectives;
    private readonly ReadOnlyCollection<OptimizerDirectiveSyntax> _optimizerDirectives;
    private readonly ReadOnlyDictionary<string, bool> _capabilities;

    public DialectSyntaxDocument(
        string name,
        string? version,
        IEnumerable<string> useModules,
        IEnumerable<string> excludeModules,
        IEnumerable<OrderRule> orderRules,
        IEnumerable<BackendDirectiveSyntax> backendDirectives,
        IEnumerable<IntrinsicDirectiveSyntax> intrinsicDirectives,
        IEnumerable<OptimizerDirectiveSyntax> optimizerDirectives,
        SecurityProfile? securityProfile,
        IEnumerable<KeyValuePair<string, bool>> capabilities)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Dialect name must not be empty.");

        if (version != null && string.IsNullOrWhiteSpace(version))
            Thrower.Argument(nameof(version), "Dialect version must be null or non-empty.");

        Name = name;
        Version = version;
        _useModules = new ReadOnlyCollection<string>(Snapshot(useModules, nameof(useModules)));
        _excludeModules = new ReadOnlyCollection<string>(Snapshot(excludeModules, nameof(excludeModules)));
        _orderRules = new ReadOnlyCollection<OrderRule>(Snapshot(orderRules, nameof(orderRules)));
        _backendDirectives = new ReadOnlyCollection<BackendDirectiveSyntax>(Snapshot(backendDirectives, nameof(backendDirectives)));
        _intrinsicDirectives = new ReadOnlyCollection<IntrinsicDirectiveSyntax>(Snapshot(intrinsicDirectives, nameof(intrinsicDirectives)));
        _optimizerDirectives = new ReadOnlyCollection<OptimizerDirectiveSyntax>(Snapshot(optimizerDirectives, nameof(optimizerDirectives)));
        _capabilities = new ReadOnlyDictionary<string, bool>(SnapshotDictionary(capabilities));
        SecurityProfile = securityProfile;
    }

    public string Name { get; }

    public string? Version { get; }

    public IReadOnlyList<string> UseModules => _useModules;

    public IReadOnlyList<string> ExcludeModules => _excludeModules;

    public IReadOnlyList<OrderRule> OrderRules => _orderRules;

    public IReadOnlyList<BackendDirectiveSyntax> BackendDirectives => _backendDirectives;

    public IReadOnlyList<IntrinsicDirectiveSyntax> IntrinsicDirectives => _intrinsicDirectives;

    public IReadOnlyList<OptimizerDirectiveSyntax> OptimizerDirectives => _optimizerDirectives;

    public SecurityProfile? SecurityProfile { get; }

    public IReadOnlyDictionary<string, bool> Capabilities => _capabilities;

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
                Thrower.Argument(nameof(source), "Capability name must not be empty.");

            dictionary[item.Key] = item.Value;
        }

        return dictionary;
    }
}
