using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Defines explicit backend enable or disable directives.
/// </summary>
public sealed class BackendPolicy
{
    private readonly ReadOnlyCollection<DialectBackendId> _disabledBackends;
    private readonly ReadOnlyCollection<DialectBackendId> _enabledBackends;

    public BackendPolicy(IEnumerable<DialectBackendId>? enabledBackends = null, IEnumerable<DialectBackendId>? disabledBackends = null)
    {
        var enabled = NormalizeNames(enabledBackends, nameof(enabledBackends));
        var disabled = NormalizeNames(disabledBackends, nameof(disabledBackends));

        var enabledSet = new HashSet<DialectBackendId>(enabled);
        foreach (var disabledBackend in disabled)
        {
            if (enabledSet.Contains(disabledBackend))
                Thrower.Argument(nameof(disabledBackends), $"Backend '{DialectBackendSelectorText.ToText(disabledBackend)}' cannot be both enabled and disabled.");
        }

        _enabledBackends = new ReadOnlyCollection<DialectBackendId>(enabled);
        _disabledBackends = new ReadOnlyCollection<DialectBackendId>(disabled);
    }

    public IReadOnlyList<DialectBackendId> EnabledBackends => _enabledBackends;

    public IReadOnlyList<DialectBackendId> DisabledBackends => _disabledBackends;

    private static List<DialectBackendId> NormalizeNames(IEnumerable<DialectBackendId>? values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        if (values == null)
            return [];

        var unique = new HashSet<DialectBackendId>();
        var normalized = new List<DialectBackendId>();

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value.Value))
                Thrower.Argument(paramName, "Policy entries must contain backend identifiers.");

            if (!unique.Add(value))
                continue;

            normalized.Add(value);
        }

        normalized.Sort();
        return normalized;
    }
}