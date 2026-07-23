namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Serializes testcase schema v1 with stable property ordering and verifies the embedded case identity.
/// </summary>
public static class PlanFuzzTestCaseSerializer
{
    public static string Serialize(PlanFuzzTestCase testCase, bool indented = true)
    {
        testCase = testCase.ArgNotNull();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", testCase.SchemaVersion);
            writer.WriteString("canonicalization", PlanFuzzConstants.Canonicalization);
            writer.WriteString("caseId", ComputeCaseId(testCase));
            WriteBody(writer, testCase);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    public static byte[] SerializeCanonicalBody(PlanFuzzTestCase testCase)
    {
        testCase = testCase.ArgNotNull();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", testCase.SchemaVersion);
            writer.WriteString("canonicalization", PlanFuzzConstants.Canonicalization);
            WriteBody(writer, testCase);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static string ComputeCaseId(PlanFuzzTestCase testCase) =>
        Convert.ToHexString(SHA256.HashData(SerializeCanonicalBody(testCase))).ToLowerInvariant();

    public static PlanFuzzTestCase Deserialize(string json)
    {
        json = json.ArgNotNull();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var schemaVersion = root.GetProperty("schemaVersion").GetInt32();
        if (schemaVersion != PlanFuzzConstants.CaseSchemaVersion)
            return Thrower.NotSupported<PlanFuzzTestCase>($"Unsupported testcase schema version '{schemaVersion}'.");
        var canonicalization = root.GetProperty("canonicalization").GetString();
        if (!StringComparer.Ordinal.Equals(canonicalization, PlanFuzzConstants.Canonicalization))
            return Thrower.NotSupported<PlanFuzzTestCase>($"Unsupported testcase canonicalization '{canonicalization}'.");

        var programElement = root.GetProperty("program");
        var program = new PlanFuzzProgram(
            programElement.GetProperty("modelKind").GetString().NotNull("Program model kind is missing."),
            programElement.GetProperty("modelSchemaVersion").GetInt32(),
            PlanFuzzPayload.FromJson(programElement.GetProperty("model").GetRawText()),
            programElement.GetProperty("sourceText").GetString().NotNull("Program source text is missing."),
            Enum.Parse<PlanFuzzProgramClass>(programElement.GetProperty("programClass").GetString().NotNull(), ignoreCase: false));

        var variants = root.GetProperty("variants").EnumerateArray()
            .Select(static item => new PlanFuzzPlanVariant(
                item.GetProperty("variantId").GetString().NotNull(),
                item.GetProperty("configurationId").GetString().NotNull(),
                item.GetProperty("backendId").GetString().NotNull(),
                Enum.Parse<PlanFuzzVariantRole>(item.GetProperty("role").GetString().NotNull(), ignoreCase: false),
                Enum.Parse<PlanFuzzExpectedRelation>(item.GetProperty("expectedRelation").GetString().NotNull(), ignoreCase: false),
                ReadOptionalString(item, "mutationId"),
                ReadOptionalString(item, "seededFaultId")))
            .ToArray();

        var oracleContracts = root.GetProperty("oracleContracts").EnumerateArray()
            .Select(static item => new PlanFuzzOracleContract(
                item.GetProperty("contractId").GetString().NotNull(),
                item.GetProperty("oracleId").GetString().NotNull(),
                item.GetProperty("oracleVersion").GetInt32(),
                item.GetProperty("variantIds").EnumerateArray().Select(static value => value.GetString().NotNull()).ToArray()))
            .ToArray();

        var testCase = new PlanFuzzTestCase(
            schemaVersion,
            root.GetProperty("adapterId").GetString().NotNull(),
            root.GetProperty("adapterVersion").GetString().NotNull(),
            root.GetProperty("campaignSeed").GetUInt64(),
            root.GetProperty("caseIndex").GetInt64(),
            root.GetProperty("caseSeed").GetUInt64(),
            root.GetProperty("prngAlgorithm").GetString().NotNull(),
            program,
            variants,
            oracleContracts);

        var recordedCaseId = root.GetProperty("caseId").GetString().NotNull("Case ID is missing.");
        var actualCaseId = ComputeCaseId(testCase);
        if (!StringComparer.Ordinal.Equals(recordedCaseId, actualCaseId))
            return Thrower.InvalidOpEx<PlanFuzzTestCase>($"Testcase case ID mismatch: recorded '{recordedCaseId}', actual '{actualCaseId}'.");
        return testCase;
    }

    private static void WriteBody(Utf8JsonWriter writer, PlanFuzzTestCase testCase)
    {
        writer.WriteString("adapterId", testCase.AdapterId);
        writer.WriteString("adapterVersion", testCase.AdapterVersion);
        writer.WriteNumber("campaignSeed", testCase.CampaignSeed);
        writer.WriteNumber("caseIndex", testCase.CaseIndex);
        writer.WriteNumber("caseSeed", testCase.CaseSeed);
        writer.WriteString("prngAlgorithm", testCase.PrngAlgorithm);
        writer.WritePropertyName("program");
        writer.WriteStartObject();
        writer.WriteString("modelKind", testCase.Program.ModelKind);
        writer.WriteNumber("modelSchemaVersion", testCase.Program.ModelSchemaVersion);
        writer.WritePropertyName("model");
        writer.WriteRawValue(testCase.Program.Model.CanonicalJson, skipInputValidation: true);
        writer.WriteString("sourceText", testCase.Program.SourceText);
        writer.WriteString("programClass", testCase.Program.ProgramClass.ToString());
        writer.WriteEndObject();
        writer.WritePropertyName("variants");
        writer.WriteStartArray();
        foreach (var variant in testCase.Variants)
        {
            writer.WriteStartObject();
            writer.WriteString("variantId", variant.VariantId);
            writer.WriteString("configurationId", variant.ConfigurationId);
            writer.WriteString("backendId", variant.BackendId);
            writer.WriteString("role", variant.Role.ToString());
            writer.WriteString("expectedRelation", variant.ExpectedRelation.ToString());
            if (variant.MutationId != null)
                writer.WriteString("mutationId", variant.MutationId);
            if (variant.SeededFaultId != null)
                writer.WriteString("seededFaultId", variant.SeededFaultId);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WritePropertyName("oracleContracts");
        writer.WriteStartArray();
        foreach (var contract in testCase.OracleContracts)
        {
            writer.WriteStartObject();
            writer.WriteString("contractId", contract.ContractId);
            writer.WriteString("oracleId", contract.OracleId);
            writer.WriteNumber("oracleVersion", contract.OracleVersion);
            writer.WritePropertyName("variantIds");
            writer.WriteStartArray();
            foreach (var variantId in contract.VariantIds)
                writer.WriteStringValue(variantId);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
}
