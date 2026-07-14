namespace UniversalToolchain.ModuleContracts;

public interface ISelectedModuleContractTableProvider
{
    ModuleContractSelectionReport? Build(
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IAirOptimizer> optimizers) =>
        Build(frontendModules, optimizers, []);

    ModuleContractSelectionReport? Build(
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IAirOptimizer> optimizers,
        IReadOnlyList<IBackendPipelineComponent> backendComponents);
}
