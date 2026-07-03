namespace UniversalToolchain.ModuleContracts;

public interface ISelectedModuleContractTableProvider
{
    ModuleContractSelectionReport? Build(
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IIRProcessingModule> optimizers) =>
        Build(frontendModules, optimizers, []);

    ModuleContractSelectionReport? Build(
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IIRProcessingModule> optimizers,
        IReadOnlyList<IBackendPipelineComponent> backendComponents);
}
