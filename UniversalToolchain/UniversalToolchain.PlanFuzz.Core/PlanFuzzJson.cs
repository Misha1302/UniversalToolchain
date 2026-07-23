namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Exposes PlanFuzz canonical JSON operations to language adapters and artifact writers.
/// </summary>
public static class PlanFuzzJson
{
    public static string Canonicalize(string json) => PlanFuzzJsonCanonicalizer.Canonicalize(json.ArgNotNull());

    public static string ComputeSha256(string json) => PlanFuzzJsonCanonicalizer.ComputeSha256(json.ArgNotNull());
}
