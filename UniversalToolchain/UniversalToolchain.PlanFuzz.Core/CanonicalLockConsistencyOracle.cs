namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Verifies repeated and pretty/canonical lock serialization consistency for each selected variant.
/// </summary>
public sealed class CanonicalLockConsistencyOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.CanonicalLockConsistency;
    public int OracleVersion => 1;

    public PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context)
    {
        context = context.ArgNotNull();
        var observations = new List<PlanFuzzObservation>();
        foreach (var variantId in context.Contract.VariantIds)
        {
            if (!context.TryGetObservation(variantId, out var observation))
                return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, $"Observation '{variantId}' is missing.", "missing-observation");
            observations.Add(observation);
        }

        if (PlanFuzzObservationComparer.HasInfrastructureFailure(observations))
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "Infrastructure failure prevents lock evaluation.", "infrastructure");
        if (PlanFuzzObservationComparer.HasTimeout(observations))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Worker timeout prevents lock evaluation.", "timeout");

        foreach (var observation in observations)
        {
            var plan = observation.Plan;
            if (plan == null)
                return Result(context, PlanFuzzOracleStatus.Inconclusive, $"Plan snapshot is unavailable for '{observation.VariantId}'.", "missing-plan");
            if (!StringComparer.Ordinal.Equals(plan.CanonicalLockSha256, plan.RepeatedCanonicalLockSha256) ||
                !StringComparer.Ordinal.Equals(plan.CanonicalLockSemanticSha256, plan.PrettyLockSemanticSha256) ||
                plan.LockSchemaVersion != LanguageLockFile.SchemaVersion ||
                !StringComparer.Ordinal.Equals(plan.LockCanonicalization, LanguageLockFile.Canonicalization))
            {
                return Result(
                    context,
                    PlanFuzzOracleStatus.Violated,
                    $"Canonical lock consistency failed for variant '{observation.VariantId}'.",
                    $"{observation.VariantId}:{plan.CanonicalLockSha256}:{plan.RepeatedCanonicalLockSha256}:{plan.CanonicalLockSemanticSha256}:{plan.PrettyLockSemanticSha256}:{plan.LockSchemaVersion}:{plan.LockCanonicalization}",
                    $"canonical-lock-consistency:{observation.VariantId}");
            }
        }

        return Result(context, PlanFuzzOracleStatus.Passed, "All selected lock snapshots are internally consistent.", "equal");
    }

    private PlanFuzzOracleResult Result(
        PlanFuzzOracleContext context,
        PlanFuzzOracleStatus status,
        string summary,
        string fingerprintMaterial,
        string? classFingerprintMaterial = null) =>
        new(context.Contract.ContractId, OracleId, OracleVersion, status, summary, fingerprintMaterial, classFingerprintMaterial);
}
