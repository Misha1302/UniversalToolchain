namespace UniversalToolchain.ModuleContracts;

public sealed record AstContractFacet(
    ModuleId ModuleId,
    IReadOnlyList<AstOwnershipContract> AstOwnership) : IAstContractFacet
{
    public ContractFacetKind Kind => ContractFacetKind.Ast;

    public ContractSchemaVersion SchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;
}
