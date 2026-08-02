namespace UniversalToolchain.ModuleContracts;

public sealed record AirVerificationRequest(
    IAbstractIR Air,
    SelectedModuleContractTable ContractTable,
    BackendCapabilitySelection BackendSelection,
    VerificationSeverityProfile Profile,
    AirVerificationScope Scope = AirVerificationScope.Full);
