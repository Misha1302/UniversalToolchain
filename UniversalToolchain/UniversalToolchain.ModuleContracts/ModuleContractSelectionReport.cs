namespace UniversalToolchain.ModuleContracts;

public sealed record ModuleContractSelectionReport(
    SelectedModuleContractTable ContractTable,
    IReadOnlyList<SelectedModuleContractStatus> ModuleStatuses,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);
