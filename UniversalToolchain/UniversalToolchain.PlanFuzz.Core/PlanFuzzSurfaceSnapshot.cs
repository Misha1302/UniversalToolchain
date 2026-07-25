namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Captures language-neutral selected-surface and activation evidence for absence and extension oracles.
/// </summary>
public sealed class PlanFuzzSurfaceSnapshot
{
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

        SelectedSurfaceIds = Snapshot(selectedSurfaceIds);
        ExcludedSurfaceIds = Snapshot(excludedSurfaceIds);
        DeclaredIndependentSurfaceIds = Snapshot(declaredIndependentSurfaceIds);
        ActivatedOwnerIds = Snapshot(activatedOwnerIds);
        ActivationTraceComplete = activationTraceComplete;
        TraceKind = traceKind.Trim();
        RouteIdentity = routeIdentity.Trim();
    }

    public IReadOnlyList<string> SelectedSurfaceIds { get; }
    public IReadOnlyList<string> ExcludedSurfaceIds { get; }
    public IReadOnlyList<string> DeclaredIndependentSurfaceIds { get; }
    public IReadOnlyList<string> ActivatedOwnerIds { get; }
    public bool ActivationTraceComplete { get; }
    public string TraceKind { get; }
    public string RouteIdentity { get; }

    private static IReadOnlyList<string> Snapshot(IEnumerable<string>? values) =>
        new ReadOnlyCollection<string>((values ?? [])
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray());
}
