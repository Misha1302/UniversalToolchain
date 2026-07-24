namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Compares a baseline execution with one declared semantics-preserving optimization or lowering route.
/// </summary>
public sealed class OptimizationRouteParityOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.OptimizationRouteParity;
    public int OracleVersion => 1;

    public PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context)
    {
        context = context.ArgNotNull();
        if (context.Contract.VariantIds.Count != 2)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "Optimization-route parity requires exactly two variants.", "invalid-arity");
        if (!context.TryGetObservation(context.Contract.VariantIds[0], out var baseline) ||
            !context.TryGetObservation(context.Contract.VariantIds[1], out var routed))
        {
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "One or more required observations are missing.", "missing-observation");
        }

        var pair = new[] { baseline, routed };
        if (PlanFuzzObservationComparer.HasInfrastructureFailure(pair))
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "Infrastructure failure prevents route-parity evaluation.", "infrastructure");
        if (PlanFuzzObservationComparer.HasTimeout(pair))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Worker timeout prevents route-parity evaluation.", "timeout");
        if (routed.Route == null)
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "The routed variant did not publish route evidence.", "missing-route");

        if (PlanFuzzObservationComparer.AreSemanticallyEquivalent(baseline, routed))
        {
            if (context.TestCase.Program.ProgramClass == PlanFuzzProgramClass.ValidDeterministic &&
                baseline.Outcome != PlanFuzzExecutionOutcome.Success)
            {
                return Result(
                    context,
                    PlanFuzzOracleStatus.Violated,
                    "A valid deterministic testcase failed on both baseline and routed executions.",
                    $"unexpected-shared-failure:{Describe(baseline)}|{Describe(routed)}",
                    $"unexpected-shared-failure:{DescribeClass(baseline)}|{DescribeClass(routed)}");
            }

            return Result(context, PlanFuzzOracleStatus.Passed, "Baseline and routed observations are semantically equivalent.", "equal");
        }

        return Result(
            context,
            PlanFuzzOracleStatus.Violated,
            $"Optimization-route parity mismatch between '{baseline.VariantId}' and '{routed.VariantId}'.",
            $"{Describe(baseline)}|{Describe(routed)}|route:{DescribeRoute(routed.Route)}",
            $"{DescribeClass(baseline)}|{DescribeClass(routed)}|route:{DescribeRouteClass(routed.Route)}");
    }

    private static string Describe(PlanFuzzObservation observation) =>
        observation.Outcome == PlanFuzzExecutionOutcome.Success
            ? $"{observation.BackendId}:success:{observation.Value?.TypeIdentity}:{observation.Value?.CanonicalValue}"
            : $"{observation.BackendId}:{observation.Outcome}:{observation.Failure?.FailureType}:{observation.Failure?.Stage}:{observation.Failure?.Category}";

    private static string DescribeClass(PlanFuzzObservation observation) =>
        observation.Outcome == PlanFuzzExecutionOutcome.Success
            ? $"{observation.BackendId}:success:{observation.Value?.TypeIdentity}"
            : $"{observation.BackendId}:{observation.Outcome}:{observation.Failure?.FailureType}:{observation.Failure?.Stage}:{observation.Failure?.Category}";

    private static string DescribeRoute(PlanFuzzRouteSnapshot route) =>
        $"{route.RouteId}:{route.RequestedPolicy}:used={route.UsedRoute}:fallback={route.FellBack}:{route.FallbackKind}:" +
        string.Join(',', route.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}@{diagnostic.Stage}"));

    private static string DescribeRouteClass(PlanFuzzRouteSnapshot route) =>
        $"{route.RouteId}:{route.RequestedPolicy}:used={route.UsedRoute}:fallback={route.FellBack}:{route.FallbackKind}:" +
        string.Join(',', route.Diagnostics
            .Select(static diagnostic => $"{diagnostic.Code}@{diagnostic.Stage}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal));

    private PlanFuzzOracleResult Result(
        PlanFuzzOracleContext context,
        PlanFuzzOracleStatus status,
        string summary,
        string fingerprintMaterial,
        string? classFingerprintMaterial = null) =>
        new(
            context.Contract.ContractId,
            OracleId,
            OracleVersion,
            status,
            summary,
            fingerprintMaterial,
            classFingerprintMaterial);
}
