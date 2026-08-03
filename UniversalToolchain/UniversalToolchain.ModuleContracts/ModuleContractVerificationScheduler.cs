namespace UniversalToolchain.ModuleContracts;

public sealed record ModuleContractVerifierRoute(
    VerifierRuleId RuleId,
    string CanonicalOwner);

public sealed record ModuleContractScheduledVerifierInvocation(
    VerifierRuleId RuleId,
    string CanonicalOwner,
    IReadOnlyList<CompilerFactId> InvalidatedFacts,
    bool IsObligationDriven,
    IReadOnlyList<VerificationObligation> Obligations);

/// <summary>
/// Deterministically maps a verification policy and typed invalidation obligations to semantic verifier invocations.
/// </summary>
public static class ModuleContractVerificationScheduler
{
    /// <summary>
    /// Compatibility overload for the frozen P0--P3 experiment protocol.
    /// New protocol versions pass boundary-indexed obligations explicitly.
    /// </summary>
    public static IReadOnlyList<ModuleContractScheduledVerifierInvocation> Schedule(
        ModuleContractVerificationPolicy policy,
        IReadOnlyList<ModuleContractVerifierRoute> availableRoutes,
        IReadOnlyList<ReverificationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(availableRoutes);
        ArgumentNullException.ThrowIfNull(requests);
        var routeOwners = BuildCanonicalRoutes(availableRoutes);
        var obligations = requests
            .SelectMany(request => request.InvalidatedFacts.Select(fact => new VerificationObligation(
                fact,
                request.RuleId,
                routeOwners.GetValueOrDefault(request.RuleId, string.Empty),
                CompilerPipelineStage.OptimizedAir,
                CompilerPipelineStage.OptimizedAir)))
            .ToArray();
        return Schedule(
            policy,
            CompilerPipelineStage.OptimizedAir,
            availableRoutes,
            obligations,
            demandedFacts: new HashSet<CompilerFactId>(),
            knownFacts: obligations.Select(static obligation => obligation.FactId).ToHashSet());
    }

    public static IReadOnlyList<ModuleContractScheduledVerifierInvocation> Schedule(
        ModuleContractVerificationPolicy policy,
        CompilerPipelineStage currentBoundary,
        IReadOnlyList<ModuleContractVerifierRoute> availableRoutes,
        IReadOnlyList<VerificationObligation> obligations,
        IReadOnlySet<CompilerFactId>? demandedFacts = null,
        IReadOnlySet<CompilerFactId>? knownFacts = null)
    {
        ArgumentNullException.ThrowIfNull(availableRoutes);
        ArgumentNullException.ThrowIfNull(obligations);
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown module-contract verification policy.");
        if (!Enum.IsDefined(currentBoundary))
            throw new ArgumentOutOfRangeException(nameof(currentBoundary), currentBoundary, "Unknown compiler boundary.");

        if (policy is ModuleContractVerificationPolicy.P0Structural or ModuleContractVerificationPolicy.P1Invalidation)
            return [];

        var canonicalRoutes = BuildCanonicalRoutes(availableRoutes);
        demandedFacts ??= new HashSet<CompilerFactId>();
        knownFacts ??= new HashSet<CompilerFactId>();

        if (policy == ModuleContractVerificationPolicy.P1DemandRecomputation)
        {
            foreach (var demandedFact in demandedFacts.OrderBy(static fact => fact.Value, StringComparer.Ordinal))
            {
                if (!knownFacts.Contains(demandedFact))
                {
                    throw new InvalidOperationException(
                        $"Demand query for unknown compiler fact '{demandedFact}' has no safe recomputation route.");
                }
            }
        }

        var normalized = obligations
            .Select(ValidateObligation)
            .Distinct()
            .OrderBy(static obligation => obligation.FirstEligibleBoundary)
            .ThenBy(static obligation => obligation.RuleId.Value, StringComparer.Ordinal)
            .ThenBy(static obligation => obligation.FactId.Value, StringComparer.Ordinal)
            .ToArray();

        IReadOnlyList<VerificationObligation> selectedObligations;
        switch (policy)
        {
            case ModuleContractVerificationPolicy.P1DemandRecomputation:
                selectedObligations = normalized
                    .Where(obligation => obligation.CreationBoundary <= currentBoundary)
                    .Where(obligation => demandedFacts.Contains(obligation.FactId))
                    .ToArray();
                break;
            case ModuleContractVerificationPolicy.P2Selective:
            case ModuleContractVerificationPolicy.P3Always:
                foreach (var overdue in normalized.Where(obligation => obligation.FirstEligibleBoundary < currentBoundary))
                {
                    throw new InvalidOperationException(
                        $"Semantic verification obligation for fact '{overdue.FactId}' missed its first eligible boundary " +
                        $"'{overdue.FirstEligibleBoundary}' before '{currentBoundary}'.");
                }

                selectedObligations = normalized
                    .Where(obligation => obligation.FirstEligibleBoundary == currentBoundary)
                    .ToArray();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown module-contract verification policy.");
        }

        foreach (var obligation in selectedObligations)
        {
            if (!canonicalRoutes.TryGetValue(obligation.RuleId, out var owner))
            {
                throw new InvalidOperationException(
                    $"Semantic verification obligation '{obligation.RuleId}' for fact '{obligation.FactId}' " +
                    $"has no canonical executable route at boundary '{currentBoundary}'.");
            }

            if (!StringComparer.Ordinal.Equals(owner, obligation.CanonicalOwner))
            {
                throw new InvalidOperationException(
                    $"Semantic verification obligation '{obligation.RuleId}' names canonical owner " +
                    $"'{obligation.CanonicalOwner}', but boundary '{currentBoundary}' exposes '{owner}'.");
            }
        }

        var obligationsByRule = selectedObligations
            .GroupBy(static obligation => obligation.RuleId)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<VerificationObligation>)group.ToArray());

        var selectedRules = policy == ModuleContractVerificationPolicy.P3Always
            ? canonicalRoutes.Keys
            : obligationsByRule.Keys;

        return selectedRules
            .OrderBy(static rule => rule.Value, StringComparer.Ordinal)
            .Select(rule =>
            {
                var ruleObligations = obligationsByRule.GetValueOrDefault(rule, []);
                return new ModuleContractScheduledVerifierInvocation(
                    rule,
                    canonicalRoutes[rule],
                    ruleObligations
                        .Select(static obligation => obligation.FactId)
                        .Distinct()
                        .OrderBy(static fact => fact.Value, StringComparer.Ordinal)
                        .ToArray(),
                    ruleObligations.Count != 0,
                    ruleObligations);
            })
            .ToArray();
    }

    private static VerificationObligation ValidateObligation(VerificationObligation obligation)
    {
        obligation = obligation.ArgNotNull();
        if (string.IsNullOrWhiteSpace(obligation.CanonicalOwner))
            throw new InvalidOperationException($"Verifier obligation '{obligation.RuleId}' has no canonical owner.");
        if (!Enum.IsDefined(obligation.CreationBoundary))
            throw new InvalidOperationException($"Verifier obligation '{obligation.RuleId}' has an unknown creation boundary.");
        if (!Enum.IsDefined(obligation.FirstEligibleBoundary))
            throw new InvalidOperationException($"Verifier obligation '{obligation.RuleId}' has an unknown first eligible boundary.");
        if (obligation.FirstEligibleBoundary < obligation.CreationBoundary)
        {
            throw new InvalidOperationException(
                $"Verifier obligation '{obligation.RuleId}' is eligible before it is created.");
        }
        return obligation;
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
