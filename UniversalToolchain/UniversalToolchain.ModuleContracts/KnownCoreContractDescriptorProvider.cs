namespace UniversalToolchain.ModuleContracts;

public sealed class KnownCoreContractDescriptorProvider : IModuleContractDescriptorProvider
{
    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        KnownCoreCompilerFacts.CreateOwnershipFacet(),
        KnownCoreBackendCapabilities.CreateFacet()
    ];
}
