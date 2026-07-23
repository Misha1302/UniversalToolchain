namespace UniversalToolchain.PlanFuzz;

internal static class PlanFuzzObservationComparer
{
    public static bool AreSemanticallyEquivalent(PlanFuzzObservation left, PlanFuzzObservation right)
    {
        if (left.Outcome != right.Outcome)
            return false;

        if (left.Outcome == PlanFuzzExecutionOutcome.Success)
            return Equals(left.Value, right.Value);

        if (left.Outcome == PlanFuzzExecutionOutcome.ProgramFailure)
        {
            return left.Failure != null &&
                   right.Failure != null &&
                   StringComparer.Ordinal.Equals(left.Failure.FailureType, right.Failure.FailureType) &&
                   StringComparer.Ordinal.Equals(left.Failure.Stage, right.Failure.Stage) &&
                   StringComparer.Ordinal.Equals(left.Failure.Category, right.Failure.Category);
        }

        return false;
    }

    public static bool HasInfrastructureFailure(IEnumerable<PlanFuzzObservation> observations) =>
        observations.Any(static observation => observation.Outcome == PlanFuzzExecutionOutcome.InfrastructureFailure);

    public static bool HasTimeout(IEnumerable<PlanFuzzObservation> observations) =>
        observations.Any(static observation => observation.Outcome == PlanFuzzExecutionOutcome.Timeout);
}
