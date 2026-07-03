namespace UniversalToolchain.ModuleContracts;

public interface IBackendCapabilityFacet : IModuleContractFacet
{
    IReadOnlyList<BackendCapabilityContract> Capabilities { get; }
}
