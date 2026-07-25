namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Verifies that one explicitly declared independent, unused extension changes neither semantics nor selected execution route.
/// </summary>
public sealed class ExtensionNoninterferenceOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.ExtensionNoninterference;
    public int OracleVersion => 3;

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
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Extension noninterference requires schema-v5 extension-binding evidence.", "legacy-evidence");
        if (pair.Any(static observation => observation.Surface!.ActivationTraceStatus != PlanFuzzActivationTraceStatus.Complete))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Extension noninterference requires complete activation traces.", "incomplete-trace");

        var first = observations[0];
        var second = observations[1];
        var firstToSecond = DescribeExtensionDelta(first.Surface!, second.Surface!);
        var secondToFirst = DescribeExtensionDelta(second.Surface!, first.Surface!);
        var directions = new[]
        {
            (Baseline: first, Extended: second, Delta: firstToSecond),
            (Baseline: second, Extended: first, Delta: secondToFirst)
        }.Where(static direction => direction.Delta.IsStrictSingleExtension).ToArray();

        if (directions.Length == 0)
        {
            if (firstToSecond.IsEqual && secondToFirst.IsEqual)
                return Result(context, PlanFuzzOracleStatus.NotApplicable, "The compared variants contain no extension delta.", "no-delta");
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "The O-005 contract does not describe one strict single-extension pair.", "invalid-or-ambiguous-delta");
        }
        if (directions.Length != 1)
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "The O-005 contract has an ambiguous extension direction.", "ambiguous-delta");

        var direction = directions[0];
        var baseline = direction.Baseline;
        var extended = direction.Extended;
        var addedOwners = direction.Delta.AddedOwners;

        if (!StringComparer.Ordinal.Equals(baseline.Surface!.TraceKind, extended.Surface!.TraceKind))
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "Surface traces use different evidence contracts.", "trace-kind-mismatch");

        var exactDimensions = new List<string>();
        var classDimensions = new List<string>();
        var summaries = new List<string>();

        var activatedAdded = addedOwners
            .Intersect(extended.Surface.ActivatedOwnerIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        if (activatedAdded.Length != 0)
        {
            var material = string.Join(',', activatedAdded);
            exactDimensions.Add($"extension-activated:{material}");
            classDimensions.Add("extension-activated");
            summaries.Add("the declared unused extension became active");
        }

        if (!StringComparer.Ordinal.Equals(baseline.Surface.RouteIdentity, extended.Surface.RouteIdentity))
        {
            exactDimensions.Add($"route:{baseline.Surface.RouteIdentity}|{extended.Surface.RouteIdentity}");
            classDimensions.Add("route-changed");
            summaries.Add("the selected execution route changed");
        }

        if (!baseline.Surface.ActivatedOwnerIds.SequenceEqual(extended.Surface.ActivatedOwnerIds, StringComparer.Ordinal))
        {
            exactDimensions.Add($"activation:{string.Join(',', baseline.Surface.ActivatedOwnerIds)}|{string.Join(',', extended.Surface.ActivatedOwnerIds)}");
            classDimensions.Add("activation-changed");
            summaries.Add("the activated-owner set changed");
        }

        if (!PlanFuzzObservationComparer.AreSemanticallyEquivalent(baseline, extended))
        {
            exactDimensions.Add($"behavior:{Describe(baseline)}|{Describe(extended)}");
            classDimensions.Add($"behavior:{DescribeClass(baseline)}|{DescribeClass(extended)}");
            summaries.Add("observable semantics changed");
        }

        if (exactDimensions.Count != 0)
        {
            return Result(
                context,
                PlanFuzzOracleStatus.Violated,
                "Extension noninterference failed: " + string.Join("; ", summaries) + ".",
                string.Join('|', exactDimensions),
                string.Join('|', classDimensions));
        }

        return Result(context, PlanFuzzOracleStatus.Passed, "The declared unused extension preserves semantics, route and activation behavior.", "equal");
    }

    private static ExtensionDelta DescribeExtensionDelta(
        PlanFuzzSurfaceSnapshot baseline,
        PlanFuzzSurfaceSnapshot extended)
    {
        var removedSurfaces = baseline.SelectedSurfaceIds.Except(extended.SelectedSurfaceIds, StringComparer.Ordinal).ToArray();
        var addedSurfaces = extended.SelectedSurfaceIds.Except(baseline.SelectedSurfaceIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal).ToArray();
        var removedOwners = baseline.SelectedOwnerIds.Except(extended.SelectedOwnerIds, StringComparer.Ordinal).ToArray();
        var addedOwners = extended.SelectedOwnerIds.Except(baseline.SelectedOwnerIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal).ToArray();

        var removedIndependentSurfaces = baseline.DeclaredIndependentSurfaceIds
            .Except(extended.DeclaredIndependentSurfaceIds, StringComparer.Ordinal).ToArray();
        var addedIndependentSurfaces = extended.DeclaredIndependentSurfaceIds
            .Except(baseline.DeclaredIndependentSurfaceIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal).ToArray();
        var removedIndependentOwners = baseline.DeclaredIndependentOwnerIds
            .Except(extended.DeclaredIndependentOwnerIds, StringComparer.Ordinal).ToArray();
        var addedIndependentOwners = extended.DeclaredIndependentOwnerIds
            .Except(baseline.DeclaredIndependentOwnerIds, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal).ToArray();

        var baselineBindings = baseline.IndependentExtensions
            .Select(static extension => extension.CanonicalIdentity)
            .ToHashSet(StringComparer.Ordinal);
        var extendedBindings = extended.IndependentExtensions
            .Select(static extension => extension.CanonicalIdentity)
            .ToHashSet(StringComparer.Ordinal);
        var removedBindings = baselineBindings.Except(extendedBindings, StringComparer.Ordinal).ToArray();
        var addedBindings = extended.IndependentExtensions
            .Where(extension => !baselineBindings.Contains(extension.CanonicalIdentity))
            .ToArray();

        var expectedExcludedOwners = baseline.ExcludedOwnerIds
            .Except(addedOwners, StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        var selectedDeltaIsAdditive =
            removedSurfaces.Length == 0 &&
            removedOwners.Length == 0 &&
            addedSurfaces.Length != 0 &&
            addedOwners.Length != 0;
        var declarationsMatchSelectedDelta =
            removedIndependentSurfaces.Length == 0 &&
            removedIndependentOwners.Length == 0 &&
            addedIndependentSurfaces.SequenceEqual(addedSurfaces, StringComparer.Ordinal) &&
            addedIndependentOwners.SequenceEqual(addedOwners, StringComparer.Ordinal);
        var exclusionPolicyIsPreserved =
            extended.ExcludedOwnerIds.SequenceEqual(expectedExcludedOwners, StringComparer.Ordinal);
        var bindingIsExact =
            removedBindings.Length == 0 &&
            addedBindings.Length == 1 &&
            addedBindings[0].SurfaceIds.SequenceEqual(addedSurfaces, StringComparer.Ordinal) &&
            addedBindings[0].OwnerIds.SequenceEqual(addedOwners, StringComparer.Ordinal);

        return new ExtensionDelta(
            selectedDeltaIsAdditive && declarationsMatchSelectedDelta && exclusionPolicyIsPreserved && bindingIsExact,
            removedSurfaces.Length == 0 &&
            removedOwners.Length == 0 &&
            addedSurfaces.Length == 0 &&
            addedOwners.Length == 0 &&
            baseline.DeclaredIndependentSurfaceIds.SequenceEqual(extended.DeclaredIndependentSurfaceIds, StringComparer.Ordinal) &&
            baseline.DeclaredIndependentOwnerIds.SequenceEqual(extended.DeclaredIndependentOwnerIds, StringComparer.Ordinal) &&
            baseline.ExcludedOwnerIds.SequenceEqual(extended.ExcludedOwnerIds, StringComparer.Ordinal) &&
            baselineBindings.SetEquals(extendedBindings),
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

    private sealed record ExtensionDelta(
        bool IsStrictSingleExtension,
        bool IsEqual,
        string[] AddedSurfaces,
        string[] AddedOwners);
}
