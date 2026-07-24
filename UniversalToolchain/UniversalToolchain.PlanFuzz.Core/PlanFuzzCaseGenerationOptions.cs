namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Controls deterministic testcase generation without exposing adapter-specific option types to the core.
/// </summary>
public sealed class PlanFuzzCaseGenerationOptions
{
    public PlanFuzzCaseGenerationOptions(
        string? seededFaultId = null,
        bool includeRegressionCorpus = false)
    {
        SeededFaultId = string.IsNullOrWhiteSpace(seededFaultId) ? null : seededFaultId;
        IncludeRegressionCorpus = includeRegressionCorpus;
    }

    public string? SeededFaultId { get; }

    /// <summary>
    /// Includes adapter-owned known regression fixtures before generated cases.
    /// This is disabled by default so discovery campaigns do not count known cases as rediscoveries.
    /// </summary>
    public bool IncludeRegressionCorpus { get; }
}
