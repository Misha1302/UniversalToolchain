namespace UniversalToolchain.PlanFuzz;

public enum PlanFuzzExecutionOutcome
{
    Success,
    ProgramFailure,
    Timeout,
    InfrastructureFailure
}
