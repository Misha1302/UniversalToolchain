namespace UniversalToolchain.ModuleContracts;

public sealed record SelectedModuleContractStatus(
    ModuleId ModuleId,
    ModuleContractCompatibilityStatus Status);
