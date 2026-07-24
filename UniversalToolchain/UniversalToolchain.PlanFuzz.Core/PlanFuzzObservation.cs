namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Captures one variant execution without timestamps or process-local identities in semantic fields.
/// </summary>
public sealed class PlanFuzzObservation
{
    public PlanFuzzObservation(
        string caseId,
        string variantId,
        string backendId,
        PlanFuzzExecutionOutcome outcome,
        PlanFuzzValueSnapshot? value,
        PlanFuzzFailureSnapshot? failure,
        PlanFuzzPlanSnapshot? plan,
        PlanFuzzRouteSnapshot? route = null)
    {
        if (string.IsNullOrWhiteSpace(caseId))
            Thrower.Argument(nameof(caseId), "Case ID must not be empty.");
        if (string.IsNullOrWhiteSpace(variantId))
            Thrower.Argument(nameof(variantId), "Variant ID must not be empty.");
        if (string.IsNullOrWhiteSpace(backendId))
            Thrower.Argument(nameof(backendId), "Backend ID must not be empty.");
        if (outcome == PlanFuzzExecutionOutcome.Success && value == null)
            Thrower.Argument(nameof(value), "Successful observation requires a value snapshot.");
        if (outcome != PlanFuzzExecutionOutcome.Success && failure == null)
            Thrower.Argument(nameof(failure), "Non-success observation requires a failure snapshot.");

        CaseId = caseId;
        VariantId = variantId;
        BackendId = backendId;
        Outcome = outcome;
        Value = value;
        Failure = failure;
        Plan = plan;
        Route = route;
    }

    public string CaseId { get; }
    public string VariantId { get; }
    public string BackendId { get; }
    public PlanFuzzExecutionOutcome Outcome { get; }
    public PlanFuzzValueSnapshot? Value { get; }
    public PlanFuzzFailureSnapshot? Failure { get; }
    public PlanFuzzPlanSnapshot? Plan { get; }
    public PlanFuzzRouteSnapshot? Route { get; }

    public static PlanFuzzObservation Timeout(string caseId, PlanFuzzPlanVariant variant, string message) =>
        new(
            caseId,
            variant.VariantId,
            variant.BackendId,
            PlanFuzzExecutionOutcome.Timeout,
            null,
            new PlanFuzzFailureSnapshot("worker-timeout", "worker", "timeout", message),
            null);

    public static PlanFuzzObservation InfrastructureFailure(
        string caseId,
        PlanFuzzPlanVariant variant,
        string category,
        string message) =>
        new(
            caseId,
            variant.VariantId,
            variant.BackendId,
            PlanFuzzExecutionOutcome.InfrastructureFailure,
            null,
            new PlanFuzzFailureSnapshot("infrastructure", "worker", category, message),
            null);
}
