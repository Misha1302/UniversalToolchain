namespace UniversalToolchain.ModuleContracts;

public sealed record OptimizerAirValidationRequest(
    string OptimizerId,
    IAbstractIR OptimizedAir,
    SelectedModuleContractTable ContractTable,
    BackendCapabilitySelection BackendSelection,
    VerificationSeverityProfile Profile);
