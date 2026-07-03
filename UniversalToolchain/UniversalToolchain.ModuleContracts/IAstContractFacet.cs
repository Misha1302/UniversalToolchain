namespace UniversalToolchain.ModuleContracts;

public interface IAstContractFacet : IModuleContractFacet
{
    IReadOnlyList<AstOwnershipContract> AstOwnership { get; }
}
