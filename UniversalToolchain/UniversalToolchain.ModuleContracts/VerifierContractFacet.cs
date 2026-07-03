namespace UniversalToolchain.ModuleContracts;

public sealed record VerifierContractFacet(
    ModuleId ModuleId,
    IReadOnlyList<VerifierRuleContribution> Rules) : IVerifierContractFacet
{
    public ContractFacetKind Kind => ContractFacetKind.Verifier;

    public ContractSchemaVersion SchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;
}
