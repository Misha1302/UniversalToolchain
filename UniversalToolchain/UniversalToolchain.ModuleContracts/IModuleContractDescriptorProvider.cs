namespace UniversalToolchain.ModuleContracts;

public interface IModuleContractDescriptorProvider
{
    /// <summary>
    ///     Namespace reservations owned by the provider's declared module facets.
    ///     Providers that do not declare ownership remain compatible but receive no ownership validation.
    /// </summary>
    IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => [];

    IReadOnlyList<IModuleContractFacet> GetFacets();
}
