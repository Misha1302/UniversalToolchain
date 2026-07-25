namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Captures language-neutral selected-surface and observed owner-activation evidence.
/// </summary>
public sealed class PlanFuzzSurfaceSnapshot
{
    public const int CurrentEvidenceContractVersion = 2;

    public PlanFuzzSurfaceSnapshot(
        int evidenceContractVersion,
        IEnumerable<string> selectedSurfaceIds,
        IEnumerable<string> selectedOwnerIds,
        IEnumerable<string> excludedOwnerIds,
        IEnumerable<string> declaredIndependentSurfaceIds,
        IEnumerable<string> declaredIndependentOwnerIds,
        IEnumerable<string> activatedOwnerIds,
        PlanFuzzActivationTraceStatus activationTraceStatus,
        string traceKind,
        string routeIdentity)
    {
        if (evidenceContractVersion != CurrentEvidenceContractVersion)
            Thrower.Argument(nameof(evidenceContractVersion), $"Surface evidence contract version must be exactly {CurrentEvidenceContractVersion}.");
        if (!Enum.IsDefined(activationTraceStatus))
            Thrower.Argument(nameof(activationTraceStatus), "Surface activation trace status is not recognized.");
        if (string.IsNullOrWhiteSpace(traceKind) || !StringComparer.Ordinal.Equals(traceKind, traceKind.Trim()))
            Thrower.Argument(nameof(traceKind), "Surface trace kind must be non-empty and canonical.");
        if (string.IsNullOrWhiteSpace(routeIdentity) || !StringComparer.Ordinal.Equals(routeIdentity, routeIdentity.Trim()))
            Thrower.Argument(nameof(routeIdentity), "Surface route identity must be non-empty and canonical.");

        EvidenceContractVersion = evidenceContractVersion;
        SelectedSurfaceIds = Snapshot(selectedSurfaceIds, nameof(selectedSurfaceIds));
        SelectedOwnerIds = Snapshot(selectedOwnerIds, nameof(selectedOwnerIds));
        ExcludedOwnerIds = Snapshot(excludedOwnerIds, nameof(excludedOwnerIds));
        DeclaredIndependentSurfaceIds = Snapshot(declaredIndependentSurfaceIds, nameof(declaredIndependentSurfaceIds));
        DeclaredIndependentOwnerIds = Snapshot(declaredIndependentOwnerIds, nameof(declaredIndependentOwnerIds));
        ActivatedOwnerIds = Snapshot(activatedOwnerIds, nameof(activatedOwnerIds));
        ActivationTraceStatus = activationTraceStatus;
        TraceKind = traceKind;
        RouteIdentity = routeIdentity;

        EnsureDisjoint(SelectedOwnerIds, ExcludedOwnerIds, nameof(excludedOwnerIds), "Selected and excluded owners must be disjoint.");
        EnsureSubset(DeclaredIndependentSurfaceIds, SelectedSurfaceIds, nameof(declaredIndependentSurfaceIds), "Independent surfaces must be selected.");
        EnsureSubset(DeclaredIndependentOwnerIds, SelectedOwnerIds, nameof(declaredIndependentOwnerIds), "Independent owners must be selected.");
        EnsureSubset(
            ActivatedOwnerIds,
            SelectedOwnerIds.Concat(ExcludedOwnerIds),
            nameof(activatedOwnerIds),
            "Activated owners must be declared as selected or explicitly excluded owners.");

        if (ActivationTraceStatus == PlanFuzzActivationTraceStatus.Complete && SelectedOwnerIds.Count == 0)
            Thrower.Argument(nameof(selectedOwnerIds), "A complete activation trace requires at least one selected owner.");
    }

    /// <summary>
    /// Legacy schema-v3 compatibility constructor. Legacy evidence remains readable but is not
    /// upgraded to the current owner-domain evidence contract or validated as current proof.
    /// </summary>
    [Obsolete("Use the schema-v4 constructor with explicit owner sets and trace status.")]
    public PlanFuzzSurfaceSnapshot(
        IEnumerable<string>? selectedSurfaceIds,
        IEnumerable<string>? excludedSurfaceIds,
        IEnumerable<string>? declaredIndependentSurfaceIds,
        IEnumerable<string>? activatedOwnerIds,
        bool activationTraceComplete,
        string traceKind,
        string routeIdentity)
    {
        if (string.IsNullOrWhiteSpace(traceKind))
            Thrower.Argument(nameof(traceKind), "Surface trace kind must not be empty.");
        if (string.IsNullOrWhiteSpace(routeIdentity))
            Thrower.Argument(nameof(routeIdentity), "Surface route identity must not be empty.");

        EvidenceContractVersion = 1;
        SelectedSurfaceIds = LegacySnapshot(selectedSurfaceIds);
        SelectedOwnerIds = [];
        ExcludedOwnerIds = LegacySnapshot(excludedSurfaceIds);
        DeclaredIndependentSurfaceIds = LegacySnapshot(declaredIndependentSurfaceIds);
        DeclaredIndependentOwnerIds = [];
        ActivatedOwnerIds = LegacySnapshot(activatedOwnerIds);
        ActivationTraceStatus = activationTraceComplete
            ? PlanFuzzActivationTraceStatus.Complete
            : PlanFuzzActivationTraceStatus.Partial;
        TraceKind = traceKind.Trim();
        RouteIdentity = routeIdentity.Trim();
    }

    public int EvidenceContractVersion { get; }
    public IReadOnlyList<string> SelectedSurfaceIds { get; }
    public IReadOnlyList<string> SelectedOwnerIds { get; }
    public IReadOnlyList<string> ExcludedOwnerIds { get; }
    public IReadOnlyList<string> DeclaredIndependentSurfaceIds { get; }
    public IReadOnlyList<string> DeclaredIndependentOwnerIds { get; }
    public IReadOnlyList<string> ActivatedOwnerIds { get; }
    public PlanFuzzActivationTraceStatus ActivationTraceStatus { get; }
    public string TraceKind { get; }
    public string RouteIdentity { get; }

    [Obsolete("Use ExcludedOwnerIds. Schema v3 used a mixed-domain name.")]
    public IReadOnlyList<string> ExcludedSurfaceIds => ExcludedOwnerIds;

    [Obsolete("Use ActivationTraceStatus.")]
    public bool ActivationTraceComplete => ActivationTraceStatus == PlanFuzzActivationTraceStatus.Complete;

    private static IReadOnlyList<string> Snapshot(IEnumerable<string> values, string parameterName)
    {
        if (values == null)
            return Thrower.Argument<IReadOnlyList<string>>(parameterName, $"Argument '{parameterName}' must not be null.");

        var snapshot = values.ToArray();
        if (snapshot.Any(static value => string.IsNullOrWhiteSpace(value)))
            return Thrower.Argument<IReadOnlyList<string>>(parameterName, $"Argument '{parameterName}' must not contain empty IDs.");
        if (snapshot.Any(static value => !StringComparer.Ordinal.Equals(value, value.Trim())))
            return Thrower.Argument<IReadOnlyList<string>>(parameterName, $"Argument '{parameterName}' must contain canonical IDs without surrounding whitespace.");
        if (snapshot.Distinct(StringComparer.Ordinal).Count() != snapshot.Length)
            return Thrower.Argument<IReadOnlyList<string>>(parameterName, $"Argument '{parameterName}' must not contain duplicate IDs.");

        Array.Sort(snapshot, StringComparer.Ordinal);
        return new ReadOnlyCollection<string>(snapshot);
    }

    private static IReadOnlyList<string> LegacySnapshot(IEnumerable<string>? values) =>
        new ReadOnlyCollection<string>((values ?? [])
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray());

    private static void EnsureSubset(
        IEnumerable<string> subset,
        IEnumerable<string> superset,
        string parameterName,
        string message)
    {
        var allowed = superset.ToHashSet(StringComparer.Ordinal);
        if (subset.Any(value => !allowed.Contains(value)))
            Thrower.Argument(parameterName, message);
    }

    private static void EnsureDisjoint(
        IEnumerable<string> left,
        IEnumerable<string> right,
        string parameterName,
        string message)
    {
        if (left.Intersect(right, StringComparer.Ordinal).Any())
            Thrower.Argument(parameterName, message);
    }
}
