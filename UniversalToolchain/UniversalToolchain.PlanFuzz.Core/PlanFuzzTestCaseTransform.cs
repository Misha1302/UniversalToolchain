namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Rebuilds immutable testcase snapshots while preserving generation provenance and schema identity.
/// </summary>
public static class PlanFuzzTestCaseTransform
{
    public static PlanFuzzTestCase WithProgram(
        PlanFuzzTestCase testCase,
        PlanFuzzProgram program) =>
        Rebuild(testCase, program, testCase.Variants, testCase.OracleContracts);

    public static PlanFuzzTestCase WithContractsAndReferencedVariants(
        PlanFuzzTestCase testCase,
        IEnumerable<PlanFuzzOracleContract> oracleContracts)
    {
        testCase = testCase.ArgNotNull();
        var contracts = oracleContracts.ArgNotNull()
            .OrderBy(static contract => contract.ContractId, StringComparer.Ordinal)
            .ToArray();
        if (contracts.Length == 0)
            return Thrower.Argument<PlanFuzzTestCase>(nameof(oracleContracts), "Reduced testcase must retain at least one oracle contract.");

        var referencedVariantIds = contracts
            .SelectMany(static contract => contract.VariantIds)
            .ToHashSet(StringComparer.Ordinal);
        var variants = testCase.Variants
            .Where(variant => referencedVariantIds.Contains(variant.VariantId))
            .ToArray();
        return Rebuild(testCase, testCase.Program, variants, contracts);
    }

    private static PlanFuzzTestCase Rebuild(
        PlanFuzzTestCase testCase,
        PlanFuzzProgram program,
        IEnumerable<PlanFuzzPlanVariant> variants,
        IEnumerable<PlanFuzzOracleContract> oracleContracts)
    {
        testCase = testCase.ArgNotNull();
        return new PlanFuzzTestCase(
            testCase.SchemaVersion,
            testCase.AdapterId,
            testCase.AdapterVersion,
            testCase.CampaignSeed,
            testCase.CaseIndex,
            testCase.CaseSeed,
            testCase.PrngAlgorithm,
            program.ArgNotNull(),
            variants.ArgNotNull(),
            oracleContracts.ArgNotNull());
    }
}
