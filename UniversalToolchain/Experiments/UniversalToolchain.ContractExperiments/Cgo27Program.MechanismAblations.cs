using System.Diagnostics;
using System.Text.Json;
using BasicCore.Core;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.ContractExperiments;

internal enum ExperimentPolicy { P0_STRUCTURAL, P1_INVALIDATION, P2_SELECTIVE, P3_ALWAYS }

internal static class MechanismAblationProgram
{
    private sealed record MechanismProbe(
        bool Detected,
        string? DiagnosticCode,
        int VerifierInvocations,
        long ElapsedNanoseconds);

    private sealed record MechanismAblationRecord(
        string Id,
        string Mechanism,
        string Counterexample,
        string ExpectedDiagnosticCode,
        MechanismProbe FullProtocol,
        MechanismProbe AblatedProtocol,
        MechanismProbe FullControl,
        MechanismProbe AblatedControl,
        int LostDetections,
        int FalsePositiveDelta);

    private sealed record MechanismAblationSummary(
        string Status,
        string Commit,
        int Mechanisms,
        int Counterexamples,
        int LostDetections,
        int FalsePositiveDeltas,
        IReadOnlyList<MechanismAblationRecord> Results);

    public static int Main(string[] args)
    {
        var outputDirectory = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.GetFullPath("artifacts/cgo27-mechanism-ablations");
        Directory.CreateDirectory(outputDirectory);
        var commit = Environment.GetEnvironmentVariable("CGO27_EXPERIMENT_COMMIT")
                     ?? Environment.GetEnvironmentVariable("GITHUB_SHA")
                     ?? "local-uncommitted";
        var records = new[]
        {
            ProducerIdentityAblation(),
            SourceIdentityAblation(),
            SelectedOrderAblation(),
            CanonicalVerifierOwnershipAblation(),
            ConflictingRouteAblation(),
            FailClosedObligationAblation(),
            CapabilityContractAblation(),
            RepeatedOccurrenceAblation()
        };

        ValidateMechanismAblations(records);
        var summary = new MechanismAblationSummary(
            "VALIDATED",
            commit,
            records.Length,
            records.Length,
            records.Sum(static record => record.LostDetections),
            records.Sum(static record => record.FalsePositiveDelta),
            records);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(
            Path.Combine(outputDirectory, "mechanism-ablations.json"),
            JsonSerializer.Serialize(summary, options) + Environment.NewLine);
        using (var writer = new StreamWriter(Path.Combine(outputDirectory, "mechanism-ablations.jsonl"), false))
        {
            foreach (var record in records)
                writer.WriteLine(JsonSerializer.Serialize(record));
        }

        Console.WriteLine("CGO27_MECHANISM_ABLATIONS=" + JsonSerializer.Serialize(summary));
        return 0;
    }

    private static MechanismAblationRecord ProducerIdentityAblation()
    {
        var declaredModule = new ModuleId("ablation.producer.declared");
        var observedModule = new ModuleId("ablation.producer.observed");
        var node = new AstNodeKind("ablation.producer.node");
        var pattern = new BytecodePatternId("ablation.producer.pattern");
        var table = BytecodeTable(declaredModule, node, pattern);
        var counterexample = new ObservedBytecodeEmission(observedModule, node, [], [pattern]);
        var normalized = counterexample with { ProducerModule = declaredModule };
        var control = new ObservedBytecodeEmission(declaredModule, node, [], [pattern]);
        return Record(
            "M01",
            "producer identity",
            "An emission uses a globally declared pattern but attributes it to a module that did not declare it.",
            ModuleContractDiagnosticCodes.UndeclaredBytecodeProducer,
            () => VerifyBytecode(table, counterexample),
            () => VerifyBytecode(table, normalized),
            () => VerifyBytecode(table, control),
            () => VerifyBytecode(table, control));
    }

    private static MechanismAblationRecord SourceIdentityAblation()
    {
        var module = new ModuleId("ablation.source.module");
        var declaredNode = new AstNodeKind("ablation.source.declared");
        var observedNode = new AstNodeKind("ablation.source.observed");
        var pattern = new BytecodePatternId("ablation.source.pattern");
        var table = BytecodeTable(module, declaredNode, pattern);
        var counterexample = new ObservedBytecodeEmission(module, observedNode, [], [pattern]);
        var normalized = counterexample with { SourceNode = declaredNode };
        var control = new ObservedBytecodeEmission(module, declaredNode, [], [pattern]);
        return Record(
            "M02",
            "source identity",
            "The producing module declared an emission, but only for a different source AST node kind.",
            ModuleContractDiagnosticCodes.UndeclaredBytecodeSource,
            () => VerifyBytecode(table, counterexample),
            () => VerifyBytecode(table, normalized),
            () => VerifyBytecode(table, control),
            () => VerifyBytecode(table, control));
    }

    private static MechanismAblationRecord SelectedOrderAblation()
    {
        var producer = new ModuleId("ablation.order.producer");
        var consumer = new ModuleId("ablation.order.consumer");
        var fact = new CompilerFactId("ablation.order.fact");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(
                producer,
                [new CompilerFactOwnershipContract(fact, producer)]))
            .AddFacet(new PipelineEffectFacet(
                producer,
                [new PipelineEffectContract(
                    new CompilerEffectId("ablation.order.produce"),
                    CompilerPipelineStage.Air,
                    [],
                    [fact],
                    [],
                    [])]))
            .AddFacet(new PipelineEffectFacet(
                consumer,
                [new PipelineEffectContract(
                    new CompilerEffectId("ablation.order.consume"),
                    CompilerPipelineStage.Air,
                    [fact],
                    [],
                    [],
                    [])]))
            .Build();
        return Record(
            "M03",
            "selected-order semantics",
            "The selected pipeline executes the consumer before the producer; sorting by module identity hides the violation.",
            ModuleContractDiagnosticCodes.MissingRequiredCompilerFact,
            () => VerifyPipeline(table, [consumer, producer]),
            () => VerifyPipeline(table, [producer, consumer]),
            () => VerifyPipeline(table, [producer, consumer]),
            () => VerifyPipeline(table, [producer, consumer]));
    }

    private static MechanismAblationRecord CanonicalVerifierOwnershipAblation()
    {
        var routes = new[]
        {
            new VerifierRouteDescriptor(KnownCoreVerifierRules.AirContract, "")
        };
        var requests = Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified);
        var normalized = new[]
        {
            new VerifierRouteDescriptor(KnownCoreVerifierRules.AirContract, "ablation.default-owner")
        };
        var control = new[]
        {
            new VerifierRouteDescriptor(KnownCoreVerifierRules.AirContract, "core.air")
        };
        return Record(
            "M04",
            "canonical verifier ownership",
            "A verifier route exists without a non-empty canonical owner.",
            "UT-SCHEDULER-OWNER-001",
            () => VerifySchedule(routes, requests),
            () => VerifySchedule(normalized, requests),
            () => VerifySchedule(control, requests),
            () => VerifySchedule(control, requests));
    }

    private static MechanismAblationRecord ConflictingRouteAblation()
    {
        var routes = new[]
        {
            new VerifierRouteDescriptor(KnownCoreVerifierRules.AirContract, "owner.a"),
            new VerifierRouteDescriptor(KnownCoreVerifierRules.AirContract, "owner.b")
        };
        var requests = Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified);
        var firstOwnerWins = routes
            .GroupBy(static route => route.RuleId)
            .Select(static group => group.First())
            .ToArray();
        var control = new[]
        {
            new VerifierRouteDescriptor(KnownCoreVerifierRules.AirContract, "owner.a")
        };
        return Record(
            "M05",
            "conflicting-route rejection",
            "Two different owners claim the same verifier rule; first-owner-wins masks the conflict.",
            "UT-SCHEDULER-OWNER-002",
            () => VerifySchedule(routes, requests),
            () => VerifySchedule(firstOwnerWins, requests),
            () => VerifySchedule(control, requests),
            () => VerifySchedule(control, requests));
    }

    private static MechanismAblationRecord FailClosedObligationAblation()
    {
        var unknownRule = new VerifierRuleId("ablation.unknown.verifier");
        var unknownFact = new CompilerFactId("ablation.unknown.fact");
        var routes = new[]
        {
            new VerifierRouteDescriptor(KnownCoreVerifierRules.AirContract, "core.air")
        };
        var requests = Requests(unknownRule, unknownFact);
        var droppedRequests = requests
            .Where(request => routes.Any(route => route.RuleId == request.RuleId))
            .ToArray();
        var control = Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified);
        return Record(
            "M06",
            "fail-closed unresolved obligations",
            "An invalidation creates an obligation for which no executable verifier route exists.",
            "UT-SCHEDULER-ROUTE-001",
            () => VerifySchedule(routes, requests),
            () => VerifySchedule(routes, droppedRequests),
            () => VerifySchedule(routes, control),
            () => VerifySchedule(routes, control));
    }

    private static MechanismAblationRecord CapabilityContractAblation()
    {
        var module = new ModuleId("ablation.capability.module");
        var selected = new BackendCapabilityId("ablation.capability.selected");
        var required = new BackendCapabilityId("ablation.capability.required");
        var intrinsic = new IntrinsicSymbolId("ablation.capability.intrinsic");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new AirContractFacet(
                module,
                [new AirEmissionContract(
                    new BytecodePatternId("ablation.capability.source"),
                    [new AirPatternId("ablation.capability.pattern")],
                    [intrinsic],
                    [required])]))
            .AddFacet(new BackendCapabilityFacet(
                module,
                [
                    new BackendCapabilityContract(selected, [intrinsic]),
                    new BackendCapabilityContract(required, [intrinsic])
                ]))
            .Build();
        var air = new AbstractIR();
        air.AppendInstructions([new Instruction(UOpCode.Intrinsic, [intrinsic.Value])]);
        var missingRequired = BackendCapabilitySelection.FromContracts(table, [selected]);
        var complete = BackendCapabilitySelection.FromContracts(table, [selected, required]);
        return Record(
            "M07",
            "capability contract",
            "The backend supports an intrinsic through one capability but omits another capability required by the emission contract.",
            ModuleContractDiagnosticCodes.MissingBackendCapability,
            () => VerifyAir(table, air, missingRequired),
            () => VerifyAir(table, air, complete),
            () => VerifyAir(table, air, complete),
            () => VerifyAir(table, air, complete));
    }

    private static MechanismAblationRecord RepeatedOccurrenceAblation()
    {
        var module = new ModuleId("ablation.occurrence.module");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new PipelineEffectFacet(
                module,
                [new PipelineEffectContract(
                    new CompilerEffectId("ablation.occurrence.effect"),
                    CompilerPipelineStage.Air,
                    [],
                    [],
                    [],
                    [])]))
            .Build();
        return Record(
            "M08",
            "repeated-occurrence rejection",
            "The same module appears twice in a module-level pipeline order whose effects cannot distinguish occurrences.",
            ModuleContractDiagnosticCodes.DuplicatePipelineModuleOccurrence,
            () => VerifyPipeline(table, [module, module]),
            () => VerifyPipeline(table, [module]),
            () => VerifyPipeline(table, [module]),
            () => VerifyPipeline(table, [module]));
    }

    private static MechanismAblationRecord Record(
        string id,
        string mechanism,
        string counterexample,
        string expectedDiagnosticCode,
        Func<MechanismProbe> fullProtocol,
        Func<MechanismProbe> ablatedProtocol,
        Func<MechanismProbe> fullControl,
        Func<MechanismProbe> ablatedControl)
    {
        var full = fullProtocol();
        var ablated = ablatedProtocol();
        var controlFull = fullControl();
        var controlAblated = ablatedControl();
        return new MechanismAblationRecord(
            id,
            mechanism,
            counterexample,
            expectedDiagnosticCode,
            full,
            ablated,
            controlFull,
            controlAblated,
            full.Detected && !ablated.Detected ? 1 : 0,
            Convert.ToInt32(controlAblated.Detected) - Convert.ToInt32(controlFull.Detected));
    }

    private static MechanismProbe VerifyBytecode(
        SelectedModuleContractTable table,
        ObservedBytecodeEmission emission) =>
        Measure(() =>
        {
            var result = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
                new Bytecode([]),
                table,
                VerificationSeverityProfile.Strict,
                [emission]));
            return (!result.IsValid, result.Diagnostics.FirstOrDefault()?.Code, 1);
        });

    private static MechanismProbe VerifyPipeline(
        SelectedModuleContractTable table,
        IReadOnlyList<ModuleId> order) =>
        Measure(() =>
        {
            var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
                table,
                CompilerPipelineStage.Air,
                CompilerFactState.Empty,
                CompilerFactVerifierRegistry.Core,
                order));
            var diagnostic = result.Diagnostics.FirstOrDefault();
            return (diagnostic != null, diagnostic?.Code, 1);
        });

    private static MechanismProbe VerifySchedule(
        IReadOnlyList<VerifierRouteDescriptor> routes,
        IReadOnlyList<ReverificationRequest> requests) =>
        Measure(() =>
        {
            try
            {
                _ = VerificationPolicyScheduler.Schedule(ExperimentPolicy.P2_SELECTIVE, routes, requests);
                return (false, (string?)null, 1);
            }
            catch (InvalidOperationException exception)
            {
                var diagnostic = exception.Message.Contains("conflicting canonical owners", StringComparison.Ordinal)
                    ? "UT-SCHEDULER-OWNER-002"
                    : exception.Message.Contains("no canonical owner", StringComparison.Ordinal)
                        ? "UT-SCHEDULER-OWNER-001"
                        : "UT-SCHEDULER-ROUTE-001";
                return (true, diagnostic, 1);
            }
        });

    private static MechanismProbe VerifyAir(
        SelectedModuleContractTable table,
        AbstractIR air,
        BackendCapabilitySelection selection) =>
        Measure(() =>
        {
            var result = new AirVerifier().Verify(new AirVerificationRequest(
                air,
                table,
                selection,
                VerificationSeverityProfile.Strict,
                AirVerificationScope.Semantic));
            return (!result.IsValid, result.Diagnostics.FirstOrDefault()?.Code, 1);
        });

    private static MechanismProbe Measure(Func<(bool Detected, string? Diagnostic, int Invocations)> action)
    {
        var started = Stopwatch.GetTimestamp();
        var result = action();
        return new MechanismProbe(
            result.Detected,
            result.Diagnostic,
            result.Invocations,
            ToNanoseconds(Stopwatch.GetTimestamp() - started));
    }

    private static SelectedModuleContractTable BytecodeTable(
        ModuleId module,
        AstNodeKind sourceNode,
        BytecodePatternId pattern) =>
        new ModuleContractTableBuilder()
            .AddFacet(new BytecodeContractFacet(
                module,
                [new BytecodeEmissionContract(
                    sourceNode,
                    [],
                    [pattern],
                    StackEffect.Unknown,
                    SideEffectPolicy.Pure)]))
            .Build();

    private static IReadOnlyList<ReverificationRequest> Requests(
        VerifierRuleId rule,
        CompilerFactId fact) =>
        [new ReverificationRequest(rule, [fact])];

    private static long ToNanoseconds(long elapsedTicks) =>
        checked((long)Math.Round(elapsedTicks * (1_000_000_000d / Stopwatch.Frequency)));

    private static void ValidateMechanismAblations(IReadOnlyList<MechanismAblationRecord> records)
    {
        if (records.Count != 8 || records.Select(static record => record.Id).Distinct().Count() != 8)
            throw new InvalidOperationException("Expected exactly eight distinct mechanism ablations.");

        foreach (var record in records)
        {
            if (!record.FullProtocol.Detected)
                throw new InvalidOperationException($"Full protocol missed mechanism counterexample {record.Id}.");
            if (!StringComparer.Ordinal.Equals(record.FullProtocol.DiagnosticCode, record.ExpectedDiagnosticCode))
                throw new InvalidOperationException($"Unexpected full-protocol diagnostic for {record.Id}: {record.FullProtocol.DiagnosticCode}.");
            if (record.AblatedProtocol.Detected)
                throw new InvalidOperationException($"Ablated mechanism still detected counterexample {record.Id}; the ablation is not isolated.");
            if (record.FullControl.Detected || record.AblatedControl.Detected)
                throw new InvalidOperationException($"Valid control rejected for mechanism {record.Id}.");
            if (record.LostDetections != 1 || record.FalsePositiveDelta != 0)
                throw new InvalidOperationException($"Mechanism ablation invariant failed for {record.Id}.");
        }
    }
}
