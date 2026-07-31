using System.Diagnostics;
using System.Text.Json;
using BasicCore.Core;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.ContractExperiments;

internal enum ExperimentPolicy { P0_STRUCTURAL, P1_INVALIDATION, P2_SELECTIVE, P3_ALWAYS }

internal sealed record MutationCase(
    string Id,
    string OperatorId,
    string StudySet,
    string Family,
    string ExpectedCode,
    Func<ExperimentPolicy, ExperimentOutcome> Execute);

internal sealed record TelemetrySnapshot(
    int VerifierInvocationsTotal,
    IReadOnlyDictionary<string, int> VerifierInvocationsByRule,
    long VerificationElapsedNanoseconds,
    long PipelineElapsedNanoseconds,
    long AllocatedBytes,
    long PeakWorkingSetBytes,
    int ObligationsCreated,
    int ObligationsDischarged,
    int ObligationsFailed,
    int FactsInvalidated,
    int FactsReverified);

internal sealed record ExperimentOutcome(
    bool Detected,
    string? DiagnosticCode,
    string Boundary,
    long ElapsedTicks,
    TelemetrySnapshot Telemetry);

internal sealed record ResultRecord(
    string RunId,
    string Commit,
    string MutationId,
    string OperatorId,
    string StudySet,
    string Family,
    string Policy,
    int Repetition,
    bool Detected,
    string? DiagnosticCode,
    string Boundary,
    long ElapsedTicks,
    string? ExpectedDiagnosticCode,
    string ExpectedBoundary,
    TelemetrySnapshot Telemetry);

internal static partial class Cgo27Program
{
    private const int Repetitions = 3;
    private const int RawSchemaVersion = 3;

    [ThreadStatic]
    private static TelemetryCollector? _activeTelemetry;

    private sealed class TelemetryCollector
    {
        private readonly Dictionary<string, int> _invocationsByRule = new(StringComparer.Ordinal);

        public long VerificationElapsedNanoseconds { get; private set; }
        public int VerifierInvocationsTotal => _invocationsByRule.Values.Sum();
        public int ObligationsCreated { get; private set; }
        public int ObligationsDischarged { get; private set; }
        public int ObligationsFailed { get; private set; }
        public int FactsInvalidated { get; private set; }
        public int FactsReverified { get; private set; }

        public T Invoke<T>(string ruleId, Func<T> action)
        {
            if (!_invocationsByRule.TryAdd(ruleId, 1))
                _invocationsByRule[ruleId]++;
            var started = Stopwatch.GetTimestamp();
            try
            {
                return action();
            }
            finally
            {
                VerificationElapsedNanoseconds += ToNanoseconds(Stopwatch.GetTimestamp() - started);
            }
        }

        public void RecordPipeline(PipelineEffectValidationResult result)
        {
            FactsInvalidated += result.OutputFacts.Invalidated.Count;
            ObligationsCreated += result.ReverificationRequests.Sum(static request => request.InvalidatedFacts.Count);
        }

        public void RecordReverification(int factCount, bool succeeded)
        {
            FactsReverified += factCount;
            if (succeeded)
                ObligationsDischarged += factCount;
            else
                ObligationsFailed += factCount;
        }

        public TelemetrySnapshot Snapshot(long pipelineElapsedNanoseconds, long allocatedBytes, long peakWorkingSetBytes) =>
            new(
                _invocationsByRule.Values.Sum(),
                new Dictionary<string, int>(_invocationsByRule, StringComparer.Ordinal),
                VerificationElapsedNanoseconds,
                pipelineElapsedNanoseconds,
                allocatedBytes,
                peakWorkingSetBytes,
                ObligationsCreated,
                ObligationsDischarged,
                ObligationsFailed,
                FactsInvalidated,
                FactsReverified);
    }

    public static int Main(string[] args)
    {
        var outputDirectory = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath("artifacts/contract-experiment");
        Directory.CreateDirectory(outputDirectory);

        var commit = Environment.GetEnvironmentVariable("GITHUB_SHA")
                     ?? Environment.GetEnvironmentVariable("CGO27_EXPERIMENT_COMMIT")
                     ?? Environment.GetEnvironmentVariable("ICSE_EXPERIMENT_COMMIT")
                     ?? "local-uncommitted";
        var runId = Environment.GetEnvironmentVariable("CGO27_RUN_ID")
                    ?? $"cgo27-{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Environment.ProcessId}";
        var cases = BuildCases();
        var primaryCases = cases.Where(static x => x.StudySet == "primary").ToArray();
        var challengeCases = cases.Where(static x => x.StudySet == "challenge").ToArray();
        if (primaryCases.Length != 40 || primaryCases.Select(static x => x.OperatorId).Distinct().Count() != 32)
            throw new InvalidOperationException("Expected 40 primary instances representing 32 operator shapes.");
        if (challengeCases.Length != 10 || challengeCases.Select(static x => x.OperatorId).Distinct().Count() != 10)
            throw new InvalidOperationException("Expected 10 post-freeze challenge operators.");

        var results = new List<ResultRecord>();
        foreach (var mutation in cases.OrderBy(static x => x.Id, StringComparer.Ordinal))
        {
            foreach (var mode in Enum.GetValues<ExperimentPolicy>())
            {
                for (var repetition = 1; repetition <= Repetitions; repetition++)
                {
                    var outcome = mutation.Execute(mode);
                    results.Add(CreateFaultRecord(runId, commit, mutation, mode, repetition, outcome));
                }
            }
        }

        ValidateTriplets(results, cases);
        var clean = RunCleanCorpus(runId, commit);
        results.AddRange(clean);
        ValidatePolicyInvariants(results);

        var jsonlPath = Path.Combine(outputDirectory, "results.jsonl");
        using (var writer = new StreamWriter(jsonlPath, false))
        {
            foreach (var result in results)
                writer.WriteLine(SerializeRawRecord(result));
        }

        ValidateRawRecords(results);
        var performance = MeasurePerformance();
        var summary = BuildSummary(results, performance);
        var summaryPath = Path.Combine(outputDirectory, "summary.json");
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(outputDirectory, "mutations.csv"), BuildMutationCatalog(cases));
        File.WriteAllText(Path.Combine(outputDirectory, "environment.json"), JsonSerializer.Serialize(new
        {
            SchemaVersion = RawSchemaVersion,
            RunId = runId,
            Commit = commit,
            Policies = Enum.GetNames<ExperimentPolicy>(),
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            StopwatchFrequency = Stopwatch.Frequency
        }, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine("CGO27_CONTRACT_EXPERIMENT_SUMMARY=" + JsonSerializer.Serialize(summary));
        Console.WriteLine($"Raw records: {jsonlPath}");
        return 0;
    }

    private static ResultRecord CreateFaultRecord(
        string runId,
        string commit,
        MutationCase mutation,
        ExperimentPolicy policy,
        int repetition,
        ExperimentOutcome outcome) =>
        new(
            runId,
            commit,
            mutation.Id,
            mutation.OperatorId,
            mutation.StudySet,
            mutation.Family,
            policy.ToString(),
            repetition,
            outcome.Detected,
            outcome.DiagnosticCode,
            outcome.Boundary,
            outcome.ElapsedTicks,
            mutation.ExpectedCode,
            ExpectedBoundary(mutation.Id, mutation.Family),
            outcome.Telemetry);

    private static ResultRecord CreateControlRecord(
        string runId,
        string commit,
        string caseId,
        string family,
        ExperimentPolicy policy,
        ExperimentOutcome outcome) =>
        new(
            runId,
            commit,
            caseId,
            caseId,
            "control",
            family,
            policy.ToString(),
            1,
            outcome.Detected,
            outcome.DiagnosticCode,
            outcome.Boundary,
            outcome.ElapsedTicks,
            null,
            ExpectedBoundary(caseId, family),
            outcome.Telemetry);

    private static string ExpectedBoundary(string caseId, string family)
    {
        if (caseId.StartsWith("OWN-", StringComparison.Ordinal))
            return caseId is "OWN-05" or "OWN-06" ? "ast-ownership" : "contract-table";
        if (caseId.StartsWith("BYTE-", StringComparison.Ordinal))
            return "bytecode";
        if (caseId.StartsWith("FACT-", StringComparison.Ordinal))
            return "pipeline-effects";
        if (caseId.StartsWith("AIR-", StringComparison.Ordinal))
            return "air-structure";
        if (caseId.StartsWith("CAP-", StringComparison.Ordinal))
            return "capability-target";
        if (caseId.StartsWith("CH-NS-", StringComparison.Ordinal) ||
            caseId.StartsWith("CH-SCHEMA-", StringComparison.Ordinal) ||
            caseId.StartsWith("CH-FACT-", StringComparison.Ordinal))
            return "contract-table";
        if (caseId.StartsWith("CH-SELECT-", StringComparison.Ordinal))
            return "challenge-selection";
        if (caseId.StartsWith("CH-LOWER-", StringComparison.Ordinal))
            return "challenge-lowerer";
        if (caseId.StartsWith("CH-META-", StringComparison.Ordinal))
            return "challenge-bytecode-metadata";
        if (caseId.StartsWith("CH-CAP-", StringComparison.Ordinal))
            return "challenge-capability-selection";
        return family;
    }

    private static string SerializeRawRecord(ResultRecord result)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schema_version"] = RawSchemaVersion,
            ["run_id"] = result.RunId,
            ["commit_sha"] = result.Commit,
            ["policy"] = result.Policy,
            ["corpus_id"] = result.StudySet,
            ["case_id"] = result.MutationId,
            ["case_kind"] = result.StudySet == "control" ? "valid-control" : "fault",
            ["language_id"] = "wist-contract-model",
            ["pipeline_id"] = "module-contract-boundary",
            ["workload_stratum"] = result.Family,
            ["expected_outcome"] = result.StudySet == "control" ? "accepted" : "rejected",
            ["actual_outcome"] = result.Detected ? "rejected" : "accepted",
            ["expected_diagnostic_family"] = result.ExpectedDiagnosticCode,
            ["actual_diagnostic_family"] = result.DiagnosticCode,
            ["expected_boundary"] = result.ExpectedBoundary,
            ["first_detection_boundary"] = result.Detected ? result.Boundary : null,
            ["verifier_invocations_total"] = result.Telemetry.VerifierInvocationsTotal,
            ["verifier_invocations_by_rule"] = result.Telemetry.VerifierInvocationsByRule,
            ["verification_elapsed_ns"] = result.Telemetry.VerificationElapsedNanoseconds,
            ["pipeline_elapsed_ns"] = result.Telemetry.PipelineElapsedNanoseconds,
            ["whole_compilation_elapsed_ns"] = null,
            ["allocated_bytes"] = result.Telemetry.AllocatedBytes,
            ["peak_working_set_bytes"] = result.Telemetry.PeakWorkingSetBytes,
            ["obligations_created"] = result.Telemetry.ObligationsCreated,
            ["obligations_discharged"] = result.Telemetry.ObligationsDischarged,
            ["obligations_failed"] = result.Telemetry.ObligationsFailed,
            ["facts_invalidated"] = result.Telemetry.FactsInvalidated,
            ["facts_reverified"] = result.Telemetry.FactsReverified,
            ["process_exit_code"] = 0,
            ["repetition"] = result.Repetition,
            ["seed"] = 0,
            ["measurement_scope"] = "boundary-kernel",
            ["operator_id"] = result.OperatorId,
            ["detected"] = result.Detected
        };
        return JsonSerializer.Serialize(payload);
    }

    private static void ValidateRawRecords(IReadOnlyList<ResultRecord> results)
    {
        var required = new[]
        {
            "run_id", "commit_sha", "policy", "corpus_id", "case_id", "case_kind", "language_id",
            "pipeline_id", "workload_stratum", "expected_outcome", "actual_outcome",
            "expected_diagnostic_family", "actual_diagnostic_family", "expected_boundary",
            "first_detection_boundary", "verifier_invocations_total", "verifier_invocations_by_rule",
            "verification_elapsed_ns", "pipeline_elapsed_ns", "whole_compilation_elapsed_ns",
            "allocated_bytes", "peak_working_set_bytes", "obligations_created", "obligations_discharged",
            "obligations_failed", "facts_invalidated", "facts_reverified", "process_exit_code",
            "repetition", "seed"
        };
        foreach (var result in results)
        {
            using var document = JsonDocument.Parse(SerializeRawRecord(result));
            foreach (var field in required)
            {
                if (!document.RootElement.TryGetProperty(field, out _))
                    throw new InvalidOperationException($"Raw schema field '{field}' is missing for {result.MutationId}/{result.Policy}.");
            }
            if (!Enum.TryParse<ExperimentPolicy>(document.RootElement.GetProperty("policy").GetString(), out _))
                throw new InvalidOperationException($"Unknown policy in raw record for {result.MutationId}.");
            if (result.Telemetry.VerifierInvocationsTotal != result.Telemetry.VerifierInvocationsByRule.Values.Sum())
                throw new InvalidOperationException($"Verifier invocation accounting mismatch for {result.MutationId}/{result.Policy}.");
            if (result.Telemetry.VerificationElapsedNanoseconds > result.Telemetry.PipelineElapsedNanoseconds)
                throw new InvalidOperationException($"Verification time exceeds pipeline time for {result.MutationId}/{result.Policy}.");
        }
    }

    private static long ToNanoseconds(long stopwatchTicks) =>
        checked((long)Math.Round(stopwatchTicks * (1_000_000_000.0 / Stopwatch.Frequency)));

    private static IReadOnlyList<MutationCase> BuildCases()
    {
        var cases = new List<MutationCase>();
        AddOwnershipCases(cases);
        AddBytecodeCases(cases);
        AddFactCases(cases);
        AddAirStructureCases(cases);
        AddCapabilityCases(cases);
        AddChallengeCases(cases);
        return cases;
    }


}
