namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Describes one deterministic adapter-owned program candidate that is strictly simpler than its source model.
/// </summary>
public sealed class PlanFuzzProgramReductionCandidate
{
    public PlanFuzzProgramReductionCandidate(
        string candidateId,
        string summary,
        long complexity,
        PlanFuzzProgram program)
    {
        if (string.IsNullOrWhiteSpace(candidateId))
            Thrower.Argument(nameof(candidateId), "Reduction candidate ID must not be empty.");
        if (string.IsNullOrWhiteSpace(summary))
            Thrower.Argument(nameof(summary), "Reduction candidate summary must not be empty.");
        if (complexity < 0)
            Thrower.Argument(nameof(complexity), "Reduction candidate complexity must not be negative.");

        CandidateId = candidateId;
        Summary = summary;
        Complexity = complexity;
        Program = program.ArgNotNull();
    }

    public string CandidateId { get; }
    public string Summary { get; }
    public long Complexity { get; }
    public PlanFuzzProgram Program { get; }
}
