namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Compares two backend observations over one adapter-declared shared semantic subset.
/// </summary>
public sealed class BackendParityOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.BackendParity;
    public int OracleVersion => 1;

    public PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context)
    {
        context = context.ArgNotNull();
        if (context.Contract.VariantIds.Count != 2)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "Backend parity requires exactly two variants.", "invalid-arity");
        if (!TryGetPair(context, out var first, out var second))
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "One or more required observations are missing.", "missing-observation");

        var pair = new[] { first, second };
        if (PlanFuzzObservationComparer.HasInfrastructureFailure(pair))
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "A worker or adapter infrastructure failure prevents parity evaluation.", "infrastructure");
        if (PlanFuzzObservationComparer.HasTimeout(pair))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "A worker timeout prevents parity evaluation.", "timeout");

        if (PlanFuzzObservationComparer.AreSemanticallyEquivalent(first, second))
        {
            if (context.TestCase.Program.ProgramClass == PlanFuzzProgramClass.ValidDeterministic &&
                first.Outcome != PlanFuzzExecutionOutcome.Success)
            {
                return Result(
                    context,
                    PlanFuzzOracleStatus.Violated,
                    "A valid deterministic testcase failed on both backends.",
                    $"unexpected-shared-failure:{Describe(first)}|{Describe(second)}");
            }

            return Result(context, PlanFuzzOracleStatus.Passed, "Backend observations are semantically equivalent.", "equal");
        }

        return Result(
            context,
            PlanFuzzOracleStatus.Violated,
            $"Backend parity mismatch between '{first.VariantId}' and '{second.VariantId}'.",
            $"{Describe(first)}|{Describe(second)}");
    }

    private static bool TryGetPair(
        PlanFuzzOracleContext context,
        out PlanFuzzObservation first,
        out PlanFuzzObservation second)
    {
        var firstFound = context.TryGetObservation(context.Contract.VariantIds[0], out first!);
        var secondFound = context.TryGetObservation(context.Contract.VariantIds[1], out second!);
        return firstFound && secondFound;
    }

    private static string Describe(PlanFuzzObservation observation) =>
        observation.Outcome == PlanFuzzExecutionOutcome.Success
            ? $"{observation.BackendId}:success:{observation.Value?.TypeIdentity}:{observation.Value?.CanonicalValue}"
            : $"{observation.BackendId}:{observation.Outcome}:{observation.Failure?.FailureType}:{observation.Failure?.Stage}:{observation.Failure?.Category}";

    private PlanFuzzOracleResult Result(
        PlanFuzzOracleContext context,
        PlanFuzzOracleStatus status,
        string summary,
        string fingerprintMaterial) =>
        new(context.Contract.ContractId, OracleId, OracleVersion, status, summary, fingerprintMaterial);
}
