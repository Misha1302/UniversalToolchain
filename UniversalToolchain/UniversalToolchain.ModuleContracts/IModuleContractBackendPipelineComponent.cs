namespace UniversalToolchain.ModuleContracts;

public interface IModuleContractBackendPipelineComponent : IBackendPipelineComponent
{
    IReadOnlyList<IModuleContractDescriptorProvider> DescriptorProviders { get; }
}
