namespace UniversalToolchain.ModuleContracts;

public interface IBytecodeContractFacet : IModuleContractFacet
{
    IReadOnlyList<BytecodeEmissionContract> BytecodeEmissions { get; }
}
