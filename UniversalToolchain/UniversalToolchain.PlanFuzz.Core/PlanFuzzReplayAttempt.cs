namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Stores one fresh-process replay attempt and its oracle results.
/// </summary>
public sealed class PlanFuzzReplayAttempt
{
    public PlanFuzzReplayAttempt(
        int attemptNumber,
        IEnumerable<PlanFuzzObservation> observations,
        IEnumerable<PlanFuzzOracleResult> oracleResults)
    {
        if (attemptNumber <= 0)
            Thrower.Argument(nameof(attemptNumber), "Attempt number must be positive.");
        AttemptNumber = attemptNumber;
        Observations = new ReadOnlyCollection<PlanFuzzObservation>(observations.ArgNotNull()
            .OrderBy(static item => item.VariantId, StringComparer.Ordinal)
            .ToArray());
        OracleResults = new ReadOnlyCollection<PlanFuzzOracleResult>(oracleResults.ArgNotNull()
            .OrderBy(static item => item.ContractId, StringComparer.Ordinal)
            .ToArray());
        Fingerprint = ComputeFingerprint(OracleResults, useClassMaterial: false);
        ClassFingerprint = ComputeFingerprint(OracleResults, useClassMaterial: true);
    }

    public int AttemptNumber { get; }
    public IReadOnlyList<PlanFuzzObservation> Observations { get; }
    public IReadOnlyList<PlanFuzzOracleResult> OracleResults { get; }
    public string Fingerprint { get; }
    public string ClassFingerprint { get; }
    public bool HasViolation => OracleResults.Any(static result => result.IsViolation);
    public bool HasInfrastructureFailure =>
        Observations.Any(static observation =>
            observation.Outcome is PlanFuzzExecutionOutcome.InfrastructureFailure or PlanFuzzExecutionOutcome.Timeout) ||
        OracleResults.Any(static result => result.Status == PlanFuzzOracleStatus.InfrastructureFailure);

    private static string ComputeFingerprint(
        IEnumerable<PlanFuzzOracleResult> results,
        bool useClassMaterial)
    {
        var material = string.Join(
            "\n",
            results.Where(static result => result.IsViolation)
                .OrderBy(static result => result.ContractId, StringComparer.Ordinal)
                .Select(result => $"{result.ContractId}|{result.OracleId}|{result.OracleVersion}|" +
                    (useClassMaterial ? result.EffectiveClassFingerprintMaterial : result.FingerprintMaterial)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
    }
}
