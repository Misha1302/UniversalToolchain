namespace UniversalToolchain.PlanFuzz.Cli;

internal sealed class PlanFuzzReplayCoordinator
{
    private readonly PlanFuzzWorkerCoordinator _worker;
    private readonly PlanFuzzOracleEngine _oracleEngine = new();

    public PlanFuzzReplayCoordinator(TimeSpan timeout)
    {
        _worker = new PlanFuzzWorkerCoordinator(timeout);
    }

    public async Task<PlanFuzzReplayReport> ReplayAsync(
        string testcasePath,
        string outputDirectory,
        int repeatCount,
        CancellationToken cancellationToken)
    {
        if (repeatCount <= 0)
            return Thrower.Argument<PlanFuzzReplayReport>(nameof(repeatCount), "Replay count must be positive.");
        testcasePath = Path.GetFullPath(testcasePath.ArgNotNull());
        outputDirectory = PlanFuzzOutputDirectory.PrepareEmpty(outputDirectory, nameof(outputDirectory));
        var testCase = PlanFuzzTestCaseSerializer.Deserialize(File.ReadAllText(testcasePath));
        PlanFuzzAtomicFile.WriteAllText(
            Path.Combine(outputDirectory, "case.json"),
            PlanFuzzTestCaseSerializer.Serialize(testCase));

        var attempts = new List<PlanFuzzReplayAttempt>();
        for (var attemptNumber = 1; attemptNumber <= repeatCount; attemptNumber++)
        {
            var attemptDirectory = Path.Combine(outputDirectory, "attempts", attemptNumber.ToString("D3", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(attemptDirectory);
            var observationSetPath = Path.Combine(attemptDirectory, "observations.json");
            var result = await _worker.ExecuteAsync(
                testcasePath,
                testCase,
                observationSetPath,
                cancellationToken).ConfigureAwait(false);
            PlanFuzzAtomicFile.WriteAllText(
                observationSetPath,
                PlanFuzzObservationSetSerializer.Serialize(testCase.CaseId, result.Observations));
            foreach (var observation in result.Observations)
            {
                PlanFuzzAtomicFile.WriteAllText(
                    Path.Combine(attemptDirectory, Sanitize(observation.VariantId) + ".observation.json"),
                    PlanFuzzObservationSerializer.Serialize(observation));
            }
            PlanFuzzAtomicFile.WriteAllText(
                Path.Combine(attemptDirectory, "worker.json"),
                SerializeWorkerMetadata(result));

            var oracleResults = _oracleEngine.Evaluate(testCase, result.Observations);
            var attempt = new PlanFuzzReplayAttempt(attemptNumber, result.Observations, oracleResults);
            attempts.Add(attempt);
            PlanFuzzAtomicFile.WriteAllText(
                Path.Combine(attemptDirectory, "oracle-results.json"),
                SerializeOracleResults(oracleResults));
        }

        var report = new PlanFuzzReplayReport(testCase.CaseId, attempts);
        PlanFuzzAtomicFile.WriteAllText(
            Path.Combine(outputDirectory, "replay-report.json"),
            PlanFuzzReplayReportSerializer.Serialize(report));
        PlanFuzzArtifactManifest.Write(outputDirectory);
        return report;
    }

    private static string SerializeWorkerMetadata(PlanFuzzWorkerResult result)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteNumber("exitCode", result.ExitCode);
            writer.WriteBoolean("timedOut", result.TimedOut);
            writer.WriteString("standardOutput", result.StandardOutput);
            writer.WriteString("standardError", result.StandardError);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    private static string SerializeOracleResults(IEnumerable<PlanFuzzOracleResult> results)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WritePropertyName("results");
            writer.WriteStartArray();
            foreach (var result in results.OrderBy(static item => item.ContractId, StringComparer.Ordinal))
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
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    private static string Sanitize(string value)
    {
        var chars = value.Select(static character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_'
            ? character
            : '_').ToArray();
        return new string(chars);
    }
}
