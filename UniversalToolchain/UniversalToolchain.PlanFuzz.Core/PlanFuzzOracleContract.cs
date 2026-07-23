namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Selects one oracle implementation and the exact variants it may compare.
/// </summary>
public sealed class PlanFuzzOracleContract
{
    public PlanFuzzOracleContract(
        string contractId,
        string oracleId,
        int oracleVersion,
        IEnumerable<string> variantIds)
    {
        if (string.IsNullOrWhiteSpace(contractId))
            Thrower.Argument(nameof(contractId), "Oracle contract ID must not be empty.");
        if (string.IsNullOrWhiteSpace(oracleId))
            Thrower.Argument(nameof(oracleId), "Oracle ID must not be empty.");
        if (oracleVersion <= 0)
            Thrower.Argument(nameof(oracleVersion), "Oracle version must be positive.");

        var variants = variantIds.ArgNotNull()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (variants.Length == 0)
            Thrower.Argument(nameof(variantIds), "Oracle contract must reference at least one variant.");

        ContractId = contractId;
        OracleId = oracleId;
        OracleVersion = oracleVersion;
        VariantIds = new ReadOnlyCollection<string>(variants);
    }

    public string ContractId { get; }
    public string OracleId { get; }
    public int OracleVersion { get; }
    public IReadOnlyList<string> VariantIds { get; }
}
