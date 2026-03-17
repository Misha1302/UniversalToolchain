using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Defines explicit backend enable/disable directives.
/// </summary>
public sealed class BackendPolicy
{
    private readonly ReadOnlyCollection<DialectBackendTarget> _enabledBackends;
    private readonly ReadOnlyCollection<DialectBackendTarget> _disabledBackends;

    public BackendPolicy(IEnumerable<DialectBackendTarget>? enabledBackends = null, IEnumerable<DialectBackendTarget>? disabledBackends = null)
    {
        var enabled = NormalizeNames(enabledBackends, nameof(enabledBackends));
        var disabled = NormalizeNames(disabledBackends, nameof(disabledBackends));

        var enabledSet = new HashSet<DialectBackendTarget>(enabled);
        var overlap = disabled.FirstOrDefault(enabledSet.Contains);
        if (enabledSet.Contains(overlap))
            Thrower.Argument(nameof(disabledBackends), $"Backend '{DialectBackendTargetText.ToText(overlap)}' cannot be both enabled and disabled.");

        _enabledBackends = new ReadOnlyCollection<DialectBackendTarget>(enabled);
        _disabledBackends = new ReadOnlyCollection<DialectBackendTarget>(disabled);
    }

    public IReadOnlyList<DialectBackendTarget> EnabledBackends => _enabledBackends;

    public IReadOnlyList<DialectBackendTarget> DisabledBackends => _disabledBackends;

    private static List<DialectBackendTarget> NormalizeNames(IEnumerable<DialectBackendTarget>? values, string paramName)
    {
        if (values == null)
            return [];

        var unique = new HashSet<DialectBackendTarget>();
        var normalized = new List<DialectBackendTarget>();

        foreach (var value in values)
        {
            if (!Enum.IsDefined(value) || value == DialectBackendTarget.Any)
                Thrower.Argument(paramName, "Policy entries must be defined backend targets without 'any'.");

            if (!unique.Add(value))
                continue;

            normalized.Add(value);
        }

        return normalized;
    }
}
