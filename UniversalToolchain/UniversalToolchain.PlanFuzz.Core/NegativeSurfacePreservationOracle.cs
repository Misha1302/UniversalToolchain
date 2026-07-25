namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Verifies that owners excluded from the selected surface are absent from a complete activation trace.
/// </summary>
public sealed class NegativeSurfacePreservationOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.NegativeSurfacePreservation;
    public int OracleVersion => 1;

    public PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context)
    {
        context = context.ArgNotNull();
        if (context.Contract.VariantIds.Count == 0)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "Negative-surface preservation requires at least one variant.", "invalid-arity");

        var evaluated = 0;
        foreach (var variantId in context.Contract.VariantIds)
        {
            if (!context.TryGetObservation(variantId, out var observation))
                return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, $"Observation '{variantId}' is missing.", "missing-observation");
            if (observation.Outcome == PlanFuzzExecutionOutcome.InfrastructureFailure)
                return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "Infrastructure failure prevents negative-surface evaluation.", "infrastructure");
            if (observation.Outcome == PlanFuzzExecutionOutcome.Timeout)
                return Result(context, PlanFuzzOracleStatus.Inconclusive, "Worker timeout prevents negative-surface evaluation.", "timeout");
            if (observation.Surface == null)
                return Result(context, PlanFuzzOracleStatus.Inconclusive, $"Variant '{variantId}' did not publish surface evidence.", "missing-surface");
            if (!observation.Surface.ActivationTraceComplete)
                return Result(context, PlanFuzzOracleStatus.Inconclusive, $"Variant '{variantId}' published an incomplete activation trace.", $"incomplete-trace:{observation.Surface.TraceKind}");
            if (observation.Surface.ExcludedSurfaceIds.Count == 0)
                continue;

            evaluated++;
            var activatedExcluded = observation.Surface.ExcludedSurfaceIds
                .Intersect(observation.Surface.ActivatedOwnerIds, StringComparer.Ordinal)
                .OrderBy(static value => value, StringComparer.Ordinal)
                .ToArray();
            if (activatedExcluded.Length != 0)
            {
                var material = string.Join(',', activatedExcluded);
                return Result(
                    context,
                    PlanFuzzOracleStatus.Violated,
                    $"Variant '{variantId}' activated owner(s) excluded from its selected surface.",
                    $"{variantId}:{observation.Surface.TraceKind}:{material}",
                    $"excluded-owner-activated:{material}");
            }
        }

        return evaluated == 0
            ? Result(context, PlanFuzzOracleStatus.NotApplicable, "No selected variant declares an excluded surface.", "no-excluded-surface")
            : Result(context, PlanFuzzOracleStatus.Passed, "Excluded surface owners are absent from all complete activation traces.", "equal");
    }

    private PlanFuzzOracleResult Result(
        PlanFuzzOracleContext context,
        PlanFuzzOracleStatus status,
        string summary,
        string fingerprintMaterial,
        string? classFingerprintMaterial = null) =>
        new(context.Contract.ContractId, OracleId, OracleVersion, status, summary, fingerprintMaterial, classFingerprintMaterial);
}
