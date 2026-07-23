namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Stores adapter-owned structured data as canonical JSON without exposing language-specific types to the core.
/// </summary>
public sealed class PlanFuzzPayload
{
    private PlanFuzzPayload(string canonicalJson)
    {
        CanonicalJson = canonicalJson;
    }

    public string CanonicalJson { get; }

    public static PlanFuzzPayload FromJson(string json)
    {
        json = json.ArgNotNull();
        return new PlanFuzzPayload(PlanFuzzJsonCanonicalizer.Canonicalize(json));
    }

    public JsonDocument Parse() => JsonDocument.Parse(CanonicalJson);
}
