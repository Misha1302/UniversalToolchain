namespace UniversalToolchain.ModuleContracts;

public interface IPipelineEffectContractFacet : IModuleContractFacet
{
    IReadOnlyList<PipelineEffectContract> Effects { get; }
}
