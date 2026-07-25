namespace UniversalToolchain.PlanFuzz.Cli;

internal sealed record PlanFuzzReductionAttempt(
    int Sequence,
    string CandidateId,
    string Dimension,
    string Summary,
    string BeforeCaseId,
    string CandidateCaseId,
    bool Accepted,
    string ReplayStatus,
    string? ConfirmedFingerprint,
    long ProgramComplexity,
    int VariantCount,
    int OracleContractCount);

internal sealed class PlanFuzzReductionReport
{
    public PlanFuzzReductionReport(
        PlanFuzzTestCase originalCase,
        PlanFuzzTestCase reducedCase,
        PlanFuzzReplayReport originalReplay,
        PlanFuzzReplayReport finalReplay,
        long originalProgramComplexity,
        long reducedProgramComplexity,
        int maximumCandidateEvaluations,
        IEnumerable<PlanFuzzReductionAttempt> attempts)
    {
        OriginalCase = originalCase.ArgNotNull();
        ReducedCase = reducedCase.ArgNotNull();
        OriginalReplay = originalReplay.ArgNotNull();
        FinalReplay = finalReplay.ArgNotNull();
        if (originalProgramComplexity < 0 || reducedProgramComplexity < 0)
            Thrower.Argument(nameof(originalProgramComplexity), "Program complexity must not be negative.");
        if (maximumCandidateEvaluations <= 0)
            Thrower.Argument(nameof(maximumCandidateEvaluations), "Maximum candidate evaluations must be positive.");

        OriginalProgramComplexity = originalProgramComplexity;
        ReducedProgramComplexity = reducedProgramComplexity;
        MaximumCandidateEvaluations = maximumCandidateEvaluations;
        Attempts = new ReadOnlyCollection<PlanFuzzReductionAttempt>(attempts.ArgNotNull()
            .OrderBy(static attempt => attempt.Sequence)
            .ToArray());
    }

    public PlanFuzzTestCase OriginalCase { get; }
    public PlanFuzzTestCase ReducedCase { get; }
    public PlanFuzzReplayReport OriginalReplay { get; }
    public PlanFuzzReplayReport FinalReplay { get; }
    public long OriginalProgramComplexity { get; }
    public long ReducedProgramComplexity { get; }
    public int MaximumCandidateEvaluations { get; }
    public IReadOnlyList<PlanFuzzReductionAttempt> Attempts { get; }
    public string? TargetFingerprint => OriginalReplay.ConfirmedFingerprint;
    public int AcceptedSteps => Attempts.Count(static attempt => attempt.Accepted);
    public bool Completed =>
        OriginalReplay.IsConfirmedViolation &&
        FinalReplay.IsConfirmedViolation &&
        StringComparer.Ordinal.Equals(TargetFingerprint, FinalReplay.ConfirmedFingerprint);

    public string Serialize()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteString("originalCaseId", OriginalCase.CaseId);
            writer.WriteString("reducedCaseId", ReducedCase.CaseId);
            writer.WriteString("originalReplayStatus", ReplayStatus(OriginalReplay));
            writer.WriteString("finalReplayStatus", ReplayStatus(FinalReplay));
            if (TargetFingerprint != null)
                writer.WriteString("targetFingerprint", TargetFingerprint);
            if (FinalReplay.ConfirmedFingerprint != null)
                writer.WriteString("finalFingerprint", FinalReplay.ConfirmedFingerprint);
            writer.WriteBoolean("completed", Completed);
            writer.WriteNumber("maximumCandidateEvaluations", MaximumCandidateEvaluations);
            writer.WriteNumber("candidateEvaluations", Attempts.Count);
            writer.WriteNumber("acceptedSteps", AcceptedSteps);
            writer.WritePropertyName("originalComplexity");
            WriteComplexity(writer, OriginalCase, OriginalProgramComplexity);
            writer.WritePropertyName("reducedComplexity");
            WriteComplexity(writer, ReducedCase, ReducedProgramComplexity);
            writer.WritePropertyName("attempts");
            writer.WriteStartArray();
            foreach (var attempt in Attempts)
            {
                writer.WriteStartObject();
                writer.WriteNumber("sequence", attempt.Sequence);
                writer.WriteString("candidateId", attempt.CandidateId);
                writer.WriteString("dimension", attempt.Dimension);
                writer.WriteString("summary", attempt.Summary);
                writer.WriteString("beforeCaseId", attempt.BeforeCaseId);
                writer.WriteString("candidateCaseId", attempt.CandidateCaseId);
                writer.WriteBoolean("accepted", attempt.Accepted);
                writer.WriteString("replayStatus", attempt.ReplayStatus);
                if (attempt.ConfirmedFingerprint != null)
                    writer.WriteString("confirmedFingerprint", attempt.ConfirmedFingerprint);
                writer.WriteNumber("programComplexity", attempt.ProgramComplexity);
                writer.WriteNumber("variantCount", attempt.VariantCount);
                writer.WriteNumber("oracleContractCount", attempt.OracleContractCount);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    public static string ReplayStatus(PlanFuzzReplayReport report)
    {
        report = report.ArgNotNull();
        if (report.IsConfirmedViolation)
            return "confirmed-violation";
        if (report.IsClean)
            return "clean";
        if (report.IsInfrastructureFailure)
            return "infrastructure-failure";
        if (report.IsInconclusive)
            return "inconclusive";
        if (report.IsFlaky)
            return "flaky";
        return "unknown";
    }

    private static void WriteComplexity(
        Utf8JsonWriter writer,
        PlanFuzzTestCase testCase,
        long programComplexity)
    {
        writer.WriteStartObject();
        writer.WriteNumber("program", programComplexity);
        writer.WriteNumber("variants", testCase.Variants.Count);
        writer.WriteNumber("oracleContracts", testCase.OracleContracts.Count);
        writer.WriteNumber("canonicalBodyBytes", PlanFuzzTestCaseSerializer.SerializeCanonicalBody(testCase).Length);
        writer.WriteEndObject();
    }
}
