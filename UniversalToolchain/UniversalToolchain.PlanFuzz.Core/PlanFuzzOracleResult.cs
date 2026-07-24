namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Records one oracle decision without collapsing not-applicable or inconclusive outcomes into a pass.
/// </summary>
public sealed record PlanFuzzOracleResult(
    string ContractId,
    string OracleId,
    int OracleVersion,
    PlanFuzzOracleStatus Status,
    string Summary,
    string FingerprintMaterial,
    string? ClassFingerprintMaterial = null)
{
    public bool IsViolation => Status == PlanFuzzOracleStatus.Violated;

    /// <summary>
    /// Returns a coarser stable discriminator used to group testcase-level findings into defect classes.
    /// Exact replay confirmation continues to use <see cref="FingerprintMaterial"/>.
    /// </summary>
    public string EffectiveClassFingerprintMaterial =>
        string.IsNullOrWhiteSpace(ClassFingerprintMaterial)
            ? FingerprintMaterial
            : ClassFingerprintMaterial;
}
