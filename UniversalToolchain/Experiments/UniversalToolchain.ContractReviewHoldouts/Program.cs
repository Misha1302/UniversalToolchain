using System.Diagnostics;
using System.Text.Json;
using BasicCore.Core;
using BasicCore.ParserWrapper;
using DynamicMethodWrapper;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.ContractReviewHoldouts;

internal enum HoldoutMode
{
    B0,
    B1,
    B2
}

internal sealed record HoldoutResult(
    string Commit,
    string CaseId,
    string Family,
    string Mode,
    int Repetition,
    bool IsControl,
    bool Detected,
    string? DiagnosticCode,
    long ElapsedTicks);

internal static class Program
{
    private const int Repetitions = 3;
    private const int ControlsPerMode = 20;

    public static int Main(string[] args)
    {
        var outputDirectory = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.GetFullPath("artifacts/contract-review-holdout");
        Directory.CreateDirectory(outputDirectory);

        var commit = Environment.GetEnvironmentVariable("GITHUB_SHA")
                     ?? Environment.GetEnvironmentVariable("CONTRACT_REVIEW_HOLDOUT_COMMIT")
                     ?? "local-uncommitted";
        var cases = new (string Id, string Family, Func<HoldoutMode, (bool Detected, string? Code)> Run)[]
        {
            ("RH-BYTECODE-MISSING-PRODUCER", "bytecode-identity", mode => RunMissingBytecodeIdentity(mode, missingProducer: true)),
            ("RH-BYTECODE-MISSING-SOURCE", "bytecode-identity", mode => RunMissingBytecodeIdentity(mode, missingProducer: false)),
            ("RH-PIPELINE-DUPLICATE-OCCURRENCE", "pipeline-order", RunDuplicatePipelineOccurrence),
            ("RH-EXTERNAL-VERIFIER-ROUTE", "reverification-routing", RunExternalVerifierRoute)
        };

        var results = new List<HoldoutResult>();
        foreach (var testCase in cases)
        {
            foreach (var mode in Enum.GetValues<HoldoutMode>())
            {
                for (var repetition = 1; repetition <= Repetitions; repetition++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var outcome = testCase.Run(mode);
                    stopwatch.Stop();
                    results.Add(new HoldoutResult(
                        commit,
                        testCase.Id,
                        testCase.Family,
                        mode.ToString(),
                        repetition,
                        IsControl: false,
                        outcome.Detected,
                        outcome.Code,
                        stopwatch.ElapsedTicks));
                }
            }
        }

        foreach (var mode in Enum.GetValues<HoldoutMode>())
        {
            for (var index = 1; index <= ControlsPerMode; index++)
            {
                var stopwatch = Stopwatch.StartNew();
                var detected = RunValidBytecodeControl(mode, index);
                stopwatch.Stop();
                results.Add(new HoldoutResult(
                    commit,
                    $"CONTROL-{index:00}",
                    "valid-control",
                    mode.ToString(),
                    Repetition: 1,
                    IsControl: true,
                    detected,
                    detected ? "unexpected-control-diagnostic" : null,
                    stopwatch.ElapsedTicks));
            }
        }

        ValidateMatrix(results, cases.Select(static item => item.Id).ToArray());
        var summary = BuildSummary(results, cases.Select(static item => item.Id).ToArray());
        using (var writer = new StreamWriter(Path.Combine(outputDirectory, "results.jsonl"), false))
        {
            foreach (var result in results)
                writer.WriteLine(JsonSerializer.Serialize(result));
        }

        File.WriteAllText(
            Path.Combine(outputDirectory, "summary.json"),
            JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(
            Path.Combine(outputDirectory, "environment.json"),
            JsonSerializer.Serialize(new
            {
                Commit = commit,
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                StopwatchFrequency = Stopwatch.Frequency,
                EvidenceClass = "post-freeze-review-derived-holdout",
                ExternalIndependenceClaimed = false
            }, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine("CONTRACT_REVIEW_HOLDOUT_SUMMARY=" + JsonSerializer.Serialize(summary));
        return 0;
    }

    private static (bool Detected, string? Code) RunMissingBytecodeIdentity(
        HoldoutMode mode,
        bool missingProducer)
    {
        if (mode == HoldoutMode.B0)
            return (false, null);

        var module = new ModuleId("holdout.bytecode.module");
        var node = new AstNodeKind("holdout.bytecode.node");
        var pattern = new BytecodePatternId("holdout.bytecode.pattern");
        var instruction = new BytecodeInstruction(new AbstractMethodImpl("holdout-bytecode", (_, _) => { }));
        if (!missingProducer)
            instruction.Tags.Add(BytecodeContractMetadata.ProducerModule(module));
        if (missingProducer)
            instruction.Tags.Add(BytecodeContractMetadata.SourceNode(node));
        instruction.Tags.Add(BytecodeContractMetadata.Pattern(pattern));

        var diagnostics = new BytecodeObservedEmissionReader()
            .ReadWithDiagnostics(new Bytecode([instruction]))
            .Diagnostics;
        var diagnostic = diagnostics.FirstOrDefault(static item =>
            item.Code == ModuleContractDiagnosticCodes.InvalidBytecodeContractMetadata);
        return (diagnostic != null, diagnostic?.Code);
    }

    private static (bool Detected, string? Code) RunDuplicatePipelineOccurrence(HoldoutMode mode)
    {
        if (mode == HoldoutMode.B0)
            return (false, null);

        var module = new ModuleId("holdout.pipeline.duplicate");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new PipelineEffectFacet(
                module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("holdout.pipeline.effect"),
                        CompilerPipelineStage.Air,
                        [],
                        [],
                        [],
                        [])
                ]))
            .Build();
        var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
            table,
            CompilerPipelineStage.Air,
            CompilerFactState.Empty,
            CompilerFactVerifierRegistry.Core,
            [module, module]));
        var diagnostic = result.Diagnostics.FirstOrDefault(static item =>
            item.Code == ModuleContractDiagnosticCodes.DuplicatePipelineModuleOccurrence);
        return (diagnostic != null, diagnostic?.Code);
    }

    private static (bool Detected, string? Code) RunExternalVerifierRoute(HoldoutMode mode)
    {
        if (mode == HoldoutMode.B0)
            return (false, null);

        var module = new ModuleId("holdout.external.module");
        var fact = new CompilerFactId("holdout.external.fact");
        var rule = new VerifierRuleId("holdout.external.verifier");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(
                module,
                [new CompilerFactOwnershipContract(fact, module)]))
            .AddFacet(new PipelineEffectFacet(
                module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("holdout.external.invalidate"),
                        CompilerPipelineStage.Air,
                        [],
                        [],
                        [],
                        [fact])
                ]))
            .Build();
        var registry = new CompilerFactVerifierRegistry(
            [
                new CoreCompilerFactVerifierRuleProvider(),
                new HoldoutVerifierProvider(fact, rule)
            ]);
        var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
            table,
            CompilerPipelineStage.Air,
            new CompilerFactState(
                new HashSet<CompilerFactId> { fact },
                new HashSet<CompilerFactId>()),
            registry,
            [module]));
        var routed = result.ReverificationRequests.Any(request =>
            request.RuleId == rule && request.InvalidatedFacts.Contains(fact));
        return (
            routed,
            routed ? ModuleContractDiagnosticCodes.CompilerFactReverificationRequired : null);
    }

    private static bool RunValidBytecodeControl(HoldoutMode mode, int index)
    {
        if (mode == HoldoutMode.B0)
            return false;

        var module = new ModuleId($"holdout.control.{index:00}");
        var node = new AstNodeKind($"holdout.control.{index:00}.node");
        var pattern = new BytecodePatternId($"holdout.control.{index:00}.pattern");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new BytecodeContractFacet(
                module,
                [
                    new BytecodeEmissionContract(
                        node,
                        [],
                        [pattern],
                        StackEffect.Unknown,
                        SideEffectPolicy.Pure)
                ]))
            .Build();
        var instruction = new BytecodeInstruction(new AbstractMethodImpl("holdout-control", (_, _) => { }))
            .WithContract(module, node, pattern);
        var bytecode = new Bytecode([instruction]);
        var read = new BytecodeObservedEmissionReader().ReadWithDiagnostics(bytecode);
        var result = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
            bytecode,
            table,
            VerificationSeverityProfile.Strict,
            read.ObservedEmissions));
        return read.Diagnostics.Count != 0 || !result.IsValid || result.Diagnostics.Count != 0;
    }

    private static void ValidateMatrix(
        IReadOnlyList<HoldoutResult> results,
        IReadOnlyList<string> caseIds)
    {
        foreach (var caseId in caseIds)
        {
            foreach (var mode in Enum.GetValues<HoldoutMode>())
            {
                var rows = results
                    .Where(result => !result.IsControl && result.CaseId == caseId && result.Mode == mode.ToString())
                    .ToArray();
                if (rows.Length != Repetitions)
                    throw new InvalidOperationException($"Holdout '{caseId}' mode '{mode}' did not produce {Repetitions} repetitions.");
                var expected = mode != HoldoutMode.B0;
                if (rows.Any(result => result.Detected != expected))
                    throw new InvalidOperationException($"Holdout '{caseId}' mode '{mode}' violated the frozen expected matrix.");
            }
        }

        var falsePositives = results.Count(static result => result.IsControl && result.Detected);
        if (falsePositives != 0)
            throw new InvalidOperationException($"Valid controls produced {falsePositives} false positives.");
    }

    private static object BuildSummary(
        IReadOnlyList<HoldoutResult> results,
        IReadOnlyList<string> caseIds) =>
        new
        {
            EvidenceClass = "post-freeze-review-derived-holdout",
            ExternalIndependenceClaimed = false,
            Limitation = "Cases were derived from a later review after the original primary/challenge corpus freeze; they are not an externally authored unseen-fault sample.",
            HoldoutOperators = caseIds.Count,
            Repetitions,
            ControlsPerMode,
            Modes = Enum.GetValues<HoldoutMode>().ToDictionary(
                static mode => mode.ToString(),
                mode => new
                {
                    DetectedOperators = results
                        .Where(result => !result.IsControl && result.Mode == mode.ToString() && result.Detected)
                        .Select(static result => result.CaseId)
                        .Distinct(StringComparer.Ordinal)
                        .Count(),
                    TotalOperators = caseIds.Count,
                    ControlFalsePositives = results.Count(result => result.IsControl && result.Mode == mode.ToString() && result.Detected),
                    Controls = results.Count(result => result.IsControl && result.Mode == mode.ToString())
                })
        };

    private sealed class HoldoutVerifierProvider(
        CompilerFactId fact,
        VerifierRuleId rule) : ICompilerFactVerifierRuleProvider
    {
        public IReadOnlyDictionary<CompilerFactId, VerifierRuleId> GetRules() =>
            new Dictionary<CompilerFactId, VerifierRuleId> { [fact] = rule };
    }
}
