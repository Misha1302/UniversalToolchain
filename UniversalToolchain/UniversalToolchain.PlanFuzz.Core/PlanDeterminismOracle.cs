namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Verifies that equivalent registry or definition ordering produces the same plan identity and behavior.
/// </summary>
public sealed class PlanDeterminismOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.PlanDeterminism;
    public int OracleVersion => 1;

    public PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context)
    {
        context = context.ArgNotNull();
        if (context.Contract.VariantIds.Count != 2)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "Plan determinism requires exactly two variants.", "invalid-arity");
        if (!context.TryGetObservation(context.Contract.VariantIds[0], out var first) ||
            !context.TryGetObservation(context.Contract.VariantIds[1], out var second))
        {
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "One or more required observations are missing.", "missing-observation");
        }

        var pair = new[] { first, second };
        if (PlanFuzzObservationComparer.HasInfrastructureFailure(pair))
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "Infrastructure failure prevents plan determinism evaluation.", "infrastructure");
        if (PlanFuzzObservationComparer.HasTimeout(pair))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Worker timeout prevents plan determinism evaluation.", "timeout");
        if (first.Plan == null || second.Plan == null)
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Plan snapshots are unavailable.", "missing-plan");

        if (!StringComparer.Ordinal.Equals(first.Plan.PlanHash, second.Plan.PlanHash) ||
            !StringComparer.Ordinal.Equals(first.Plan.CanonicalLockSha256, second.Plan.CanonicalLockSha256))
        {
            return Result(
                context,
                PlanFuzzOracleStatus.Violated,
                $"Equivalent variants '{first.VariantId}' and '{second.VariantId}' produced different plan identities.",
                $"{first.Plan.PlanHash}:{first.Plan.CanonicalLockSha256}|{second.Plan.PlanHash}:{second.Plan.CanonicalLockSha256}");
        }

        if (!PlanFuzzObservationComparer.AreSemanticallyEquivalent(first, second))
        {
            return Result(
                context,
                PlanFuzzOracleStatus.Violated,
                $"Equivalent plan variants '{first.VariantId}' and '{second.VariantId}' produced different behavior.",
                $"behavior:{first.Outcome}:{first.Value?.CanonicalValue}|{second.Outcome}:{second.Value?.CanonicalValue}");
        }

        return Result(context, PlanFuzzOracleStatus.Passed, "Equivalent variants produced the same plan identity and behavior.", "equal");
    }

    private PlanFuzzOracleResult Result(
        PlanFuzzOracleContext context,
        PlanFuzzOracleStatus status,
        string summary,
        string fingerprintMaterial) =>
        new(context.Contract.ContractId, OracleId, OracleVersion, status, summary, fingerprintMaterial);
}
