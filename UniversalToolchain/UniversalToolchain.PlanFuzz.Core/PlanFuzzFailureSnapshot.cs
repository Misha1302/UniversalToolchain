namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Normalizes one program or infrastructure failure without using message text as the sole identity.
/// </summary>
public sealed record PlanFuzzFailureSnapshot(
    string FailureType,
    string Stage,
    string Category,
    string? Message = null);
