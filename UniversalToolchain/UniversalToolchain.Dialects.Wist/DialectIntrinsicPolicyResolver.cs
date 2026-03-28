using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

public sealed class DialectIntrinsicPolicyResolver
{
    public (IReadOnlyList<string> Allowed, IReadOnlyList<string> Forbidden, bool HasExplicitAllowList) Resolve(
        DialectBuildPlan buildPlan,
        DialectBackendId backendId)
    {
        if (buildPlan == null)
            Thrower.ArgumentNull(nameof(buildPlan));

        var allowed = buildPlan.IntrinsicDirectives
            .Where(x => x.Allowed && x.Target.Matches(backendId))
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var forbidden = buildPlan.IntrinsicDirectives
            .Where(x => !x.Allowed && x.Target.Matches(backendId))
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var hasExplicitAllowList = buildPlan.IntrinsicDirectives.Any(x => x.Allowed && x.Target.Matches(backendId));
        return (allowed, forbidden, hasExplicitAllowList);
    }
}