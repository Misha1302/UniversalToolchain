namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Captures language-neutral selected-surface and observed owner-activation evidence.
/// </summary>
public sealed class PlanFuzzSurfaceSnapshot
{
    public const int CurrentEvidenceContractVersion = 3;
    public const int MinimumNegativeSurfaceEvidenceContractVersion = 2;

    public PlanFuzzSurfaceSnapshot(
        int evidenceContractVersion,
        IEnumerable<string> selectedSurfaceIds,
        IEnumerable<string> selectedOwnerIds,
        IEnumerable<string> excludedOwnerIds,
        IEnumerable<string> declaredIndependentSurfaceIds,
        IEnumerable<string> declaredIndependentOwnerIds,
        IEnumerable<PlanFuzzIndependentExtensionEvidence> independentExtensions,
        IEnumerable<string> activatedOwnerIds,
        PlanFuzzActivationTraceStatus activationTraceStatus,
        string traceKind,
        string routeIdentity)
        : this(CreateCurrent(
            evidenceContractVersion,
            selectedSurfaceIds,
            selectedOwnerIds,
            excludedOwnerIds,
            declaredIndependentSurfaceIds,
            declaredIndependentOwnerIds,
            independentExtensions,
            activatedOwnerIds,
            activationTraceStatus,
            traceKind,
            routeIdentity))
    {
    }

    /// <summary>
    /// Schema-v4/evidence-v2 compatibility constructor. Evidence-v2 remains readable and is
    /// sufficient for O-004, but it does not contain explicit extension bindings required by current O-005.
    /// </summary>
    [Obsolete("Use the evidence-v3 constructor with explicit independent extension bindings.")]
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
        : this(CreateWithoutBindings(
            evidenceContractVersion,
            selectedSurfaceIds,
            selectedOwnerIds,
            excludedOwnerIds,
            declaredIndependentSurfaceIds,
            declaredIndependentOwnerIds,
            activatedOwnerIds,
            activationTraceStatus,
            traceKind,
            routeIdentity))
    {
    }

    /// <summary>
    /// Legacy schema-v3 compatibility constructor. Legacy evidence remains readable but is not
    /// upgraded to the current owner-domain evidence contract or validated as current proof.
    /// </summary>
    [Obsolete("Use the schema-v5 constructor with explicit owner sets, extension bindings and trace status.")]
    public PlanFuzzSurfaceSnapshot(
        IEnumerable<string>? selectedSurfaceIds,
        IEnumerable<string>? excludedSurfaceIds,
        IEnumerable<string>? declaredIndependentSurfaceIds,
        IEnumerable<string>? activatedOwnerIds,
        bool activationTraceComplete,
        string traceKind,
        string routeIdentity)
        : this(CreateLegacy(
            selectedSurfaceIds,
            excludedSurfaceIds,
            declaredIndependentSurfaceIds,
            activatedOwnerIds,
            activationTraceComplete,
            traceKind,
            routeIdentity))
    {
    }

    private PlanFuzzSurfaceSnapshot(SnapshotState state)
    {
        EvidenceContractVersion = state.EvidenceContractVersion;
        SelectedSurfaceIds = state.SelectedSurfaceIds;
        SelectedOwnerIds = state.SelectedOwnerIds;
        ExcludedOwnerIds = state.ExcludedOwnerIds;
        DeclaredIndependentSurfaceIds = state.DeclaredIndependentSurfaceIds;
        DeclaredIndependentOwnerIds = state.DeclaredIndependentOwnerIds;
        IndependentExtensions = state.IndependentExtensions;
        ActivatedOwnerIds = state.ActivatedOwnerIds;
        ActivationTraceStatus = state.ActivationTraceStatus;
        TraceKind = state.TraceKind;
        RouteIdentity = state.RouteIdentity;
    }

    public int EvidenceContractVersion { get; }
    public IReadOnlyList<string> SelectedSurfaceIds { get; }
    public IReadOnlyList<string> SelectedOwnerIds { get; }
    public IReadOnlyList<string> ExcludedOwnerIds { get; }
    public IReadOnlyList<string> DeclaredIndependentSurfaceIds { get; }
    public IReadOnlyList<string> DeclaredIndependentOwnerIds { get; }
    public IReadOnlyList<PlanFuzzIndependentExtensionEvidence> IndependentExtensions { get; }
    public IReadOnlyList<string> ActivatedOwnerIds { get; }
    public PlanFuzzActivationTraceStatus ActivationTraceStatus { get; }
    public string TraceKind { get; }
    public string RouteIdentity { get; }

    [Obsolete("Use ExcludedOwnerIds. Schema v3 used a mixed-domain name.")]
    public IReadOnlyList<string> ExcludedSurfaceIds => ExcludedOwnerIds;

    [Obsolete("Use ActivationTraceStatus.")]
    public bool ActivationTraceComplete => ActivationTraceStatus == PlanFuzzActivationTraceStatus.Complete;

    private static SnapshotState CreateCurrent(
        int evidenceContractVersion,
        IEnumerable<string> selectedSurfaceIds,
        IEnumerable<string> selectedOwnerIds,
        IEnumerable<string> excludedOwnerIds,
        IEnumerable<string> declaredIndependentSurfaceIds,
        IEnumerable<string> declaredIndependentOwnerIds,
        IEnumerable<PlanFuzzIndependentExtensionEvidence> independentExtensions,
        IEnumerable<string> activatedOwnerIds,
        PlanFuzzActivationTraceStatus activationTraceStatus,
        string traceKind,
        string routeIdentity)
    {
        if (evidenceContractVersion != CurrentEvidenceContractVersion)
            Thrower.Argument(nameof(evidenceContractVersion), $"Surface evidence contract version must be exactly {CurrentEvidenceContractVersion}.");

        var state = CreateValidated(
            evidenceContractVersion,
            selectedSurfaceIds,
            selectedOwnerIds,
            excludedOwnerIds,
            declaredIndependentSurfaceIds,
            declaredIndependentOwnerIds,
            independentExtensions,
            activatedOwnerIds,
            activationTraceStatus,
            traceKind,
            routeIdentity);
        EnsureExtensionCoverage(state);
        return state;
    }

    private static SnapshotState CreateWithoutBindings(
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
        if (evidenceContractVersion is not 2 and not CurrentEvidenceContractVersion)
            Thrower.Argument(nameof(evidenceContractVersion), "Binding-free surface evidence must use contract version 2 or an empty version-3 independent-extension surface.");

        var state = CreateValidated(
            evidenceContractVersion,
            selectedSurfaceIds,
            selectedOwnerIds,
            excludedOwnerIds,
            declaredIndependentSurfaceIds,
            declaredIndependentOwnerIds,
            [],
            activatedOwnerIds,
            activationTraceStatus,
            traceKind,
            routeIdentity);
        if (evidenceContractVersion == CurrentEvidenceContractVersion &&
            (state.DeclaredIndependentSurfaceIds.Count != 0 || state.DeclaredIndependentOwnerIds.Count != 0))
        {
            Thrower.Argument(nameof(declaredIndependentSurfaceIds), "Evidence contract version 3 requires explicit bindings for every independent surface and owner.");
        }
        return state;
    }

    private static SnapshotState CreateValidated(
        int evidenceContractVersion,
        IEnumerable<string> selectedSurfaceIds,
        IEnumerable<string> selectedOwnerIds,
        IEnumerable<string> excludedOwnerIds,
        IEnumerable<string> declaredIndependentSurfaceIds,
        IEnumerable<string> declaredIndependentOwnerIds,
        IEnumerable<PlanFuzzIndependentExtensionEvidence> independentExtensions,
        IEnumerable<string> activatedOwnerIds,
        PlanFuzzActivationTraceStatus activationTraceStatus,
        string traceKind,
        string routeIdentity)
    {
        if (!Enum.IsDefined(activationTraceStatus))
            Thrower.Argument(nameof(activationTraceStatus), "Surface activation trace status is not recognized.");
        if (string.IsNullOrWhiteSpace(traceKind) || !StringComparer.Ordinal.Equals(traceKind, traceKind.Trim()))
            Thrower.Argument(nameof(traceKind), "Surface trace kind must be non-empty and canonical.");
        if (string.IsNullOrWhiteSpace(routeIdentity) || !StringComparer.Ordinal.Equals(routeIdentity, routeIdentity.Trim()))
            Thrower.Argument(nameof(routeIdentity), "Surface route identity must be non-empty and canonical.");

        var selectedSurfaces = Snapshot(selectedSurfaceIds, nameof(selectedSurfaceIds));
        var selectedOwners = Snapshot(selectedOwnerIds, nameof(selectedOwnerIds));
        var excludedOwners = Snapshot(excludedOwnerIds, nameof(excludedOwnerIds));
        var independentSurfaces = Snapshot(declaredIndependentSurfaceIds, nameof(declaredIndependentSurfaceIds));
        var independentOwners = Snapshot(declaredIndependentOwnerIds, nameof(declaredIndependentOwnerIds));
        var extensions = SnapshotExtensions(independentExtensions);
        var activatedOwners = Snapshot(activatedOwnerIds, nameof(activatedOwnerIds));

        EnsureDisjoint(selectedOwners, excludedOwners, nameof(excludedOwnerIds), "Selected and excluded owners must be disjoint.");
        EnsureSubset(independentSurfaces, selectedSurfaces, nameof(declaredIndependentSurfaceIds), "Independent surfaces must be selected.");
        EnsureSubset(independentOwners, selectedOwners, nameof(declaredIndependentOwnerIds), "Independent owners must be selected.");
        EnsureSubset(
            activatedOwners,
            selectedOwners.Concat(excludedOwners),
            nameof(activatedOwnerIds),
            "Activated owners must be declared as selected or explicitly excluded owners.");

        if (activationTraceStatus == PlanFuzzActivationTraceStatus.Complete && selectedOwners.Count == 0)
            Thrower.Argument(nameof(selectedOwnerIds), "A complete activation trace requires at least one selected owner.");

        return new SnapshotState(
            evidenceContractVersion,
            selectedSurfaces,
            selectedOwners,
            excludedOwners,
            independentSurfaces,
            independentOwners,
            extensions,
            activatedOwners,
            activationTraceStatus,
            traceKind,
            routeIdentity);
    }

    private static SnapshotState CreateLegacy(
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

        return new SnapshotState(
            1,
            LegacySnapshot(selectedSurfaceIds),
            [],
            LegacySnapshot(excludedSurfaceIds),
            LegacySnapshot(declaredIndependentSurfaceIds),
            [],
            [],
            LegacySnapshot(activatedOwnerIds),
            activationTraceComplete ? PlanFuzzActivationTraceStatus.Complete : PlanFuzzActivationTraceStatus.Partial,
            traceKind.Trim(),
            routeIdentity.Trim());
    }

    private static void EnsureExtensionCoverage(SnapshotState state)
    {
        var surfaces = state.IndependentExtensions.SelectMany(static extension => extension.SurfaceIds).ToArray();
        var owners = state.IndependentExtensions.SelectMany(static extension => extension.OwnerIds).ToArray();
        if (surfaces.Distinct(StringComparer.Ordinal).Count() != surfaces.Length)
            Thrower.Argument(nameof(IndependentExtensions), "An independent surface must belong to exactly one extension binding.");
        if (owners.Distinct(StringComparer.Ordinal).Count() != owners.Length)
            Thrower.Argument(nameof(IndependentExtensions), "An independent owner must belong to exactly one extension binding.");
        if (!surfaces.Order(StringComparer.Ordinal).SequenceEqual(state.DeclaredIndependentSurfaceIds, StringComparer.Ordinal))
            Thrower.Argument(nameof(IndependentExtensions), "Independent extension bindings must cover exactly the declared independent surface IDs.");
        if (!owners.Order(StringComparer.Ordinal).SequenceEqual(state.DeclaredIndependentOwnerIds, StringComparer.Ordinal))
            Thrower.Argument(nameof(IndependentExtensions), "Independent extension bindings must cover exactly the declared independent owner IDs.");
    }

    private static IReadOnlyList<PlanFuzzIndependentExtensionEvidence> SnapshotExtensions(
        IEnumerable<PlanFuzzIndependentExtensionEvidence> values)
    {
        if (values == null)
            return Thrower.Argument<IReadOnlyList<PlanFuzzIndependentExtensionEvidence>>(nameof(values), "Independent extension bindings must not be null.");
        var snapshot = values.ToArray();
        if (snapshot.Any(static value => value == null))
            return Thrower.Argument<IReadOnlyList<PlanFuzzIndependentExtensionEvidence>>(nameof(values), "Independent extension bindings must not contain null entries.");
        if (snapshot.Select(static value => value.ExtensionId).Distinct(StringComparer.Ordinal).Count() != snapshot.Length)
            return Thrower.Argument<IReadOnlyList<PlanFuzzIndependentExtensionEvidence>>(nameof(values), "Independent extension IDs must be unique.");
        return new ReadOnlyCollection<PlanFuzzIndependentExtensionEvidence>(snapshot
            .OrderBy(static value => value.ExtensionId, StringComparer.Ordinal)
            .ToArray());
    }

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

    private sealed record SnapshotState(
        int EvidenceContractVersion,
        IReadOnlyList<string> SelectedSurfaceIds,
        IReadOnlyList<string> SelectedOwnerIds,
        IReadOnlyList<string> ExcludedOwnerIds,
        IReadOnlyList<string> DeclaredIndependentSurfaceIds,
        IReadOnlyList<string> DeclaredIndependentOwnerIds,
        IReadOnlyList<PlanFuzzIndependentExtensionEvidence> IndependentExtensions,
        IReadOnlyList<string> ActivatedOwnerIds,
        PlanFuzzActivationTraceStatus ActivationTraceStatus,
        string TraceKind,
        string RouteIdentity);
}
