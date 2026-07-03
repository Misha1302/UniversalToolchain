namespace UniversalToolchain.ModuleContracts;

public sealed record BytecodeContractFacet(
    ModuleId ModuleId,
    IReadOnlyList<BytecodeEmissionContract> BytecodeEmissions) : IBytecodeContractFacet
{
    public ContractFacetKind Kind => ContractFacetKind.Bytecode;

    public ContractSchemaVersion SchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;
}
