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
{    private sealed record PerformanceSummary(
        int Samples,
        int IterationsPerSample,
        IReadOnlyDictionary<string, double> MedianTicksPerIteration,
        IReadOnlyDictionary<string, double> MedianOverheadPercent);

    private static PerformanceSummary MeasurePerformance()
    {
        const int samples = 33;
        const int iterations = 2_000;
        var measurements = Enum.GetNames<ExperimentPolicy>()
            .ToDictionary(static mode => mode, static _ => new List<double>());

        var module = new ModuleId("experiment.performance");
        var fact = new CompilerFactId("experiment.performance.fact");
        var capability = new BackendCapabilityId("experiment.performance.capability");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(module, [new CompilerFactOwnershipContract(fact, module)]))
            .AddFacet(new PipelineEffectFacet(module,
            [new PipelineEffectContract(new CompilerEffectId("experiment.performance.effect"), CompilerPipelineStage.Air, [], [fact], [], [])]))
            .AddFacet(new BackendCapabilityFacet(module, [new BackendCapabilityContract(capability, [])]))
            .Build();
        var selection = BackendCapabilitySelection.FromContracts(table, [capability]);
        var air = new AbstractIR();
        air.Push(1);
        var verifier = new AirVerifier();
        var effectVerifier = new PipelineEffectVerifier();
        var request = new AirVerificationRequest(air, table, selection, VerificationSeverityProfile.Strict);
        var effectRequest = new PipelineEffectValidationRequest(table, CompilerPipelineStage.Air, CompilerFactState.Empty, CompilerFactVerifierRegistry.Core, [module]);

        for (var warmup = 0; warmup < 1_000; warmup++)
        {
            _ = verifier.Verify(request);
            _ = effectVerifier.Validate(effectRequest);
        }

        var counterbalancedOrders = new[]
        {
            new[] { ExperimentPolicy.P0_STRUCTURAL, ExperimentPolicy.P1_INVALIDATION, ExperimentPolicy.P2_SELECTIVE, ExperimentPolicy.P3_ALWAYS },
            new[] { ExperimentPolicy.P1_INVALIDATION, ExperimentPolicy.P2_SELECTIVE, ExperimentPolicy.P3_ALWAYS, ExperimentPolicy.P0_STRUCTURAL },
            new[] { ExperimentPolicy.P2_SELECTIVE, ExperimentPolicy.P3_ALWAYS, ExperimentPolicy.P0_STRUCTURAL, ExperimentPolicy.P1_INVALIDATION },
            new[] { ExperimentPolicy.P3_ALWAYS, ExperimentPolicy.P0_STRUCTURAL, ExperimentPolicy.P1_INVALIDATION, ExperimentPolicy.P2_SELECTIVE }
        };

        for (var sample = 0; sample < samples; sample++)
        {
            foreach (var mode in counterbalancedOrders[sample % counterbalancedOrders.Length])
            {
                var stopwatch = Stopwatch.StartNew();
                for (var iteration = 0; iteration < iterations; iteration++)
                {
                    var airResult = verifier.Verify(request);
                    if (!airResult.IsValid)
                        throw new InvalidOperationException("Clean AIR unexpectedly failed during performance measurement.");
                    if (mode == ExperimentPolicy.P0_STRUCTURAL)
                        continue;
                    var effectResult = effectVerifier.Validate(effectRequest);
                    if (effectResult.Diagnostics.Count != 0)
                        throw new InvalidOperationException("Clean effect validation unexpectedly failed during performance measurement.");
                    if (mode == ExperimentPolicy.P2_SELECTIVE && effectResult.ReverificationRequests.Count != 0)
                        throw new InvalidOperationException("Clean selective validation unexpectedly requested reverification.");
                    if (mode == ExperimentPolicy.P3_ALWAYS)
                    {
                        var alwaysResult = verifier.Verify(request);
                        if (!alwaysResult.IsValid)
                            throw new InvalidOperationException("Clean always-verify AIR validation unexpectedly failed.");
                    }
                }
                stopwatch.Stop();
                measurements[mode.ToString()].Add((double)stopwatch.ElapsedTicks / iterations);
            }
        }

        var medians = measurements.ToDictionary(
            static pair => pair.Key,
            static pair => Median(pair.Value));
        var baseline = medians[nameof(ExperimentPolicy.P0_STRUCTURAL)];
        var overhead = medians.ToDictionary(
            static pair => pair.Key,
            pair => pair.Key == nameof(ExperimentPolicy.P0_STRUCTURAL) ? 0.0 : (pair.Value / baseline - 1.0) * 100.0);
        return new PerformanceSummary(samples, iterations, medians, overhead);
    }

    private static double Median(IReadOnlyList<double> values)
    {
        var ordered = values.OrderBy(static value => value).ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[middle - 1] + ordered[middle]) / 2.0 : ordered[middle];
    }

    private static object BuildSummary(IReadOnlyList<ResultRecord> allResults, PerformanceSummary performance)
    {
        var stable = allResults
            .Where(static x => x.StudySet is "primary" or "challenge")
            .GroupBy(static x => (x.MutationId, x.Policy))
            .Select(static g => g.First())
            .ToArray();
        var controls = allResults.Where(static x => x.StudySet == "control").ToArray();

        static object SummarizeSet(IReadOnlyList<ResultRecord> rows)
        {
            var operatorRows = rows
                .GroupBy(static x => (x.OperatorId, x.Policy))
                .Select(static g => g.First())
                .ToArray();
            var byPolicy = operatorRows.GroupBy(static x => x.Policy).ToDictionary(
                static g => g.Key,
                static g => new
                {
                    Operators = g.Count(),
                    Detected = g.Count(static x => x.Detected),
                    Localized = g.Count(static x => x.Detected && x.DiagnosticCode != null)
                });
            var byFamily = operatorRows
                .GroupBy(static x => (x.Family, x.Policy))
                .OrderBy(static g => g.Key.Family)
                .ThenBy(static g => g.Key.Policy)
                .Select(static g => new
                {
                    g.Key.Family,
                    g.Key.Policy,
                    Operators = g.Count(),
                    Detected = g.Count(static x => x.Detected)
                })
                .ToArray();
            return new
            {
                InstanceCount = rows.Select(static x => x.MutationId).Distinct().Count(),
                OperatorCount = operatorRows.Select(static x => x.OperatorId).Distinct().Count(),
                FamilyCount = operatorRows.Select(static x => x.Family).Distinct().Count(),
                ByPolicy = byPolicy,
                ByFamily = byFamily
            };
        }

        var primary = stable.Where(static x => x.StudySet == "primary").ToArray();
        var challenge = stable.Where(static x => x.StudySet == "challenge").ToArray();
        var cleanByPolicy = controls.GroupBy(static x => x.Policy).ToDictionary(
            static g => g.Key,
            static g => new
            {
                Runs = g.Count(),
                FalsePositives = g.Count(static x => x.Detected),
                Families = g.Select(static x => x.Family).Distinct().Count()
            });
        var cleanByFamily = controls
            .GroupBy(static x => (x.Family, x.Policy))
            .OrderBy(static g => g.Key.Family)
            .ThenBy(static g => g.Key.Policy)
            .Select(static g => new
            {
                g.Key.Family,
                g.Key.Policy,
                Runs = g.Count(),
                FalsePositives = g.Count(static x => x.Detected)
            })
            .ToArray();

        return new
        {
            Repetitions,
            Primary = SummarizeSet(primary),
            Challenge = SummarizeSet(challenge),
            Clean = cleanByPolicy,
            CleanByFamily = cleanByFamily,
            Performance = performance
        };
    }

    private static string BuildMutationCatalog(IEnumerable<MutationCase> cases)
    {
        var lines = new List<string> { "study_set,mutation_id,operator_id,family,expected_diagnostic" };
        lines.AddRange(cases
            .OrderBy(static x => x.StudySet, StringComparer.Ordinal)
            .ThenBy(static x => x.Id, StringComparer.Ordinal)
            .Select(static x => $"{x.StudySet},{x.Id},{x.OperatorId},{x.Family},{x.ExpectedCode}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

}
