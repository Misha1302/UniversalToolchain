namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Controls deterministic testcase generation without exposing adapter-specific option types to the core.
/// </summary>
public sealed class PlanFuzzCaseGenerationOptions
{
    public PlanFuzzCaseGenerationOptions(string? seededFaultId = null)
    {
        SeededFaultId = string.IsNullOrWhiteSpace(seededFaultId) ? null : seededFaultId;
    }

    public string? SeededFaultId { get; }
}
