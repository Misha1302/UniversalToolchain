namespace UniversalToolchain.ModuleContracts;

public sealed record ModuleContractPipelineOptions
{
    public bool Enabled { get; init; } = true;

    public ModuleContractVerificationPolicy VerificationPolicy { get; init; } =
        ModuleContractVerificationPolicy.P3Always;

    /// <summary>
    /// Facts explicitly queried by a downstream consumer under the demand-recomputation baseline.
    /// Other policies ignore this set.
    /// </summary>
    public IReadOnlySet<CompilerFactId> DemandedFacts { get; init; } = new HashSet<CompilerFactId>();

    public required VerificationSeverityProfile BytecodeProfile { get; init; }

    public required VerificationSeverityProfile AirProfile { get; init; }

    public required ModuleContractEnforcementPolicy EnforcementPolicy { get; init; }

    public required AirBackendPolicy BackendPolicy { get; init; }
}
