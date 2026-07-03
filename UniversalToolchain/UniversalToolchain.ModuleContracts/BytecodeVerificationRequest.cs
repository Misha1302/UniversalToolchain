namespace UniversalToolchain.ModuleContracts;

public sealed record BytecodeVerificationRequest(
    Bytecode Bytecode,
    SelectedModuleContractTable ContractTable,
    VerificationSeverityProfile Profile,
    IReadOnlyList<ObservedBytecodeEmission>? ObservedEmissions = null,
    bool VerifyLegacyOperationNames = true);
