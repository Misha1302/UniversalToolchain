using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Defines optimizer enable/disable directives.
/// </summary>
public sealed class OptimizerPolicy
{
    private readonly ReadOnlyCollection<string> _disabledOptimizers;
    private readonly ReadOnlyCollection<string> _enabledOptimizers;

    public OptimizerPolicy(IEnumerable<string>? enabledOptimizers = null, IEnumerable<string>? disabledOptimizers = null)
    {
        var enabled = NormalizeNames(enabledOptimizers, nameof(enabledOptimizers));
        var disabled = NormalizeNames(disabledOptimizers, nameof(disabledOptimizers));

        var overlap = enabled.Intersect(disabled, StringComparer.Ordinal).FirstOrDefault();
        if (overlap != null)
            Thrower.Argument(nameof(disabledOptimizers), $"Optimizer '{overlap}' cannot be both enabled and disabled.");

        _enabledOptimizers = new ReadOnlyCollection<string>(enabled);
        _disabledOptimizers = new ReadOnlyCollection<string>(disabled);
    }

    public IReadOnlyList<string> EnabledOptimizers => _enabledOptimizers;

    public IReadOnlyList<string> DisabledOptimizers => _disabledOptimizers;

    private static List<string> NormalizeNames(IEnumerable<string>? values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        if (values == null)
            return [];

        var unique = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<string>();

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                Thrower.Argument(paramName.NotNull(), "Policy entries must not be null or empty.");

            if (!unique.Add(value))
                continue;

            normalized.Add(value);
        }

        return normalized;
    }
}