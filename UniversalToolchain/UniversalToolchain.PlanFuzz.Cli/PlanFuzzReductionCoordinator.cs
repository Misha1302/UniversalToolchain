namespace UniversalToolchain.PlanFuzz.Cli;

internal sealed class PlanFuzzReductionCoordinator
{
    private readonly IPlanFuzzLanguageAdapter _adapter;
    private readonly IPlanFuzzProgramReducer? _programReducer;
    private readonly TimeSpan _timeout;

    public PlanFuzzReductionCoordinator(
        IPlanFuzzLanguageAdapter adapter,
        TimeSpan timeout)
    {
        _adapter = adapter.ArgNotNull();
        _programReducer = adapter as IPlanFuzzProgramReducer;
        _timeout = timeout;
    }

    public async Task<PlanFuzzReductionReport> ReduceAsync(
        string testcasePath,
        string outputDirectory,
        int repeatCount,
        int maximumCandidateEvaluations,
        CancellationToken cancellationToken)
    {
        if (repeatCount <= 0)
            return Thrower.Argument<PlanFuzzReductionReport>(nameof(repeatCount), "Reduction replay count must be positive.");
        if (maximumCandidateEvaluations <= 0)
        {
            return Thrower.Argument<PlanFuzzReductionReport>(
                nameof(maximumCandidateEvaluations),
                "Maximum candidate evaluations must be positive.");
        }

        testcasePath = Path.GetFullPath(testcasePath.ArgNotNull());
        outputDirectory = PlanFuzzOutputDirectory.PrepareEmpty(outputDirectory, nameof(outputDirectory));
        var originalCase = PlanFuzzTestCaseSerializer.Deserialize(File.ReadAllText(testcasePath));
        EnsureAdapterIdentity(originalCase);
        var normalizedOriginalPath = Path.Combine(outputDirectory, "original-case.json");
        PlanFuzzAtomicFile.WriteAllText(normalizedOriginalPath, PlanFuzzTestCaseSerializer.Serialize(originalCase));

        var replayCoordinator = new PlanFuzzReplayCoordinator(_timeout);
        var originalReplay = await replayCoordinator.ReplayAsync(
            normalizedOriginalPath,
            Path.Combine(outputDirectory, "original-replay"),
            repeatCount,
            cancellationToken).ConfigureAwait(false);
        var originalComplexity = GetProgramComplexity(originalCase);

        if (!originalReplay.IsConfirmedViolation)
        {
            PlanFuzzAtomicFile.WriteAllText(
                Path.Combine(outputDirectory, "reduced-case.json"),
                PlanFuzzTestCaseSerializer.Serialize(originalCase));
            var preconditionReport = new PlanFuzzReductionReport(
                originalCase,
                originalCase,
                originalReplay,
                originalReplay,
                originalComplexity,
                originalComplexity,
                maximumCandidateEvaluations,
                []);
            PlanFuzzAtomicFile.WriteAllText(
                Path.Combine(outputDirectory, "reduction-report.json"),
                preconditionReport.Serialize());
            PlanFuzzArtifactManifest.Write(outputDirectory);
            return preconditionReport;
        }

        var targetFingerprint = originalReplay.ConfirmedFingerprint.NotNull();
        var currentCase = originalCase;
        var currentReplay = originalReplay;
        var currentProgramComplexity = originalComplexity;
        var attempts = new List<PlanFuzzReductionAttempt>();
        var seenCaseIds = new HashSet<string>(StringComparer.Ordinal) { originalCase.CaseId };
        var sequence = 0;

        while (sequence < maximumCandidateEvaluations)
        {
            var acceptedInPass = false;
            foreach (var candidate in CreateCandidates(currentCase, currentReplay, currentProgramComplexity))
            {
                if (sequence >= maximumCandidateEvaluations)
                    break;
                if (!seenCaseIds.Add(candidate.TestCase.CaseId))
                    continue;

                sequence++;
                var candidateRoot = Path.Combine(
                    outputDirectory,
                    "candidates",
                    $"{sequence.ToString("D4", CultureInfo.InvariantCulture)}-{Sanitize(candidate.CandidateId)}");
                Directory.CreateDirectory(candidateRoot);
                var candidateCasePath = Path.Combine(candidateRoot, "case.json");
                PlanFuzzAtomicFile.WriteAllText(
                    candidateCasePath,
                    PlanFuzzTestCaseSerializer.Serialize(candidate.TestCase));
                var candidateReplay = await replayCoordinator.ReplayAsync(
                    candidateCasePath,
                    Path.Combine(candidateRoot, "replay"),
                    repeatCount,
                    cancellationToken).ConfigureAwait(false);
                var accepted = candidateReplay.IsConfirmedViolation &&
                               StringComparer.Ordinal.Equals(targetFingerprint, candidateReplay.ConfirmedFingerprint);
                attempts.Add(new PlanFuzzReductionAttempt(
                    sequence,
                    candidate.CandidateId,
                    candidate.Dimension,
                    candidate.Summary,
                    currentCase.CaseId,
                    candidate.TestCase.CaseId,
                    accepted,
                    PlanFuzzReductionReport.ReplayStatus(candidateReplay),
                    candidateReplay.ConfirmedFingerprint,
                    candidate.ProgramComplexity,
                    candidate.TestCase.Variants.Count,
                    candidate.TestCase.OracleContracts.Count));
                PlanFuzzAtomicFile.WriteAllText(
                    Path.Combine(candidateRoot, "decision.json"),
                    SerializeDecision(attempts[^1], targetFingerprint));

                if (!accepted)
                    continue;

                currentCase = candidate.TestCase;
                currentReplay = candidateReplay;
                currentProgramComplexity = candidate.ProgramComplexity;
                acceptedInPass = true;
                break;
            }

            if (!acceptedInPass)
                break;
        }

        PlanFuzzAtomicFile.WriteAllText(
            Path.Combine(outputDirectory, "reduced-case.json"),
            PlanFuzzTestCaseSerializer.Serialize(currentCase));
        var report = new PlanFuzzReductionReport(
            originalCase,
            currentCase,
            originalReplay,
            currentReplay,
            originalComplexity,
            currentProgramComplexity,
            maximumCandidateEvaluations,
            attempts);
        PlanFuzzAtomicFile.WriteAllText(
            Path.Combine(outputDirectory, "reduction-report.json"),
            report.Serialize());
        PlanFuzzArtifactManifest.Write(outputDirectory);
        return report;
    }

    private IEnumerable<ReductionCandidate> CreateCandidates(
        PlanFuzzTestCase currentCase,
        PlanFuzzReplayReport currentReplay,
        long currentProgramComplexity)
    {
        if (_programReducer != null)
        {
            foreach (var programCandidate in _programReducer
                         .GetProgramReductionCandidates(currentCase)
                         .Where(candidate => candidate.Complexity < currentProgramComplexity)
                         .OrderBy(static candidate => candidate.Complexity)
                         .ThenBy(static candidate => candidate.CandidateId, StringComparer.Ordinal))
            {
                yield return new ReductionCandidate(
                    $"program-{programCandidate.CandidateId}",
                    "program",
                    programCandidate.Summary,
                    PlanFuzzTestCaseTransform.WithProgram(currentCase, programCandidate.Program),
                    programCandidate.Complexity);
            }
        }

        var firstAttempt = currentReplay.Attempts[0];
        var passedContractIds = firstAttempt.OracleResults
            .Where(static result => result.Status == PlanFuzzOracleStatus.Passed)
            .Select(static result => result.ContractId)
            .OrderBy(static contractId => contractId, StringComparer.Ordinal)
            .ToArray();
        if (passedContractIds.Length == 0)
            yield break;

        if (passedContractIds.Length < currentCase.OracleContracts.Count)
        {
            var passedSet = passedContractIds.ToHashSet(StringComparer.Ordinal);
            var onlyViolatingContracts = currentCase.OracleContracts
                .Where(contract => !passedSet.Contains(contract.ContractId))
                .ToArray();
            if (onlyViolatingContracts.Length > 0)
            {
                yield return new ReductionCandidate(
                    "plan-remove-all-passed-contracts",
                    "plan",
                    "Remove every currently passing oracle contract and prune variants no longer required by a violation.",
                    PlanFuzzTestCaseTransform.WithContractsAndReferencedVariants(currentCase, onlyViolatingContracts),
                    currentProgramComplexity);
            }
        }

        foreach (var contractId in passedContractIds)
        {
            var remainingContracts = currentCase.OracleContracts
                .Where(contract => !StringComparer.Ordinal.Equals(contract.ContractId, contractId))
                .ToArray();
            if (remainingContracts.Length == 0)
                continue;
            yield return new ReductionCandidate(
                $"plan-remove-contract-{contractId}",
                "plan",
                $"Remove passing oracle contract '{contractId}' and prune unreferenced variants.",
                PlanFuzzTestCaseTransform.WithContractsAndReferencedVariants(currentCase, remainingContracts),
                currentProgramComplexity);
        }
    }

    private long GetProgramComplexity(PlanFuzzTestCase testCase) =>
        _programReducer?.GetProgramComplexity(testCase) ?? testCase.Program.Model.CanonicalJson.Length;

    private void EnsureAdapterIdentity(PlanFuzzTestCase testCase)
    {
        if (!StringComparer.Ordinal.Equals(testCase.AdapterId, _adapter.Descriptor.AdapterId) ||
            !StringComparer.Ordinal.Equals(testCase.AdapterVersion, _adapter.Descriptor.AdapterVersion))
        {
            Thrower.Argument(
                nameof(testCase),
                $"Testcase adapter '{testCase.AdapterId}@{testCase.AdapterVersion}' does not match reducer adapter " +
                $"'{_adapter.Descriptor.AdapterId}@{_adapter.Descriptor.AdapterVersion}'.");
        }
    }

    private static string SerializeDecision(
        PlanFuzzReductionAttempt attempt,
        string targetFingerprint)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteNumber("sequence", attempt.Sequence);
            writer.WriteString("candidateId", attempt.CandidateId);
            writer.WriteString("dimension", attempt.Dimension);
            writer.WriteString("beforeCaseId", attempt.BeforeCaseId);
            writer.WriteString("candidateCaseId", attempt.CandidateCaseId);
            writer.WriteString("targetFingerprint", targetFingerprint);
            writer.WriteString("replayStatus", attempt.ReplayStatus);
            if (attempt.ConfirmedFingerprint != null)
                writer.WriteString("confirmedFingerprint", attempt.ConfirmedFingerprint);
            writer.WriteBoolean("accepted", attempt.Accepted);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    private static string Sanitize(string value) =>
        new(value.Select(static character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_'
            ? character
            : '_').ToArray());

    private sealed record ReductionCandidate(
        string CandidateId,
        string Dimension,
        string Summary,
        PlanFuzzTestCase TestCase,
        long ProgramComplexity);
}
