namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Captures language-neutral evidence about an optional optimization or lowering route.
/// </summary>
public sealed class PlanFuzzRouteSnapshot
{
    public PlanFuzzRouteSnapshot(
        string routeId,
        string requestedPolicy,
        bool usedRoute,
        bool fellBack,
        PlanFuzzFallbackKind fallbackKind,
        string? profile = null,
        int inputInstructionCount = 0,
        int outputInstructionCount = 0,
        IEnumerable<string>? executedPasses = null,
        IEnumerable<PlanFuzzRouteDiagnosticSnapshot>? diagnostics = null)
    {
        if (string.IsNullOrWhiteSpace(routeId))
            Thrower.Argument(nameof(routeId), "Route ID must not be empty.");
        if (string.IsNullOrWhiteSpace(requestedPolicy))
            Thrower.Argument(nameof(requestedPolicy), "Requested route policy must not be empty.");
        if (inputInstructionCount < 0)
            Thrower.Argument(nameof(inputInstructionCount), "Input instruction count must not be negative.");
        if (outputInstructionCount < 0)
            Thrower.Argument(nameof(outputInstructionCount), "Output instruction count must not be negative.");
        if (usedRoute && fellBack)
            Thrower.Argument(nameof(fellBack), "A route cannot both complete and fall back.");
        if (!fellBack && fallbackKind != PlanFuzzFallbackKind.None)
            Thrower.Argument(nameof(fallbackKind), "A non-fallback route must use fallback kind None.");
        if (fellBack && fallbackKind == PlanFuzzFallbackKind.None)
            Thrower.Argument(nameof(fallbackKind), "A fallback route must identify its fallback classification.");

        RouteId = routeId;
        RequestedPolicy = requestedPolicy;
        UsedRoute = usedRoute;
        FellBack = fellBack;
        FallbackKind = fallbackKind;
        Profile = string.IsNullOrWhiteSpace(profile) ? null : profile.Trim();
        InputInstructionCount = inputInstructionCount;
        OutputInstructionCount = outputInstructionCount;
        ExecutedPasses = new ReadOnlyCollection<string>((executedPasses ?? [])
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray());
        Diagnostics = new ReadOnlyCollection<PlanFuzzRouteDiagnosticSnapshot>((diagnostics ?? []).ToArray());
    }

    public string RouteId { get; }
    public string RequestedPolicy { get; }
    public bool UsedRoute { get; }
    public bool FellBack { get; }
    public PlanFuzzFallbackKind FallbackKind { get; }
    public string? Profile { get; }
    public int InputInstructionCount { get; }
    public int OutputInstructionCount { get; }
    public IReadOnlyList<string> ExecutedPasses { get; }
    public IReadOnlyList<PlanFuzzRouteDiagnosticSnapshot> Diagnostics { get; }
}
