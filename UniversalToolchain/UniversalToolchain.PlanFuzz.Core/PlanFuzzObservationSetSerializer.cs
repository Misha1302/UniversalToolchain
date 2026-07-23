namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Serializes the complete fresh-process observation set for one testcase attempt.
/// </summary>
public static class PlanFuzzObservationSetSerializer
{
    public const int SchemaVersion = 1;

    public static string Serialize(
        string caseId,
        IEnumerable<PlanFuzzObservation> observations,
        bool indented = true)
    {
        if (string.IsNullOrWhiteSpace(caseId))
            Thrower.Argument(nameof(caseId), "Case ID must not be empty.");
        var snapshot = observations.ArgNotNull()
            .OrderBy(static observation => observation.VariantId, StringComparer.Ordinal)
            .ToArray();
        Validate(caseId, snapshot);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteString("canonicalization", PlanFuzzConstants.Canonicalization);
            writer.WriteString("caseId", caseId);
            writer.WritePropertyName("observations");
            writer.WriteStartArray();
            foreach (var observation in snapshot)
            {
                using var document = JsonDocument.Parse(PlanFuzzObservationSerializer.Serialize(observation, indented: false));
                document.RootElement.WriteTo(writer);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    public static IReadOnlyList<PlanFuzzObservation> Deserialize(string json)
    {
        json = json.ArgNotNull();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var schemaVersion = root.GetProperty("schemaVersion").GetInt32();
        if (schemaVersion != SchemaVersion)
            return Thrower.NotSupported<IReadOnlyList<PlanFuzzObservation>>($"Unsupported observation-set schema version '{schemaVersion}'.");
        var canonicalization = root.GetProperty("canonicalization").GetString();
        if (!StringComparer.Ordinal.Equals(canonicalization, PlanFuzzConstants.Canonicalization))
            return Thrower.NotSupported<IReadOnlyList<PlanFuzzObservation>>($"Unsupported observation-set canonicalization '{canonicalization}'.");
        var caseId = root.GetProperty("caseId").GetString().NotNull("Observation-set case ID is missing.");
        var observations = root.GetProperty("observations").EnumerateArray()
            .Select(static element => PlanFuzzObservationSerializer.Deserialize(element.GetRawText()))
            .OrderBy(static observation => observation.VariantId, StringComparer.Ordinal)
            .ToArray();
        Validate(caseId, observations);
        return new ReadOnlyCollection<PlanFuzzObservation>(observations);
    }

    private static void Validate(string caseId, IReadOnlyCollection<PlanFuzzObservation> observations)
    {
        if (observations.Count == 0)
            Thrower.Argument(nameof(observations), "Observation set must not be empty.");
        if (observations.Any(observation => !StringComparer.Ordinal.Equals(observation.CaseId, caseId)))
            Thrower.Argument(nameof(observations), "Every observation must match the observation-set case ID.");
        if (observations.Select(static observation => observation.VariantId).Distinct(StringComparer.Ordinal).Count() != observations.Count)
            Thrower.Argument(nameof(observations), "Observation variant IDs must be unique.");
    }
}
