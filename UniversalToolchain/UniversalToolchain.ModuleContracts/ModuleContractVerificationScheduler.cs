namespace UniversalToolchain.ModuleContracts;

public sealed record ModuleContractVerifierRoute(
    VerifierRuleId RuleId,
    string CanonicalOwner);

public sealed record ModuleContractScheduledVerifierInvocation(
    VerifierRuleId RuleId,
    string CanonicalOwner,
    IReadOnlyList<CompilerFactId> InvalidatedFacts,
    bool IsObligationDriven);

/// <summary>
/// Deterministically maps a verification policy and typed invalidation obligations to semantic verifier invocations.
/// </summary>
public static class ModuleContractVerificationScheduler
{
    public static IReadOnlyList<ModuleContractScheduledVerifierInvocation> Schedule(
        ModuleContractVerificationPolicy policy,
        IReadOnlyList<ModuleContractVerifierRoute> availableRoutes,
        IReadOnlyList<ReverificationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(availableRoutes);
        ArgumentNullException.ThrowIfNull(requests);
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown module-contract verification policy.");

        if (policy is ModuleContractVerificationPolicy.P0Structural or ModuleContractVerificationPolicy.P1Invalidation)
            return [];

        var canonicalRoutes = BuildCanonicalRoutes(availableRoutes);
        var requestsByRule = requests
            .GroupBy(static request => request.RuleId)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CompilerFactId>)group
                    .SelectMany(static request => request.InvalidatedFacts)
                    .Distinct()
                    .OrderBy(static fact => fact.Value, StringComparer.Ordinal)
                    .ToArray());

        foreach (var requestedRule in requestsByRule.Keys)
        {
            if (!canonicalRoutes.ContainsKey(requestedRule))
            {
                throw new InvalidOperationException(
                    $"Semantic verification obligation '{requestedRule}' has no canonical executable route.");
            }
        }

        var selectedRules = policy == ModuleContractVerificationPolicy.P2Selective
            ? requestsByRule.Keys
            : canonicalRoutes.Keys;

        return selectedRules
            .OrderBy(static rule => rule.Value, StringComparer.Ordinal)
            .Select(rule => new ModuleContractScheduledVerifierInvocation(
                rule,
                canonicalRoutes[rule],
                requestsByRule.GetValueOrDefault(rule, []),
                requestsByRule.ContainsKey(rule)))
            .ToArray();
    }

    private static IReadOnlyDictionary<VerifierRuleId, string> BuildCanonicalRoutes(
        IReadOnlyList<ModuleContractVerifierRoute> availableRoutes)
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
