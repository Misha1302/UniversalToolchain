namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Captures deterministic plan and lock identities needed by plan-level oracles.
/// </summary>
public sealed record PlanFuzzPlanSnapshot(
    string PlanHash,
    string CanonicalLockSha256,
    string RepeatedCanonicalLockSha256,
    string CanonicalLockSemanticSha256,
    string PrettyLockSemanticSha256,
    int LockSchemaVersion,
    string LockCanonicalization);
