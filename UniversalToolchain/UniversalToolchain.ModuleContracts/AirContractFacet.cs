namespace UniversalToolchain.ModuleContracts;

public sealed record AirContractFacet(
    ModuleId ModuleId,
    IReadOnlyList<AirEmissionContract> AirEmissions) : IAirContractFacet
{
    public ContractFacetKind Kind => ContractFacetKind.Air;

    public ContractSchemaVersion SchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;
}
