namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Represents one fully replayable generated testcase before observations are attached.
/// </summary>
public sealed class PlanFuzzTestCase
{
    public PlanFuzzTestCase(
        int schemaVersion,
        string adapterId,
        string adapterVersion,
        ulong campaignSeed,
        long caseIndex,
        ulong caseSeed,
        string prngAlgorithm,
        PlanFuzzProgram program,
        IEnumerable<PlanFuzzPlanVariant> variants,
        IEnumerable<PlanFuzzOracleContract> oracleContracts)
    {
        if (schemaVersion != PlanFuzzConstants.CaseSchemaVersion)
            Thrower.Argument(nameof(schemaVersion), $"Unsupported testcase schema version '{schemaVersion}'.");
        if (string.IsNullOrWhiteSpace(adapterId))
            Thrower.Argument(nameof(adapterId), "Adapter ID must not be empty.");
        if (string.IsNullOrWhiteSpace(adapterVersion))
            Thrower.Argument(nameof(adapterVersion), "Adapter version must not be empty.");
        if (caseIndex < 0)
            Thrower.Argument(nameof(caseIndex), "Case index must not be negative.");
        if (string.IsNullOrWhiteSpace(prngAlgorithm))
            Thrower.Argument(nameof(prngAlgorithm), "PRNG algorithm ID must not be empty.");

        var variantSnapshot = variants.ArgNotNull().OrderBy(static item => item.VariantId, StringComparer.Ordinal).ToArray();
        if (variantSnapshot.Length == 0)
            Thrower.Argument(nameof(variants), "Testcase must contain at least one variant.");
        if (variantSnapshot.Select(static item => item.VariantId).Distinct(StringComparer.Ordinal).Count() != variantSnapshot.Length)
            Thrower.Argument(nameof(variants), "Variant IDs must be unique.");

        var oracleSnapshot = oracleContracts.ArgNotNull().OrderBy(static item => item.ContractId, StringComparer.Ordinal).ToArray();
        if (oracleSnapshot.Select(static item => item.ContractId).Distinct(StringComparer.Ordinal).Count() != oracleSnapshot.Length)
            Thrower.Argument(nameof(oracleContracts), "Oracle contract IDs must be unique.");

        var variantIds = variantSnapshot.Select(static item => item.VariantId).ToHashSet(StringComparer.Ordinal);
        foreach (var contract in oracleSnapshot)
        {
            if (contract.VariantIds.Any(id => !variantIds.Contains(id)))
                Thrower.Argument(nameof(oracleContracts), $"Oracle contract '{contract.ContractId}' references an unknown variant.");
        }

        SchemaVersion = schemaVersion;
        AdapterId = adapterId;
        AdapterVersion = adapterVersion;
        CampaignSeed = campaignSeed;
        CaseIndex = caseIndex;
        CaseSeed = caseSeed;
        PrngAlgorithm = prngAlgorithm;
        Program = program.ArgNotNull();
        Variants = new ReadOnlyCollection<PlanFuzzPlanVariant>(variantSnapshot);
        OracleContracts = new ReadOnlyCollection<PlanFuzzOracleContract>(oracleSnapshot);
    }

    public int SchemaVersion { get; }
    public string AdapterId { get; }
    public string AdapterVersion { get; }
    public ulong CampaignSeed { get; }
    public long CaseIndex { get; }
    public ulong CaseSeed { get; }
    public string PrngAlgorithm { get; }
    public PlanFuzzProgram Program { get; }
    public IReadOnlyList<PlanFuzzPlanVariant> Variants { get; }
    public IReadOnlyList<PlanFuzzOracleContract> OracleContracts { get; }
    public string CaseId => PlanFuzzTestCaseSerializer.ComputeCaseId(this);

    public PlanFuzzPlanVariant GetRequiredVariant(string variantId)
    {
        var variant = Variants.SingleOrDefault(item => StringComparer.Ordinal.Equals(item.VariantId, variantId));
        return variant ?? Thrower.InvalidOpEx<PlanFuzzPlanVariant>($"Unknown testcase variant '{variantId}'.");
    }
}
