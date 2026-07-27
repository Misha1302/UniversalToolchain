namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Serializes the versioned worker observation protocol.
/// </summary>
public static class PlanFuzzObservationSerializer
{
    public static string Serialize(PlanFuzzObservation observation, bool indented = true)
    {
        observation = observation.ArgNotNull();
        const int schemaVersion = PlanFuzzConstants.ObservationSchemaVersion;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", schemaVersion);
            writer.WriteString("canonicalization", PlanFuzzConstants.Canonicalization);
            writer.WriteString("caseId", observation.CaseId);
            writer.WriteString("variantId", observation.VariantId);
            writer.WriteString("backendId", observation.BackendId);
            writer.WriteString("outcome", observation.Outcome.ToString());
            WriteValue(writer, observation.Value);
            WriteFailure(writer, observation.Failure);
            WritePlan(writer, observation.Plan);
            WriteRoute(writer, observation.Route);
            WriteSurface(writer, observation.Surface);
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
            return Thrower.NotSupported<PlanFuzzObservation>(
                $"Unsupported observation schema version '{schemaVersion}'. Expected '{PlanFuzzConstants.ObservationSchemaVersion}'.");
        var canonicalization = root.GetProperty("canonicalization").GetString();
        if (!StringComparer.Ordinal.Equals(canonicalization, PlanFuzzConstants.Canonicalization))
            return Thrower.NotSupported<PlanFuzzObservation>($"Unsupported observation canonicalization '{canonicalization}'.");

        var value = ReadValue(root);
        var failure = ReadFailure(root);
        var plan = ReadPlan(root);
        var route = ReadRoute(root);
        var surface = ReadSurface(root);

        return new PlanFuzzObservation(
            root.GetProperty("caseId").GetString().NotNull(),
            root.GetProperty("variantId").GetString().NotNull(),
            root.GetProperty("backendId").GetString().NotNull(),
            Enum.Parse<PlanFuzzExecutionOutcome>(root.GetProperty("outcome").GetString().NotNull(), ignoreCase: false),
            value,
            failure,
            plan,
            route,
            surface);
    }

    private static void WriteValue(Utf8JsonWriter writer, PlanFuzzValueSnapshot? value)
    {
        if (value == null)
            return;
        writer.WritePropertyName("value");
        writer.WriteStartObject();
        writer.WriteString("typeIdentity", value.TypeIdentity);
        writer.WriteString("canonicalValue", value.CanonicalValue);
        writer.WriteEndObject();
    }

    private static void WriteFailure(Utf8JsonWriter writer, PlanFuzzFailureSnapshot? failure)
    {
        if (failure == null)
            return;
        writer.WritePropertyName("failure");
        writer.WriteStartObject();
        writer.WriteString("failureType", failure.FailureType);
        writer.WriteString("stage", failure.Stage);
        writer.WriteString("category", failure.Category);
        if (failure.Message != null)
            writer.WriteString("message", failure.Message);
        writer.WriteEndObject();
    }

    private static void WritePlan(Utf8JsonWriter writer, PlanFuzzPlanSnapshot? plan)
    {
        if (plan == null)
            return;
        writer.WritePropertyName("plan");
        writer.WriteStartObject();
        writer.WriteString("planHash", plan.PlanHash);
        writer.WriteString("canonicalLockSha256", plan.CanonicalLockSha256);
        writer.WriteString("repeatedCanonicalLockSha256", plan.RepeatedCanonicalLockSha256);
        writer.WriteString("canonicalLockSemanticSha256", plan.CanonicalLockSemanticSha256);
        writer.WriteString("prettyLockSemanticSha256", plan.PrettyLockSemanticSha256);
        writer.WriteNumber("lockSchemaVersion", plan.LockSchemaVersion);
        writer.WriteString("lockCanonicalization", plan.LockCanonicalization);
        writer.WriteEndObject();
    }

    private static void WriteRoute(Utf8JsonWriter writer, PlanFuzzRouteSnapshot? route)
    {
        if (route == null)
            return;
        writer.WritePropertyName("route");
        writer.WriteStartObject();
        writer.WriteString("routeId", route.RouteId);
        writer.WriteString("requestedPolicy", route.RequestedPolicy);
        writer.WriteBoolean("usedRoute", route.UsedRoute);
        writer.WriteBoolean("fellBack", route.FellBack);
        writer.WriteString("fallbackKind", route.FallbackKind.ToString());
        if (route.Profile != null)
            writer.WriteString("profile", route.Profile);
        writer.WriteNumber("inputInstructionCount", route.InputInstructionCount);
        writer.WriteNumber("outputInstructionCount", route.OutputInstructionCount);
        writer.WritePropertyName("executedPasses");
        writer.WriteStartArray();
        foreach (var pass in route.ExecutedPasses)
            writer.WriteStringValue(pass);
        writer.WriteEndArray();
        writer.WritePropertyName("diagnostics");
        writer.WriteStartArray();
        foreach (var diagnostic in route.Diagnostics)
        {
            writer.WriteStartObject();
            writer.WriteString("code", diagnostic.Code);
            if (diagnostic.Stage != null)
                writer.WriteString("stage", diagnostic.Stage);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }


    private static void WriteSurface(
        Utf8JsonWriter writer,
        PlanFuzzSurfaceSnapshot? surface)
    {
        if (surface == null)
            return;
        writer.WritePropertyName("surface");
        writer.WriteStartObject();
        writer.WriteNumber("evidenceContractVersion", surface.EvidenceContractVersion);
        WriteStrings(writer, "selectedSurfaceIds", surface.SelectedSurfaceIds);
        WriteStrings(writer, "selectedOwnerIds", surface.SelectedOwnerIds);
        WriteStrings(writer, "excludedOwnerIds", surface.ExcludedOwnerIds);
        WriteStrings(writer, "declaredIndependentSurfaceIds", surface.DeclaredIndependentSurfaceIds);
        WriteStrings(writer, "declaredIndependentOwnerIds", surface.DeclaredIndependentOwnerIds);
        WriteStrings(writer, "activatedOwnerIds", surface.ActivatedOwnerIds);
        writer.WriteString("activationTraceStatus", surface.ActivationTraceStatus.ToString());
        writer.WriteString("traceKind", surface.TraceKind);
        writer.WriteString("routeIdentity", surface.RouteIdentity);
        writer.WriteEndObject();
    }

    private static void WriteStrings(Utf8JsonWriter writer, string propertyName, IEnumerable<string> values)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();
        foreach (var value in values)
            writer.WriteStringValue(value);
        writer.WriteEndArray();
    }

    private static PlanFuzzValueSnapshot? ReadValue(JsonElement root)
    {
        if (!root.TryGetProperty("value", out var element))
            return null;
        return new PlanFuzzValueSnapshot(
            element.GetProperty("typeIdentity").GetString().NotNull(),
            element.GetProperty("canonicalValue").GetString().NotNull());
    }

    private static PlanFuzzFailureSnapshot? ReadFailure(JsonElement root)
    {
        if (!root.TryGetProperty("failure", out var element))
            return null;
        return new PlanFuzzFailureSnapshot(
            element.GetProperty("failureType").GetString().NotNull(),
            element.GetProperty("stage").GetString().NotNull(),
            element.GetProperty("category").GetString().NotNull(),
            element.TryGetProperty("message", out var message) ? message.GetString() : null);
    }

    private static PlanFuzzPlanSnapshot? ReadPlan(JsonElement root)
    {
        if (!root.TryGetProperty("plan", out var element))
            return null;
        return new PlanFuzzPlanSnapshot(
            element.GetProperty("planHash").GetString().NotNull(),
            element.GetProperty("canonicalLockSha256").GetString().NotNull(),
            element.GetProperty("repeatedCanonicalLockSha256").GetString().NotNull(),
            element.GetProperty("canonicalLockSemanticSha256").GetString().NotNull(),
            element.GetProperty("prettyLockSemanticSha256").GetString().NotNull(),
            element.GetProperty("lockSchemaVersion").GetInt32(),
            element.GetProperty("lockCanonicalization").GetString().NotNull());
    }

    private static PlanFuzzRouteSnapshot? ReadRoute(JsonElement root)
    {
        if (!root.TryGetProperty("route", out var element))
            return null;
        return new PlanFuzzRouteSnapshot(
            element.GetProperty("routeId").GetString().NotNull(),
            element.GetProperty("requestedPolicy").GetString().NotNull(),
            element.GetProperty("usedRoute").GetBoolean(),
            element.GetProperty("fellBack").GetBoolean(),
            Enum.Parse<PlanFuzzFallbackKind>(element.GetProperty("fallbackKind").GetString().NotNull(), ignoreCase: false),
            element.TryGetProperty("profile", out var profile) ? profile.GetString() : null,
            element.GetProperty("inputInstructionCount").GetInt32(),
            element.GetProperty("outputInstructionCount").GetInt32(),
            element.GetProperty("executedPasses").EnumerateArray().Select(static value => value.GetString().NotNull()).ToArray(),
            element.GetProperty("diagnostics").EnumerateArray().Select(static diagnostic =>
                new PlanFuzzRouteDiagnosticSnapshot(
                    diagnostic.GetProperty("code").GetString().NotNull(),
                    diagnostic.TryGetProperty("stage", out var stage) ? stage.GetString() : null)).ToArray());
    }
    private static PlanFuzzSurfaceSnapshot? ReadSurface(JsonElement root)
    {
        if (!root.TryGetProperty("surface", out var element))
            return null;
        return new PlanFuzzSurfaceSnapshot(
            element.GetProperty("evidenceContractVersion").GetInt32(),
            ReadStrings(element, "selectedSurfaceIds"),
            ReadStrings(element, "selectedOwnerIds"),
            ReadStrings(element, "excludedOwnerIds"),
            ReadStrings(element, "declaredIndependentSurfaceIds"),
            ReadStrings(element, "declaredIndependentOwnerIds"),
            ReadStrings(element, "activatedOwnerIds"),
            Enum.Parse<PlanFuzzActivationTraceStatus>(element.GetProperty("activationTraceStatus").GetString().NotNull(), ignoreCase: false),
            element.GetProperty("traceKind").GetString().NotNull(),
            element.GetProperty("routeIdentity").GetString().NotNull());
    }

    private static string[] ReadStrings(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).EnumerateArray()
            .Select(static value => value.GetString().NotNull())
            .ToArray();

}
