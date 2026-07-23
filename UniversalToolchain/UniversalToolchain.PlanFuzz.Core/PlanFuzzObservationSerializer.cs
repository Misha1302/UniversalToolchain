namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Serializes the versioned worker observation protocol.
/// </summary>
public static class PlanFuzzObservationSerializer
{
    public static string Serialize(PlanFuzzObservation observation, bool indented = true)
    {
        observation = observation.ArgNotNull();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", PlanFuzzConstants.ObservationSchemaVersion);
            writer.WriteString("canonicalization", PlanFuzzConstants.Canonicalization);
            writer.WriteString("caseId", observation.CaseId);
            writer.WriteString("variantId", observation.VariantId);
            writer.WriteString("backendId", observation.BackendId);
            writer.WriteString("outcome", observation.Outcome.ToString());
            if (observation.Value != null)
            {
                writer.WritePropertyName("value");
                writer.WriteStartObject();
                writer.WriteString("typeIdentity", observation.Value.TypeIdentity);
                writer.WriteString("canonicalValue", observation.Value.CanonicalValue);
                writer.WriteEndObject();
            }
            if (observation.Failure != null)
            {
                writer.WritePropertyName("failure");
                writer.WriteStartObject();
                writer.WriteString("failureType", observation.Failure.FailureType);
                writer.WriteString("stage", observation.Failure.Stage);
                writer.WriteString("category", observation.Failure.Category);
                if (observation.Failure.Message != null)
                    writer.WriteString("message", observation.Failure.Message);
                writer.WriteEndObject();
            }
            if (observation.Plan != null)
            {
                writer.WritePropertyName("plan");
                writer.WriteStartObject();
                writer.WriteString("planHash", observation.Plan.PlanHash);
                writer.WriteString("canonicalLockSha256", observation.Plan.CanonicalLockSha256);
                writer.WriteString("repeatedCanonicalLockSha256", observation.Plan.RepeatedCanonicalLockSha256);
                writer.WriteString("canonicalLockSemanticSha256", observation.Plan.CanonicalLockSemanticSha256);
                writer.WriteString("prettyLockSemanticSha256", observation.Plan.PrettyLockSemanticSha256);
                writer.WriteNumber("lockSchemaVersion", observation.Plan.LockSchemaVersion);
                writer.WriteString("lockCanonicalization", observation.Plan.LockCanonicalization);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    public static PlanFuzzObservation Deserialize(string json)
    {
        json = json.ArgNotNull();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var schemaVersion = root.GetProperty("schemaVersion").GetInt32();
        if (schemaVersion != PlanFuzzConstants.ObservationSchemaVersion)
            return Thrower.NotSupported<PlanFuzzObservation>($"Unsupported observation schema version '{schemaVersion}'.");
        var canonicalization = root.GetProperty("canonicalization").GetString();
        if (!StringComparer.Ordinal.Equals(canonicalization, PlanFuzzConstants.Canonicalization))
            return Thrower.NotSupported<PlanFuzzObservation>($"Unsupported observation canonicalization '{canonicalization}'.");

        PlanFuzzValueSnapshot? value = null;
        if (root.TryGetProperty("value", out var valueElement))
        {
            value = new PlanFuzzValueSnapshot(
                valueElement.GetProperty("typeIdentity").GetString().NotNull(),
                valueElement.GetProperty("canonicalValue").GetString().NotNull());
        }

        PlanFuzzFailureSnapshot? failure = null;
        if (root.TryGetProperty("failure", out var failureElement))
        {
            failure = new PlanFuzzFailureSnapshot(
                failureElement.GetProperty("failureType").GetString().NotNull(),
                failureElement.GetProperty("stage").GetString().NotNull(),
                failureElement.GetProperty("category").GetString().NotNull(),
                failureElement.TryGetProperty("message", out var message) ? message.GetString() : null);
        }

        PlanFuzzPlanSnapshot? plan = null;
        if (root.TryGetProperty("plan", out var planElement))
        {
            plan = new PlanFuzzPlanSnapshot(
                planElement.GetProperty("planHash").GetString().NotNull(),
                planElement.GetProperty("canonicalLockSha256").GetString().NotNull(),
                planElement.GetProperty("repeatedCanonicalLockSha256").GetString().NotNull(),
                planElement.GetProperty("canonicalLockSemanticSha256").GetString().NotNull(),
                planElement.GetProperty("prettyLockSemanticSha256").GetString().NotNull(),
                planElement.GetProperty("lockSchemaVersion").GetInt32(),
                planElement.GetProperty("lockCanonicalization").GetString().NotNull());
        }

        return new PlanFuzzObservation(
            root.GetProperty("caseId").GetString().NotNull(),
            root.GetProperty("variantId").GetString().NotNull(),
            root.GetProperty("backendId").GetString().NotNull(),
            Enum.Parse<PlanFuzzExecutionOutcome>(root.GetProperty("outcome").GetString().NotNull(), ignoreCase: false),
            value,
            failure,
            plan);
    }
}
