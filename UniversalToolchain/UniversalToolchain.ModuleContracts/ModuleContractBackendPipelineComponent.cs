namespace UniversalToolchain.ModuleContracts;

public sealed class ModuleContractBackendPipelineComponent(
    string componentId,
    IReadOnlyList<IModuleContractDescriptorProvider> descriptorProviders) : IModuleContractBackendPipelineComponent
{
    public string ComponentId { get; } = string.IsNullOrWhiteSpace(componentId)
        ? throw new ArgumentException("Backend pipeline component id cannot be empty.", nameof(componentId))
        : componentId;

    public IReadOnlyList<IModuleContractDescriptorProvider> DescriptorProviders { get; } =
        descriptorProviders.ArgNotNull();
}
