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

internal static partial class Cgo27Program
{    private static ExperimentOutcome Timed(
        ExperimentPolicy policy,
        string boundary,
        Func<(bool Detected, string? Code)> action)
    {
        var previousTelemetry = _activeTelemetry;
        var telemetry = new TelemetryCollector();
        _activeTelemetry = telemetry;
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var process = Process.GetCurrentProcess();
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = action();
            var elapsedTicks = Stopwatch.GetTimestamp() - started;
            var elapsedNanoseconds = ToNanoseconds(elapsedTicks);
            process.Refresh();
            var allocatedBytes = Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
            var snapshot = telemetry.Snapshot(elapsedNanoseconds, allocatedBytes, process.PeakWorkingSet64);
            return new ExperimentOutcome(result.Detected, result.Code, boundary, elapsedTicks, snapshot);
        }
        finally
        {
            _activeTelemetry = previousTelemetry;
            process.Dispose();
        }
    }

    private static T InvokeVerifier<T>(string ruleId, Func<T> action) =>
        _activeTelemetry == null ? action() : _activeTelemetry.Invoke(ruleId, action);

    private static void ValidateTriplets(IReadOnlyList<ResultRecord> results, IReadOnlyList<MutationCase> cases)
    {
        foreach (var mutation in cases)
        {
            foreach (var mode in Enum.GetNames<ExperimentPolicy>())
            {
                var group = results.Where(x => x.MutationId == mutation.Id && x.Policy == mode).ToArray();
                if (group.Length != Repetitions)
                    throw new InvalidOperationException($"Incomplete triplet for {mutation.Id}/{mode}.");
                if (group.Select(static x => (x.Detected, x.DiagnosticCode, x.Boundary)).Distinct().Count() != 1)
                    throw new InvalidOperationException($"Flaky classification for {mutation.Id}/{mode}.");
            }
        }

        foreach (var group in results
                     .Where(static x => x.StudySet is "primary" or "challenge")
                     .GroupBy(static x => (x.StudySet, x.OperatorId, x.Policy)))
        {
            if (group.Select(static x => x.Detected).Distinct().Count() != 1)
                throw new InvalidOperationException($"Operator shape {group.Key} has inconsistent instance classifications.");
        }
    }

    private static void ValidatePolicyInvariants(IReadOnlyList<ResultRecord> results)
    {
        var stable = results
            .Where(static result => result.StudySet is "primary" or "challenge")
            .GroupBy(static result => (result.MutationId, result.Policy))
            .Select(static group => group.First())
            .ToArray();
        foreach (var mutationId in stable.Select(static result => result.MutationId).Distinct())
        {
            var selective = stable.Single(result => result.MutationId == mutationId && result.Policy == nameof(ExperimentPolicy.P2_SELECTIVE));
            var always = stable.Single(result => result.MutationId == mutationId && result.Policy == nameof(ExperimentPolicy.P3_ALWAYS));
            if ((selective.Detected, selective.DiagnosticCode) != (always.Detected, always.DiagnosticCode))
                throw new InvalidOperationException($"Selective/always correctness parity failed for {mutationId}.");
        }

        if (results.Any(static result => result.StudySet == "control" && result.Detected))
            throw new InvalidOperationException("A valid control was rejected by at least one policy.");

        var selectiveCleanFacts = results
            .Where(static result => result.Family == "clean-facts" && result.Policy == nameof(ExperimentPolicy.P2_SELECTIVE))
            .Average(static result => result.Telemetry.VerifierInvocationsTotal);
        var alwaysCleanFacts = results
            .Where(static result => result.Family == "clean-facts" && result.Policy == nameof(ExperimentPolicy.P3_ALWAYS))
            .Average(static result => result.Telemetry.VerifierInvocationsTotal);
        if (alwaysCleanFacts <= selectiveCleanFacts)
            throw new InvalidOperationException("AlwaysVerify must invoke more semantic verifiers than Selective on clean fact boundaries.");

        var invalidationCases = new[] { "FACT-05", "FACT-06", "FACT-07", "FACT-08" };
        foreach (var caseId in invalidationCases)
        {
            var invalidationOnly = stable.Single(result => result.MutationId == caseId && result.Policy == nameof(ExperimentPolicy.P1_INVALIDATION));
            var selective = stable.Single(result => result.MutationId == caseId && result.Policy == nameof(ExperimentPolicy.P2_SELECTIVE));
            if (invalidationOnly.Detected || !selective.Detected)
                throw new InvalidOperationException($"Invalidation/selective distinction failed for {caseId}.");
        }
    }


}
