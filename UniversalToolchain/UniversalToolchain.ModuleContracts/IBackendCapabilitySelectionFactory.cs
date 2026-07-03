namespace UniversalToolchain.ModuleContracts;

public interface IBackendCapabilitySelectionFactory
{
    BackendCapabilitySelection Create(
        SelectedModuleContractTable table,
        IReadOnlyList<string> compilerSupportedIntrinsics);
}
