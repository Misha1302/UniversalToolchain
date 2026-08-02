using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.ContractExperiments;

internal sealed record VerifierRouteDescriptor(
    VerifierRuleId RuleId,
    string CanonicalOwner);

internal sealed record ScheduledVerifierInvocation(
    VerifierRuleId RuleId,
    string CanonicalOwner,
    IReadOnlyList<CompilerFactId> InvalidatedFacts,
    bool IsObligationDriven);

internal static class VerificationPolicyScheduler
{
    public static IReadOnlyList<ScheduledVerifierInvocation> Schedule(
        ExperimentPolicy policy,
        IReadOnlyList<VerifierRouteDescriptor> availableRoutes,
        IReadOnlyList<ReverificationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(availableRoutes);
        ArgumentNullException.ThrowIfNull(requests);

        if (policy is ExperimentPolicy.P0_STRUCTURAL or ExperimentPolicy.P1_INVALIDATION)
            return [];

        var canonicalRoutes = BuildCanonicalRoutes(availableRoutes);
        var requestsByRule = GroupInvalidatedFactsByRule(requests);

        foreach (var requestedRule in requestsByRule.Keys)
        {
            if (!canonicalRoutes.ContainsKey(requestedRule))
            {
                throw new InvalidOperationException(
                    $"Semantic verification obligation '{requestedRule}' has no canonical executable route.");
            }
        }

        var selectedRules = policy == ExperimentPolicy.P2_SELECTIVE
            ? requestsByRule.Keys
            : canonicalRoutes.Keys;

        return selectedRules
            .OrderBy(static rule => rule.Value, StringComparer.Ordinal)
            .Select(rule => new ScheduledVerifierInvocation(
                rule,
                canonicalRoutes[rule],
                requestsByRule.GetValueOrDefault(rule, []),
                requestsByRule.ContainsKey(rule)))
            .ToArray();
    }

    public static IReadOnlyList<ScheduledVerifierInvocation> ScheduleDemandDriven(
        IReadOnlyList<VerifierRouteDescriptor> availableRoutes,
        IReadOnlyList<ReverificationRequest> invalidations,
        IReadOnlyCollection<VerifierRuleId> demandedRules)
    {
        ArgumentNullException.ThrowIfNull(availableRoutes);
        ArgumentNullException.ThrowIfNull(invalidations);
        ArgumentNullException.ThrowIfNull(demandedRules);

        var canonicalRoutes = BuildCanonicalRoutes(availableRoutes);
        var invalidationsByRule = GroupInvalidatedFactsByRule(invalidations);
        var selectedRules = demandedRules
            .Distinct()
            .Where(invalidationsByRule.ContainsKey)
            .OrderBy(static rule => rule.Value, StringComparer.Ordinal)
            .ToArray();

        foreach (var demandedRule in selectedRules)
        {
            if (!canonicalRoutes.ContainsKey(demandedRule))
            {
                throw new InvalidOperationException(
                    $"Demanded invalidated fact route '{demandedRule}' has no canonical executable route.");
            }
        }

        return selectedRules
            .Select(rule => new ScheduledVerifierInvocation(
                rule,
                canonicalRoutes[rule],
                invalidationsByRule[rule],
                IsObligationDriven: false))
            .ToArray();
    }

    private static IReadOnlyDictionary<VerifierRuleId, IReadOnlyList<CompilerFactId>> GroupInvalidatedFactsByRule(
        IReadOnlyList<ReverificationRequest> requests) =>
        requests
            .GroupBy(static request => request.RuleId)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CompilerFactId>)group
                    .SelectMany(static request => request.InvalidatedFacts)
                    .Distinct()
                    .OrderBy(static fact => fact.Value, StringComparer.Ordinal)
                    .ToArray());

    private static IReadOnlyDictionary<VerifierRuleId, string> BuildCanonicalRoutes(
        IReadOnlyList<VerifierRouteDescriptor> availableRoutes)
    {
        var result = new Dictionary<VerifierRuleId, string>();
        foreach (var route in availableRoutes
                     .OrderBy(static route => route.RuleId.Value, StringComparer.Ordinal)
                     .ThenBy(static route => route.CanonicalOwner, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(route.CanonicalOwner))
                throw new InvalidOperationException($"Verifier route '{route.RuleId}' has no canonical owner.");

            if (result.TryGetValue(route.RuleId, out var owner) &&
                !StringComparer.Ordinal.Equals(owner, route.CanonicalOwner))
            {
                throw new InvalidOperationException(
                    $"Verifier route '{route.RuleId}' has conflicting canonical owners '{owner}' and '{route.CanonicalOwner}'.");
            }

            result[route.RuleId] = route.CanonicalOwner;
        }

        return result;
    }
}
