using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSlice
{
    private readonly ReadOnlyCollection<DialectBackendDirective> _backendDirectives;
    private readonly ReadOnlyCollection<DialectCapabilityDirective> _capabilityDirectives;
    private readonly ReadOnlyCollection<string> _excludeModules;
    private readonly ReadOnlyCollection<DialectIntrinsicDirective> _intrinsicDirectives;
    private readonly ReadOnlyCollection<DialectOptimizerDirective> _optimizerDirectives;
    private readonly ReadOnlyCollection<DialectOrderDirective> _orderDirectives;
    private readonly ReadOnlyCollection<string> _useModules;

    public DialectDefinitionSlice(
        string name,
        IEnumerable<string> useModules,
        IEnumerable<string> excludeModules,
        IEnumerable<DialectOrderDirective> orderDirectives,
        IEnumerable<DialectBackendDirective> backendDirectives,
        IEnumerable<DialectIntrinsicDirective> intrinsicDirectives,
        IEnumerable<DialectOptimizerDirective> optimizerDirectives,
        DialectSecurityProfile? securityProfile,
        IEnumerable<DialectCapabilityDirective> capabilityDirectives,
        string? version = null,
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
        _useModules = new ReadOnlyCollection<string>(SnapshotStrings(useModules, nameof(useModules), "Module names must not be empty."));
        _excludeModules = new ReadOnlyCollection<string>(SnapshotStrings(excludeModules, nameof(excludeModules), "Module names must not be empty."));
        _orderDirectives = new ReadOnlyCollection<DialectOrderDirective>(Snapshot(orderDirectives, nameof(orderDirectives)));
        _backendDirectives = new ReadOnlyCollection<DialectBackendDirective>(Snapshot(backendDirectives, nameof(backendDirectives)));
        _intrinsicDirectives = new ReadOnlyCollection<DialectIntrinsicDirective>(Snapshot(intrinsicDirectives, nameof(intrinsicDirectives)));
        _optimizerDirectives = new ReadOnlyCollection<DialectOptimizerDirective>(Snapshot(optimizerDirectives, nameof(optimizerDirectives)));
        SecurityProfile = securityProfile;
        _capabilityDirectives = new ReadOnlyCollection<DialectCapabilityDirective>(Snapshot(capabilityDirectives, nameof(capabilityDirectives)));
    }

    public string Name { get; }

    public string? Version { get; }

    public string? BaseDialectName { get; }

    public IReadOnlyList<string> UseModules => _useModules;

    public IReadOnlyList<string> ExcludeModules => _excludeModules;

    public IReadOnlyList<DialectOrderDirective> OrderDirectives => _orderDirectives;

    public IReadOnlyList<DialectBackendDirective> BackendDirectives => _backendDirectives;

    public IReadOnlyList<DialectIntrinsicDirective> IntrinsicDirectives => _intrinsicDirectives;

    public IReadOnlyList<DialectOptimizerDirective> OptimizerDirectives => _optimizerDirectives;

    public DialectSecurityProfile? SecurityProfile { get; }

    public IReadOnlyList<DialectCapabilityDirective> CapabilityDirectives => _capabilityDirectives;

    private static List<string> SnapshotStrings(IEnumerable<string> values, string paramName, string emptyMessage)
    {
        var result = new List<string>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                Thrower.Argument(paramName, emptyMessage);

            result.Add(value);
        }

        return result;
    }

    private static List<T> Snapshot<T>(IEnumerable<T> values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        var result = new List<T>();
        foreach (var value in values)
        {
            if (value == null)
                Thrower.Argument(paramName.NotNull(), "Collection must not contain null values.");

            result.Add(value);
        }

        return result;
    }
}