namespace UniversalToolchain.ModuleContracts;

public interface IModuleContractFacet
{
    ModuleId ModuleId { get; }

    ContractFacetKind Kind { get; }

    ContractSchemaVersion SchemaVersion { get; }
}
