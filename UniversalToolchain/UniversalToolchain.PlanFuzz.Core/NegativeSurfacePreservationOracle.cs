namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Verifies that explicitly excluded owners are absent from complete observed activation traces.
/// </summary>
public sealed class NegativeSurfacePreservationOracle : IPlanFuzzOracle
{
    public string OracleId => PlanFuzzOracleIds.NegativeSurfacePreservation;
    public int OracleVersion => 2;

    public PlanFuzzOracleResult Evaluate(PlanFuzzOracleContext context)
    {
        context = context.ArgNotNull();
        if (context.Contract.VariantIds.Count == 0)
            return Result(context, PlanFuzzOracleStatus.NotApplicable, "Negative-surface preservation requires at least one variant.", "invalid-arity");

        var violations = new List<(string VariantId, string TraceKind, string[] Owners)>();
        var infrastructure = new List<string>();
        var inconclusive = new List<string>();
        var evaluated = 0;

        foreach (var variantId in context.Contract.VariantIds.OrderBy(static id => id, StringComparer.Ordinal))
        {
            if (!context.TryGetObservation(variantId, out var observation))
            {
                infrastructure.Add(PlanFuzzFingerprintEncoding.EncodeFields(variantId, "missing-observation"));
                continue;
            }
            if (observation.Outcome == PlanFuzzExecutionOutcome.InfrastructureFailure)
            {
                infrastructure.Add(PlanFuzzFingerprintEncoding.EncodeFields(variantId, "infrastructure"));
                continue;
            }
            if (observation.Outcome == PlanFuzzExecutionOutcome.Timeout)
            {
                inconclusive.Add(PlanFuzzFingerprintEncoding.EncodeFields(variantId, "timeout"));
                continue;
            }
            if (observation.Surface == null)
            {
                inconclusive.Add(PlanFuzzFingerprintEncoding.EncodeFields(variantId, "missing-surface"));
                continue;
            }
            if (observation.Surface.EvidenceContractVersion < PlanFuzzSurfaceSnapshot.MinimumNegativeSurfaceEvidenceContractVersion ||
                observation.Surface.EvidenceContractVersion > PlanFuzzSurfaceSnapshot.CurrentEvidenceContractVersion)
            {
                inconclusive.Add(PlanFuzzFingerprintEncoding.EncodeFields(
                    variantId,
                    "legacy-evidence",
                    observation.Surface.EvidenceContractVersion.ToString(CultureInfo.InvariantCulture)));
                continue;
            }
            if (observation.Surface.ActivationTraceStatus != PlanFuzzActivationTraceStatus.Complete)
            {
                inconclusive.Add(PlanFuzzFingerprintEncoding.EncodeFields(
                    variantId,
                    observation.Surface.ActivationTraceStatus.ToString(),
                    observation.Surface.TraceKind));
                continue;
            }
            if (observation.Surface.ExcludedOwnerIds.Count == 0)
                continue;

            evaluated++;
            var activatedExcluded = observation.Surface.ExcludedOwnerIds
                .Intersect(observation.Surface.ActivatedOwnerIds, StringComparer.Ordinal)
                .OrderBy(static value => value, StringComparer.Ordinal)
                .ToArray();
            if (activatedExcluded.Length != 0)
                violations.Add((variantId, observation.Surface.TraceKind, activatedExcluded));
        }

        if (violations.Count != 0)
        {
            var material = PlanFuzzFingerprintEncoding.EncodeSequence(violations.Select(static violation =>
                PlanFuzzFingerprintEncoding.EncodeFields(
                    violation.VariantId,
                    violation.TraceKind,
                    PlanFuzzFingerprintEncoding.EncodeSequence(violation.Owners))));
            var owners = violations.SelectMany(static violation => violation.Owners)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static owner => owner, StringComparer.Ordinal)
                .ToArray();
            return Result(
                context,
                PlanFuzzOracleStatus.Violated,
                "One or more variants activated owner(s) explicitly excluded from their selected surface.",
                material,
                "excluded-owner-activated:" + PlanFuzzFingerprintEncoding.EncodeSequence(owners));
        }

        if (infrastructure.Count != 0)
        {
            var material = PlanFuzzFingerprintEncoding.EncodeSequence(
                infrastructure.OrderBy(static item => item, StringComparer.Ordinal));
            return Result(context, PlanFuzzOracleStatus.InfrastructureFailure, "Infrastructure evidence is missing for one or more negative-surface variants.", material);
        }

        if (inconclusive.Count != 0)
        {
            var material = PlanFuzzFingerprintEncoding.EncodeSequence(
                inconclusive.OrderBy(static item => item, StringComparer.Ordinal));
            return Result(context, PlanFuzzOracleStatus.Inconclusive, "One or more variants lack complete owner-activation evidence.", material);
        }

        return evaluated == 0
            ? Result(context, PlanFuzzOracleStatus.NotApplicable, "No selected variant declares excluded owners.", "no-excluded-owners")
            : Result(context, PlanFuzzOracleStatus.Passed, "Excluded owners are absent from all complete observed activation traces.", "equal");
    }

    private PlanFuzzOracleResult Result(
        PlanFuzzOracleContext context,
        PlanFuzzOracleStatus status,
        string summary,
        string fingerprintMaterial,
        string? classFingerprintMaterial = null) =>
        new(context.Contract.ContractId, OracleId, OracleVersion, status, summary, fingerprintMaterial, classFingerprintMaterial);
}
