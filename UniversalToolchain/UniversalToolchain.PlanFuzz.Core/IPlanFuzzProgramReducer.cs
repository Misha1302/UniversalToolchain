namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Exposes adapter-owned structured program reduction without leaking language models into the generic core.
/// </summary>
public interface IPlanFuzzProgramReducer
{
    long GetProgramComplexity(PlanFuzzTestCase testCase);

    IReadOnlyList<PlanFuzzProgramReductionCandidate> GetProgramReductionCandidates(
        PlanFuzzTestCase testCase);
}
