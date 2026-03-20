namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Describes include/exclude module directives.
/// </summary>
public sealed class ModulePolicy
{
    private readonly ReadOnlyCollection<string> _excludedModules;
    private readonly ReadOnlyCollection<string> _includedModules;

    public ModulePolicy(IEnumerable<string>? includedModules = null, IEnumerable<string>? excludedModules = null)
    {
        var included = NormalizeNames(includedModules, nameof(includedModules));
        var excluded = NormalizeNames(excludedModules, nameof(excludedModules));

        var overlap = included.Intersect(excluded, StringComparer.Ordinal).FirstOrDefault();
        if (overlap != null)
            Thrower.Argument(nameof(excludedModules), $"Module '{overlap}' cannot be both included and excluded.");

        _includedModules = new ReadOnlyCollection<string>(included);
        _excludedModules = new ReadOnlyCollection<string>(excluded);
    }

    public IReadOnlyList<string> IncludedModules => _includedModules;

    public IReadOnlyList<string> ExcludedModules => _excludedModules;

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