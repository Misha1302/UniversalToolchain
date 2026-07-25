namespace UniversalToolchain.ModuleContracts;

public sealed record ModuleContractPipelineOptions
{
    public bool Enabled { get; init; } = true;

    public required VerificationSeverityProfile BytecodeProfile { get; init; }

    public required VerificationSeverityProfile AirProfile { get; init; }

    public required ModuleContractEnforcementPolicy EnforcementPolicy { get; init; }

    public required AirBackendPolicy BackendPolicy { get; init; }

    public required bool VerifyLegacyBytecodeOperationNames { get; init; }
}
