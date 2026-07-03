namespace UniversalToolchain.ModuleContracts;

public sealed record BackendCapabilityFacet(
    ModuleId ModuleId,
    IReadOnlyList<BackendCapabilityContract> Capabilities) : IBackendCapabilityFacet
{
    public ContractFacetKind Kind => ContractFacetKind.BackendCapability;

    public ContractSchemaVersion SchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;
}
