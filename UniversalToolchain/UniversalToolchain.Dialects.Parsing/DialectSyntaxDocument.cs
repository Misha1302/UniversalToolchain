using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
///     Represents parsed DSL directives as immutable syntax data.
/// </summary>
public sealed class DialectSyntaxDocument
{
    private readonly ReadOnlyCollection<BackendDirectiveSyntax> _backendDirectives;
    private readonly ReadOnlyDictionary<string, bool> _capabilities;
    private readonly ReadOnlyCollection<string> _excludeModules;
    private readonly ReadOnlyCollection<IntrinsicDirectiveSyntax> _intrinsicDirectives;
    private readonly ReadOnlyCollection<OptimizerDirectiveSyntax> _optimizerDirectives;
    private readonly ReadOnlyCollection<OrderRule> _orderRules;
    private readonly ReadOnlyCollection<string> _useModules;

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
        IEnumerable<KeyValuePair<string, bool>> capabilities,
        string? baseDialectName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Dialect name must not be empty.");

        if (version != null && string.IsNullOrWhiteSpace(version))
            Thrower.Argument(nameof(version), "Dialect version must be null or non-empty.");

        if (baseDialectName != null && string.IsNullOrWhiteSpace(baseDialectName))
            Thrower.Argument(nameof(baseDialectName), "Base dialect name must be null or non-empty.");

        Name = name;
        Version = version;
        BaseDialectName = baseDialectName;
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

    public string? BaseDialectName { get; }

    public IReadOnlyList<string> UseModules => _useModules;

    public IReadOnlyList<string> ExcludeModules => _excludeModules;

    public IReadOnlyList<OrderRule> OrderRules => _orderRules;

    public IReadOnlyList<BackendDirectiveSyntax> BackendDirectives => _backendDirectives;

    public IReadOnlyList<IntrinsicDirectiveSyntax> IntrinsicDirectives => _intrinsicDirectives;

    public IReadOnlyList<OptimizerDirectiveSyntax> OptimizerDirectives => _optimizerDirectives;

    public SecurityProfile? SecurityProfile { get; }

    public IReadOnlyDictionary<string, bool> Capabilities => _capabilities;

    private static List<T> Snapshot<T>(IEnumerable<T> source, [CallerArgumentExpression(nameof(source))] string? paramName = null)
    {
        var list = new List<T>();
        foreach (var item in source)
        {
            if (item == null)
                Thrower.Argument(paramName.NotNull(), "Collection must not contain null entries.");

            list.Add(item);
        }

        return list;
    }

    private static Dictionary<string, bool> SnapshotDictionary(IEnumerable<KeyValuePair<string, bool>> source)
    {
        source = source.ArgNotNull();

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