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
    string FingerprintMaterial)
{
    public bool IsViolation => Status == PlanFuzzOracleStatus.Violated;
}
