namespace UniversalToolchain.PlanFuzz.Cli;

internal sealed record PlanFuzzWorkerResult(
    IReadOnlyList<PlanFuzzObservation> Observations,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);
