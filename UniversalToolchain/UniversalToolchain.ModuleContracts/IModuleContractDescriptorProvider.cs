namespace UniversalToolchain.ModuleContracts;

public interface IModuleContractDescriptorProvider
{
    IReadOnlyList<IModuleContractFacet> GetFacets();
}
