namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Verifies that a fallback is either absent or explicitly classified by the language adapter as a documented unsupported shape.
/// </summary>
public sealed class ControlledFallbackOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.ControlledFallback;
    public int OracleVersion => 1;

    public PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context)
    {
        context = context.ArgNotNull();
        if (context.Contract.VariantIds.Count == 0)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "Controlled fallback requires at least one variant.", "invalid-arity");

        foreach (var variantId in context.Contract.VariantIds)
        {
            if (!context.TryGetObservation(variantId, out var observation))
                return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, $"Observation '{variantId}' is missing.", "missing-observation");
            if (observation.Outcome == PlanFuzzExecutionOutcome.InfrastructureFailure)
                return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "Infrastructure failure prevents fallback evaluation.", "infrastructure");
            if (observation.Outcome == PlanFuzzExecutionOutcome.Timeout)
                return Result(context, PlanFuzzOracleStatus.Inconclusive, "Worker timeout prevents fallback evaluation.", "timeout");
            if (observation.Route == null)
                return Result(context, PlanFuzzOracleStatus.Inconclusive, $"Route evidence is missing for '{variantId}'.", "missing-route");

            var route = observation.Route;
            if (!route.FellBack)
                continue;
            if (route.FallbackKind == PlanFuzzFallbackKind.ClassifiedUnsupportedShape)
                continue;

            var diagnosticSequence = route.Diagnostics
                .Select(static diagnostic => $"{diagnostic.Code}@{diagnostic.Stage}")
                .ToArray();
            var diagnosticClasses = diagnosticSequence
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static value => value, StringComparer.Ordinal);
            return Result(
                context,
                PlanFuzzOracleStatus.Violated,
                $"Variant '{variantId}' performed an unclassified or internal-error fallback.",
                $"{variantId}:{route.RouteId}:{route.RequestedPolicy}:{route.FallbackKind}:" +
                string.Join(',', diagnosticSequence),
                $"{variantId}:{route.RouteId}:{route.RequestedPolicy}:{route.FallbackKind}:" +
                string.Join(',', diagnosticClasses));
        }

        return Result(context, PlanFuzzOracleStatus.Passed, "All observed fallbacks are absent or explicitly classified unsupported shapes.", "controlled");
    }

    private PlanFuzzOracleResult Result(
        PlanFuzzOracleContext context,
        PlanFuzzOracleStatus status,
        string summary,
        string fingerprintMaterial,
        string? classFingerprintMaterial = null) =>
        new(
            context.Contract.ContractId,
            OracleId,
            OracleVersion,
            status,
            summary,
            fingerprintMaterial,
            classFingerprintMaterial);
}
