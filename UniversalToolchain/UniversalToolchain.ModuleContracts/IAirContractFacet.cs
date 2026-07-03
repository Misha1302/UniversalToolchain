namespace UniversalToolchain.ModuleContracts;

public interface IAirContractFacet : IModuleContractFacet
{
    IReadOnlyList<AirEmissionContract> AirEmissions { get; }
}
