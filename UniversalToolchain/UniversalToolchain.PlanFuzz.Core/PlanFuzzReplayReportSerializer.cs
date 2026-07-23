namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Writes a deterministic human-readable replay summary for research artifacts.
/// </summary>
public static class PlanFuzzReplayReportSerializer
{
    public static string Serialize(PlanFuzzReplayReport report)
    {
        report = report.ArgNotNull();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", PlanFuzzConstants.ReplayReportSchemaVersion);
            writer.WriteString("caseId", report.CaseId);
            writer.WriteBoolean("confirmedViolation", report.IsConfirmedViolation);
            writer.WriteBoolean("clean", report.IsClean);
            writer.WriteBoolean("flaky", report.IsFlaky);
            writer.WriteBoolean("infrastructureFailure", report.IsInfrastructureFailure);
            if (report.ConfirmedFingerprint != null)
                writer.WriteString("confirmedFingerprint", report.ConfirmedFingerprint);
            writer.WritePropertyName("attempts");
            writer.WriteStartArray();
            foreach (var attempt in report.Attempts)
            {
                writer.WriteStartObject();
                writer.WriteNumber("attemptNumber", attempt.AttemptNumber);
                writer.WriteString("fingerprint", attempt.Fingerprint);
                writer.WriteBoolean("hasViolation", attempt.HasViolation);
                writer.WriteBoolean("hasInfrastructureFailure", attempt.HasInfrastructureFailure);
                writer.WritePropertyName("oracleResults");
                writer.WriteStartArray();
                foreach (var result in attempt.OracleResults)
                {
                    writer.WriteStartObject();
                    writer.WriteString("contractId", result.ContractId);
                    writer.WriteString("oracleId", result.OracleId);
                    writer.WriteNumber("oracleVersion", result.OracleVersion);
                    writer.WriteString("status", result.Status.ToString());
                    writer.WriteString("summary", result.Summary);
                    writer.WriteString("fingerprintMaterial", result.FingerprintMaterial);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }
}
