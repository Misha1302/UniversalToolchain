namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Verifies that one explicitly declared independent, unused extension changes neither semantics nor selected execution route.
/// </summary>
public sealed class ExtensionNoninterferenceOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.ExtensionNoninterference;
    public int OracleVersion => 2;

    public PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context)
    {
        context = context.ArgNotNull();
        if (context.Contract.VariantIds.Count != 2)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "Extension noninterference requires exactly two variants.", "invalid-arity");

        var observations = new List<PlanFuzzObservation>();
        foreach (var variantId in context.Contract.VariantIds)
        {
            if (!context.TryGetObservation(variantId, out var observation))
                return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, $"Observation '{variantId}' is missing.", $"missing-observation:{variantId}");
            observations.Add(observation);
        }

        var pair = observations.ToArray();
        if (PlanFuzzObservationComparer.HasInfrastructureFailure(pair))
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "Infrastructure failure prevents extension-noninterference evaluation.", "infrastructure");
        if (PlanFuzzObservationComparer.HasTimeout(pair))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Worker timeout prevents extension-noninterference evaluation.", "timeout");
        if (pair.Any(static observation => observation.Surface == null))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "One or more variants did not publish surface evidence.", "missing-surface");
        if (pair.Any(static observation => observation.Surface!.EvidenceContractVersion != PlanFuzzSurfaceSnapshot.CurrentEvidenceContractVersion))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Extension noninterference requires the current surface evidence contract.", "legacy-evidence");
        if (pair.Any(static observation => observation.Surface!.ActivationTraceStatus != PlanFuzzActivationTraceStatus.Complete))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Extension noninterference requires complete activation traces.", "incomplete-trace");

        var first = observations[0];
        var second = observations[1];
        var firstToSecond = DescribeAdditiveDelta(first.Surface!, second.Surface!);
        var secondToFirst = DescribeAdditiveDelta(second.Surface!, first.Surface!);
        var directions = new[]
        {
            (Baseline: first, Extended: second, Delta: firstToSecond),
            (Baseline: second, Extended: first, Delta: secondToFirst)
        }.Where(static direction => direction.Delta.IsPureAdditive).ToArray();

        if (directions.Length == 0)
        {
            if (firstToSecond.IsEqual && secondToFirst.IsEqual)
                return Result(context, PlanFuzzOracleStatus.NotApplicable, "The compared variants contain no extension delta.", "no-delta");
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "The O-005 contract does not describe one unambiguous pure additive extension pair.", "invalid-or-ambiguous-delta");
        }
        if (directions.Length != 1)
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "The O-005 contract has an ambiguous extension direction.", "ambiguous-delta");

        var direction = directions[0];
        var baseline = direction.Baseline;
        var extended = direction.Extended;
        var addedSurfaces = direction.Delta.AddedSurfaces;
        var addedOwners = direction.Delta.AddedOwners;

        if (!StringComparer.Ordinal.Equals(baseline.Surface!.TraceKind, extended.Surface!.TraceKind))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Surface traces use different evidence contracts.", "trace-kind-mismatch");

        var undeclaredSurfaces = addedSurfaces
            .Except(extended.Surface.DeclaredIndependentSurfaceIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var undeclaredOwners = addedOwners
            .Except(extended.Surface.DeclaredIndependentOwnerIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        if (undeclaredSurfaces.Length != 0 || undeclaredOwners.Length != 0)
        {
            return Result(
                context,
                PlanFuzzOracleStatus.InfrastructureFailure,
                "The additive extension is not fully declared independent in both surface and owner domains.",
                $"undeclared-surfaces={string.Join(',', undeclaredSurfaces)}:undeclared-owners={string.Join(',', undeclaredOwners)}");
        }

        var activatedAdded = addedOwners
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

    private static AdditiveDelta DescribeAdditiveDelta(PlanFuzzSurfaceSnapshot baseline, PlanFuzzSurfaceSnapshot extended)
    {
        var removedSurfaces = baseline.SelectedSurfaceIds.Except(extended.SelectedSurfaceIds, StringComparer.Ordinal).ToArray();
        var addedSurfaces = extended.SelectedSurfaceIds.Except(baseline.SelectedSurfaceIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal).ToArray();
        var removedOwners = baseline.SelectedOwnerIds.Except(extended.SelectedOwnerIds, StringComparer.Ordinal).ToArray();
        var addedOwners = extended.SelectedOwnerIds.Except(baseline.SelectedOwnerIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal).ToArray();
        return new AdditiveDelta(
            removedSurfaces.Length == 0 && removedOwners.Length == 0 && addedSurfaces.Length != 0 && addedOwners.Length != 0,
            removedSurfaces.Length == 0 && removedOwners.Length == 0 && addedSurfaces.Length == 0 && addedOwners.Length == 0,
            addedSurfaces,
            addedOwners);
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

    private sealed record AdditiveDelta(
        bool IsPureAdditive,
        bool IsEqual,
        string[] AddedSurfaces,
        string[] AddedOwners);
}
