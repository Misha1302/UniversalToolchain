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
        IReadOnlyList<ReverificationRequest> requests) =>
        Schedule(
            policy,
            CompilerPipelineStage.OptimizedAir,
            availableRoutes,
            requests,
            new HashSet<CompilerFactId>());

    public static IReadOnlyList<ScheduledVerifierInvocation> Schedule(
        ExperimentPolicy policy,
        CompilerPipelineStage currentBoundary,
        IReadOnlyList<VerifierRouteDescriptor> availableRoutes,
        IReadOnlyList<ReverificationRequest> requests,
        IReadOnlySet<CompilerFactId>? demandedFacts)
    {
        ArgumentNullException.ThrowIfNull(availableRoutes);
        ArgumentNullException.ThrowIfNull(requests);
        demandedFacts ??= new HashSet<CompilerFactId>();

        var routeOwners = availableRoutes
            .GroupBy(static route => route.RuleId)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static route => route.CanonicalOwner).Distinct(StringComparer.Ordinal).ToArray());
        var obligations = requests
            .SelectMany(request => request.InvalidatedFacts.Select(fact => new VerificationObligation(
                fact,
                request.RuleId,
                ResolveOwner(request.RuleId, routeOwners),
                currentBoundary,
                currentBoundary)))
            .ToArray();
        var knownFacts = CompilerFactVerifierRegistry.Core.KnownFacts
            .Concat(requests.SelectMany(static request => request.InvalidatedFacts))
            .Concat(demandedFacts)
            .ToHashSet();
        var scheduled = ModuleContractVerificationScheduler.Schedule(
            MapPolicy(policy),
            currentBoundary,
            availableRoutes.Select(static route => new ModuleContractVerifierRoute(
                route.RuleId,
                route.CanonicalOwner)).ToArray(),
            obligations,
            demandedFacts,
            knownFacts);
        return scheduled
            .Select(static invocation => new ScheduledVerifierInvocation(
                invocation.RuleId,
                invocation.CanonicalOwner,
                invocation.InvalidatedFacts,
                invocation.IsObligationDriven))
            .ToArray();
    }

    private static ModuleContractVerificationPolicy MapPolicy(ExperimentPolicy policy) => policy switch
    {
        ExperimentPolicy.P0_STRUCTURAL => ModuleContractVerificationPolicy.P0Structural,
        ExperimentPolicy.P1_INVALIDATION => ModuleContractVerificationPolicy.P1Invalidation,
        ExperimentPolicy.P1D_DEMAND_RECOMPUTATION => ModuleContractVerificationPolicy.P1DemandRecomputation,
        ExperimentPolicy.P2_SELECTIVE => ModuleContractVerificationPolicy.P2Selective,
        ExperimentPolicy.P3_ALWAYS => ModuleContractVerificationPolicy.P3Always,
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown experiment policy.")
    };

    private static string ResolveOwner(
        VerifierRuleId rule,
        IReadOnlyDictionary<VerifierRuleId, string[]> routeOwners)
    {
        if (!routeOwners.TryGetValue(rule, out var owners) || owners.Length == 0)
            return string.Empty;
        if (owners.Length != 1)
        {
            throw new InvalidOperationException(
                $"Verifier route '{rule}' has conflicting canonical owners: {string.Join(", ", owners)}.");
        }
        return owners[0];
    }
}
