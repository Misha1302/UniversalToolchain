namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Owns versioned protocol identifiers used by the PlanFuzz core.
/// </summary>
public static class PlanFuzzConstants
{
    public const int CaseSchemaVersion = 1;
    public const int ObservationSchemaVersion = 5;
    public const int ReplayReportSchemaVersion = 3;
    public const string Canonicalization = "planfuzz-json-v1";
}
