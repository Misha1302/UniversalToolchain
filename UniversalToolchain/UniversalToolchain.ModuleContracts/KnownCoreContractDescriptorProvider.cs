namespace UniversalToolchain.ModuleContracts;

public sealed class KnownCoreContractDescriptorProvider : IModuleContractDescriptorProvider
{
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => [ContractNamespaceOwner.Core];

    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        KnownCoreCompilerFacts.CreateOwnershipFacet(),
        KnownCoreBackendCapabilities.CreateFacet()
    ];
}
