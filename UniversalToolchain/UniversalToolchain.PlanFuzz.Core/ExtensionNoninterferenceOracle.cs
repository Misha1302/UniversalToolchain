namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Verifies that one explicitly declared independent, unused surface extension changes neither semantics nor selected execution route.
/// </summary>
public sealed class ExtensionNoninterferenceOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.ExtensionNoninterference;
    public int OracleVersion => 1;

    public PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context)
    {
        context = context.ArgNotNull();
        if (context.Contract.VariantIds.Count != 2)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "Extension noninterference requires exactly two variants.", "invalid-arity");
        if (!context.TryGetObservation(context.Contract.VariantIds[0], out var baseline) ||
            !context.TryGetObservation(context.Contract.VariantIds[1], out var extended))
        {
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "One or more required observations are missing.", "missing-observation");
        }

        var pair = new[] { baseline, extended };
        if (PlanFuzzObservationComparer.HasInfrastructureFailure(pair))
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "Infrastructure failure prevents extension-noninterference evaluation.", "infrastructure");
        if (PlanFuzzObservationComparer.HasTimeout(pair))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Worker timeout prevents extension-noninterference evaluation.", "timeout");
        if (baseline.Surface == null || extended.Surface == null)
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "One or more variants did not publish surface evidence.", "missing-surface");
        if (!baseline.Surface.ActivationTraceComplete || !extended.Surface.ActivationTraceComplete)
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Extension noninterference requires complete activation traces.", "incomplete-trace");
        if (!StringComparer.Ordinal.Equals(baseline.Surface.TraceKind, extended.Surface.TraceKind))
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "Surface traces use different evidence contracts.", "trace-kind-mismatch");

        var removed = baseline.Surface.SelectedSurfaceIds
            .Except(extended.Surface.SelectedSurfaceIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var added = extended.Surface.SelectedSurfaceIds
            .Except(baseline.Surface.SelectedSurfaceIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        if (removed.Length != 0 || added.Length == 0)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "The compared variants are not a pure additive extension pair.", $"invalid-delta:removed={string.Join(',', removed)}:added={string.Join(',', added)}");

        var undeclared = added
            .Except(extended.Surface.DeclaredIndependentSurfaceIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        if (undeclared.Length != 0)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "The added surface is not fully declared independent.", $"undeclared:{string.Join(',', undeclared)}");

        var activatedAdded = added
            .Intersect(extended.Surface.ActivatedOwnerIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        if (activatedAdded.Length != 0)
        {
            var material = string.Join(',', activatedAdded);
            return Result(
                context,
                PlanFuzzOracleStatus.Violated,
                "The declared unused extension became active.",
                $"extension-activated:{material}",
                $"extension-activated:{material}");
        }

        if (!StringComparer.Ordinal.Equals(baseline.Surface.RouteIdentity, extended.Surface.RouteIdentity))
        {
            return Result(
                context,
                PlanFuzzOracleStatus.Violated,
                "The declared unused extension changed the selected execution route.",
                $"route:{baseline.Surface.RouteIdentity}|{extended.Surface.RouteIdentity}",
                "extension-route-changed");
        }

        if (!baseline.Surface.ActivatedOwnerIds.SequenceEqual(extended.Surface.ActivatedOwnerIds, StringComparer.Ordinal))
        {
            return Result(
                context,
                PlanFuzzOracleStatus.Violated,
                "The declared unused extension changed activation behavior.",
                $"activation:{string.Join(',', baseline.Surface.ActivatedOwnerIds)}|{string.Join(',', extended.Surface.ActivatedOwnerIds)}",
                "extension-activation-changed");
        }

        if (!PlanFuzzObservationComparer.AreSemanticallyEquivalent(baseline, extended))
        {
            return Result(
                context,
                PlanFuzzOracleStatus.Violated,
                "The declared unused extension changed observable semantics.",
                $"behavior:{Describe(baseline)}|{Describe(extended)}",
                $"behavior:{DescribeClass(baseline)}|{DescribeClass(extended)}");
        }

        return Result(context, PlanFuzzOracleStatus.Passed, "The declared unused extension preserves semantics, route and activation behavior.", "equal");
    }

    private static string Describe(PlanFuzzObservation observation) =>
        observation.Outcome == PlanFuzzExecutionOutcome.Success
            ? $"{observation.BackendId}:success:{observation.Value?.TypeIdentity}:{observation.Value?.CanonicalValue}"
            : $"{observation.BackendId}:{observation.Outcome}:{observation.Failure?.FailureType}:{observation.Failure?.Stage}:{observation.Failure?.Category}";

    private static string DescribeClass(PlanFuzzObservation observation) =>
        observation.Outcome == PlanFuzzExecutionOutcome.Success
            ? $"{observation.BackendId}:success:{observation.Value?.TypeIdentity}"
            : $"{observation.BackendId}:{observation.Outcome}:{observation.Failure?.FailureType}:{observation.Failure?.Stage}:{observation.Failure?.Category}";

    private PlanFuzzOracleResult Result(
        PlanFuzzOracleContext context,
        PlanFuzzOracleStatus status,
        string summary,
        string fingerprintMaterial,
        string? classFingerprintMaterial = null) =>
        new(context.Contract.ContractId, OracleId, OracleVersion, status, summary, fingerprintMaterial, classFingerprintMaterial);
}
