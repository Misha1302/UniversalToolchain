namespace UniversalToolchain.PlanFuzz.Cli;

internal sealed class PlanFuzzCampaignRunner
{
    private readonly IPlanFuzzLanguageAdapter _adapter;
    private readonly TimeSpan _timeout;

    public PlanFuzzCampaignRunner(IPlanFuzzLanguageAdapter adapter, TimeSpan timeout)
    {
        _adapter = adapter.ArgNotNull();
        _timeout = timeout;
    }

    public async Task<PlanFuzzCampaignSummary> RunAsync(
        ulong campaignSeed,
        int caseCount,
        int confirmationCount,
        string outputDirectory,
        string? seededFaultId,
        bool includeRegressionCorpus,
        CancellationToken cancellationToken)
    {
        if (caseCount <= 0)
            return Thrower.Argument<PlanFuzzCampaignSummary>(nameof(caseCount), "Case count must be positive.");
        if (confirmationCount <= 0)
            return Thrower.Argument<PlanFuzzCampaignSummary>(nameof(confirmationCount), "Confirmation count must be positive.");
        if (includeRegressionCorpus &&
            !_adapter.Descriptor.Capabilities.Contains("regression-corpus", StringComparer.Ordinal))
        {
            return Thrower.NotSupported<PlanFuzzCampaignSummary>(
                $"Adapter '{_adapter.Descriptor.AdapterId}' does not expose a regression corpus.");
        }

        outputDirectory = PlanFuzzOutputDirectory.PrepareEmpty(outputDirectory, nameof(outputDirectory));
        var clean = 0;
        var confirmed = 0;
        var flaky = 0;
        var infrastructure = 0;
        var findingClasses = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < caseCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var testCase = _adapter.GenerateCase(
                campaignSeed,
                index,
                new PlanFuzzCaseGenerationOptions(seededFaultId, includeRegressionCorpus));
            var caseDirectory = Path.Combine(outputDirectory, "cases", testCase.CaseId);
            Directory.CreateDirectory(caseDirectory);
            var casePath = Path.Combine(caseDirectory, "case.json");
            PlanFuzzAtomicFile.WriteAllText(casePath, PlanFuzzTestCaseSerializer.Serialize(testCase));
            var replay = await new PlanFuzzReplayCoordinator(_timeout).ReplayAsync(
                casePath,
                Path.Combine(caseDirectory, "replay"),
                confirmationCount,
                cancellationToken).ConfigureAwait(false);
            if (replay.IsConfirmedViolation)
            {
                confirmed++;
                if (replay.ConfirmedClassFingerprint != null)
                    findingClasses.Add(replay.ConfirmedClassFingerprint);
            }
            else if (replay.IsClean)
                clean++;
            else if (replay.IsInfrastructureFailure)
                infrastructure++;
            else
                flaky++;
            WriteProgress(index + 1, caseCount, clean, confirmed, flaky, infrastructure);
        }

        var summary = new PlanFuzzCampaignSummary(
            campaignSeed,
            caseCount,
            caseCount,
            clean,
            confirmed,
            findingClasses.Count,
            flaky,
            infrastructure,
            _adapter.Descriptor.AdapterId,
            seededFaultId,
            includeRegressionCorpus);
        PlanFuzzAtomicFile.WriteAllText(Path.Combine(outputDirectory, "summary.json"), SerializeSummary(summary));
        PlanFuzzArtifactManifest.Write(outputDirectory);
        return summary;
    }

    private static void WriteProgress(int completed, int requested, int clean, int confirmed, int flaky, int infrastructure)
    {
        Console.WriteLine(
            $"cases: {completed.ToString(CultureInfo.InvariantCulture)}/{requested.ToString(CultureInfo.InvariantCulture)}; " +
            $"clean: {clean.ToString(CultureInfo.InvariantCulture)}; " +
            $"confirmed: {confirmed.ToString(CultureInfo.InvariantCulture)}; " +
            $"flaky: {flaky.ToString(CultureInfo.InvariantCulture)}; " +
            $"infrastructure: {infrastructure.ToString(CultureInfo.InvariantCulture)}");
    }

    private static string SerializeSummary(PlanFuzzCampaignSummary summary)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 3);
            writer.WriteString("adapterId", summary.AdapterId);
            writer.WriteNumber("campaignSeed", summary.CampaignSeed);
            writer.WriteNumber("requestedCases", summary.RequestedCases);
            writer.WriteNumber("completedCases", summary.CompletedCases);
            writer.WriteNumber("cleanCases", summary.CleanCases);
            writer.WriteNumber("confirmedFindings", summary.ConfirmedFindings);
            writer.WriteNumber("distinctFindingClasses", summary.DistinctFindingClasses);
            writer.WriteNumber("flakyCases", summary.FlakyCases);
            writer.WriteNumber("infrastructureFailures", summary.InfrastructureFailures);
            writer.WriteBoolean("includedRegressionCorpus", summary.IncludedRegressionCorpus);
            if (summary.SeededFaultId != null)
                writer.WriteString("seededFaultId", summary.SeededFaultId);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }
}
