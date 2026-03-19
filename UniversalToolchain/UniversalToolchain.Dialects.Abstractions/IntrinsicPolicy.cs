using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Defines intrinsic allow/forbid directives.
/// </summary>
public sealed class IntrinsicPolicy
{
    private readonly ReadOnlyCollection<string> _allowedIntrinsics;
    private readonly ReadOnlyCollection<string> _forbiddenIntrinsics;

    public IntrinsicPolicy(IEnumerable<string>? allowedIntrinsics = null, IEnumerable<string>? forbiddenIntrinsics = null)
    {
        var allowed = NormalizeNames(allowedIntrinsics, nameof(allowedIntrinsics));
        var forbidden = NormalizeNames(forbiddenIntrinsics, nameof(forbiddenIntrinsics));

        var overlap = allowed.Intersect(forbidden, StringComparer.Ordinal).FirstOrDefault();
        if (overlap != null)
            Thrower.Argument(nameof(forbiddenIntrinsics), $"Intrinsic '{overlap}' cannot be both allowed and forbidden.");

        _allowedIntrinsics = new ReadOnlyCollection<string>(allowed);
        _forbiddenIntrinsics = new ReadOnlyCollection<string>(forbidden);
    }

    public IReadOnlyList<string> AllowedIntrinsics => _allowedIntrinsics;

    public IReadOnlyList<string> ForbiddenIntrinsics => _forbiddenIntrinsics;

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