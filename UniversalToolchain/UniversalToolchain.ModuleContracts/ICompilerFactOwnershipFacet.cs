namespace UniversalToolchain.ModuleContracts;

public interface ICompilerFactOwnershipFacet : IModuleContractFacet
{
    IReadOnlyList<CompilerFactOwnershipContract> Facts { get; }
}
