namespace UniversalToolchain.ModuleContracts;

public static class ModuleContractPipelineProfiles
{
    public static ModuleContractPipelineOptions Observe => new()
    {
        BytecodeProfile = VerificationSeverityProfile.Observe,
        AirProfile = VerificationSeverityProfile.Observe,
        EnforcementPolicy = ModuleContractEnforcementPolicy.AllowUndeclared,
        BackendPolicy = AirBackendPolicy.CapabilityGated,
        VerifyLegacyBytecodeOperationNames = false
    };

    public static ModuleContractPipelineOptions Warn => new()
    {
        BytecodeProfile = VerificationSeverityProfile.Warn,
        AirProfile = VerificationSeverityProfile.Warn,
        EnforcementPolicy = ModuleContractEnforcementPolicy.AllowUndeclared,
        BackendPolicy = AirBackendPolicy.CapabilityGated,
        VerifyLegacyBytecodeOperationNames = false
    };

    public static ModuleContractPipelineOptions StrictEnforced => new()
    {
        BytecodeProfile = VerificationSeverityProfile.Strict,
        AirProfile = VerificationSeverityProfile.Strict,
        EnforcementPolicy = ModuleContractEnforcementPolicy.EnforceNewModules([]),
        BackendPolicy = AirBackendPolicy.CapabilityGated,
        VerifyLegacyBytecodeOperationNames = false
    };

    public static ModuleContractPipelineOptions StrictUniversalInterpreter => new()
    {
        BytecodeProfile = VerificationSeverityProfile.Strict,
        AirProfile = VerificationSeverityProfile.Strict,
        EnforcementPolicy = ModuleContractEnforcementPolicy.EnforceNewModules([]),
        BackendPolicy = AirBackendPolicy.UniversalInterpreter,
        VerifyLegacyBytecodeOperationNames = false
    };
}
