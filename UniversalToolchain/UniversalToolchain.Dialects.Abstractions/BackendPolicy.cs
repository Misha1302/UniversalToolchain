using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Defines explicit backend enable/disable directives.
/// </summary>
public sealed class BackendPolicy
{
    private readonly ReadOnlyCollection<string> _enabledBackends;
    private readonly ReadOnlyCollection<string> _disabledBackends;

    public BackendPolicy(IEnumerable<string>? enabledBackends = null, IEnumerable<string>? disabledBackends = null)
    {
        var enabled = NormalizeNames(enabledBackends, nameof(enabledBackends));
        var disabled = NormalizeNames(disabledBackends, nameof(disabledBackends));

        var overlap = enabled.Intersect(disabled, StringComparer.Ordinal).FirstOrDefault();
        if (overlap != null)
            Thrower.Argument(nameof(disabledBackends), $"Backend '{overlap}' cannot be both enabled and disabled.");

        _enabledBackends = new ReadOnlyCollection<string>(enabled);
        _disabledBackends = new ReadOnlyCollection<string>(disabled);
    }

    public IReadOnlyList<string> EnabledBackends => _enabledBackends;

    public IReadOnlyList<string> DisabledBackends => _disabledBackends;

    private static List<string> NormalizeNames(IEnumerable<string>? values, string paramName)
    {
        if (values == null)
            return [];

        var unique = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<string>();

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                Thrower.Argument(paramName, "Policy entries must not be null or empty.");

            if (!unique.Add(value))
                continue;

            normalized.Add(value);
        }

        return normalized;
    }
}
