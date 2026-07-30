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

internal enum ExperimentMode { B0, B1, B2 }

internal sealed record MutationCase(
    string Id,
    string OperatorId,
    string StudySet,
    string Family,
    string ExpectedCode,
    Func<ExperimentMode, ExperimentOutcome> Execute);

internal sealed record ExperimentOutcome(
    bool Detected,
    string? DiagnosticCode,
    string Boundary,
    long ElapsedTicks);

internal sealed record ResultRecord(
    string Commit,
    string MutationId,
    string OperatorId,
    string StudySet,
    string Family,
    string Mode,
    int Repetition,
    bool Detected,
    string? DiagnosticCode,
    string Boundary,
    long ElapsedTicks);

internal static class Program
{
    private const int Repetitions = 3;

    public static int Main(string[] args)
    {
        var outputDirectory = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath("artifacts/contract-experiment");
        Directory.CreateDirectory(outputDirectory);

        var commit = Environment.GetEnvironmentVariable("GITHUB_SHA")
                     ?? Environment.GetEnvironmentVariable("ICSE_EXPERIMENT_COMMIT")
                     ?? "local-uncommitted";
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
            foreach (var mode in Enum.GetValues<ExperimentMode>())
            {
                for (var repetition = 1; repetition <= Repetitions; repetition++)
                {
                    var outcome = mutation.Execute(mode);
                    results.Add(new ResultRecord(
                        commit,
                        mutation.Id,
                        mutation.OperatorId,
                        mutation.StudySet,
                        mutation.Family,
                        mode.ToString(),
                        repetition,
                        outcome.Detected,
                        outcome.DiagnosticCode,
                        outcome.Boundary,
                        outcome.ElapsedTicks));
                }
            }
        }

        ValidateTriplets(results, cases);
        var clean = RunCleanCorpus(commit);
        results.AddRange(clean);

        var jsonlPath = Path.Combine(outputDirectory, "results.jsonl");
        using (var writer = new StreamWriter(jsonlPath, false))
        {
            foreach (var result in results)
                writer.WriteLine(JsonSerializer.Serialize(result));
        }

        var performance = MeasurePerformance();
        var summary = BuildSummary(results, performance);
        var summaryPath = Path.Combine(outputDirectory, "summary.json");
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(outputDirectory, "mutations.csv"), BuildMutationCatalog(cases));
        File.WriteAllText(Path.Combine(outputDirectory, "environment.json"), JsonSerializer.Serialize(new
        {
            Commit = commit,
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            StopwatchFrequency = Stopwatch.Frequency
        }, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine("ICSE_CONTRACT_EXPERIMENT_SUMMARY=" + JsonSerializer.Serialize(summary));
        Console.WriteLine($"Raw records: {jsonlPath}");
        return 0;
    }

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

    private static void AddOwnershipCases(List<MutationCase> cases)
    {
        cases.Add(TableCase("OWN-01", "ownership", ModuleContractDiagnosticCodes.DuplicateFacet, static () =>
        {
            var m = new ModuleId("experiment.own.01");
            return new ModuleContractTableBuilder().AddFacet(AstFacet(m, "node.a")).AddFacet(AstFacet(m, "node.b")).Build().Diagnostics;
        }));
        cases.Add(TableCase("OWN-02", "ownership", ModuleContractDiagnosticCodes.DuplicateFacet, static () =>
        {
            var m = new ModuleId("experiment.own.02");
            return new ModuleContractTableBuilder().AddFacet(SyntaxFacet(m, "a")).AddFacet(SyntaxFacet(m, "b")).Build().Diagnostics;
        }));
        cases.Add(TableCase("OWN-03", "ownership", ModuleContractDiagnosticCodes.DuplicateCompilerFactOwner, static () =>
        {
            var fact = new CompilerFactId("experiment.fact.own03");
            var a = new ModuleId("experiment.own.03.a");
            var b = new ModuleId("experiment.own.03.b");
            return new ModuleContractTableBuilder()
                .AddFacet(new CompilerFactOwnershipFacet(a, [new CompilerFactOwnershipContract(fact, a)]))
                .AddFacet(new CompilerFactOwnershipFacet(b, [new CompilerFactOwnershipContract(fact, b)]))
                .Build().Diagnostics;
        }));
        cases.Add(TableCase("OWN-04", "ownership", ModuleContractDiagnosticCodes.ForeignCompilerFactProduction, static () =>
        {
            var fact = new CompilerFactId("experiment.fact.own04");
            var declaring = new ModuleId("experiment.own.04.declaring");
            var owner = new ModuleId("experiment.own.04.owner");
            return new ModuleContractTableBuilder()
                .AddFacet(new CompilerFactOwnershipFacet(declaring, [new CompilerFactOwnershipContract(fact, owner)]))
                .Build().Diagnostics;
        }));
        cases.Add(AstOwnershipCase("OWN-05", ModuleContractDiagnosticCodes.ZeroAstOwner, duplicate: false, zero: true));
        cases.Add(AstOwnershipCase("OWN-06", ModuleContractDiagnosticCodes.MultipleAstOwners, duplicate: true, zero: false));
        cases.Add(TableCase("OWN-07", "ownership", ModuleContractDiagnosticCodes.InvalidPipelineEffect, static () =>
        {
            var m = new ModuleId("experiment.own.07");
            var fact = new CompilerFactId("experiment.fact.own07");
            return new ModuleContractTableBuilder()
                .AddFacet(new CompilerFactOwnershipFacet(m, [new CompilerFactOwnershipContract(fact, m)]))
                .AddFacet(new PipelineEffectFacet(m, [new PipelineEffectContract(new CompilerEffectId("experiment.effect.own07"), CompilerPipelineStage.Air, [], [fact], [], [fact])]))
                .Build().Diagnostics;
        }));
        cases.Add(TableCase("OWN-08", "ownership", ModuleContractDiagnosticCodes.InvalidPipelineEffect, static () =>
        {
            var m = new ModuleId("experiment.own.08");
            var fact = new CompilerFactId("experiment.fact.own08");
            return new ModuleContractTableBuilder()
                .AddFacet(new CompilerFactOwnershipFacet(m, [new CompilerFactOwnershipContract(fact, m)]))
                .AddFacet(new PipelineEffectFacet(m, [new PipelineEffectContract(new CompilerEffectId("experiment.effect.own08"), CompilerPipelineStage.Air, [], [], [fact], [fact])]))
                .Build().Diagnostics;
        }));
    }

    private static void AddBytecodeCases(List<MutationCase> cases)
    {
        for (var index = 1; index <= 8; index++)
        {
            var id = $"BYTE-{index:00}";
            var variant = index;
            var expected = variant switch
            {
                1 or 2 => ModuleContractDiagnosticCodes.UnknownBytecodeTag,
                3 or 4 => ModuleContractDiagnosticCodes.UnknownBytecodePattern,
                5 or 6 => ModuleContractDiagnosticCodes.UndeclaredBytecodeProducer,
                _ => ModuleContractDiagnosticCodes.BytecodeStackEffectMismatch
            };
            var operatorId = $"BYTE-{(index + 1) / 2:00}";
            cases.Add(new MutationCase(id, operatorId, "primary", "bytecode-drift", expected, mode => ExecuteBytecodeMutation(mode, id, variant, expected)));
        }
    }

    private static void AddFactCases(List<MutationCase> cases)
    {
        cases.Add(PipelineCase("FACT-01", ModuleContractDiagnosticCodes.MissingRequiredCompilerFact, PipelineMutation.MissingRequirement));
        cases.Add(PipelineCase("FACT-02", ModuleContractDiagnosticCodes.MissingRequiredCompilerFact, PipelineMutation.ConsumerBeforeProducer));
        cases.Add(PipelineCase("FACT-03", ModuleContractDiagnosticCodes.MissingRequiredCompilerFact, PipelineMutation.PreserveUnavailable));
        cases.Add(PipelineCase("FACT-04", ModuleContractDiagnosticCodes.MissingPipelineOrder, PipelineMutation.MissingOrder));
        cases.Add(PipelineCase("FACT-05", ModuleContractDiagnosticCodes.CompilerFactReverificationRequired, PipelineMutation.InvalidateAirVerified));
        cases.Add(PipelineCase("FACT-06", ModuleContractDiagnosticCodes.CompilerFactReverificationRequired, PipelineMutation.InvalidateBytecodeVerified));
        cases.Add(PipelineCase("FACT-07", ModuleContractDiagnosticCodes.CompilerFactReverificationRequired, PipelineMutation.InvalidateAirStackBalanced));
        cases.Add(PipelineCase("FACT-08", ModuleContractDiagnosticCodes.CompilerFactReverificationRequired, PipelineMutation.InvalidateAirIntrinsics));
    }

    private static void AddAirStructureCases(List<MutationCase> cases)
    {
        cases.Add(AirCase("AIR-01", "air-structure", ModuleContractDiagnosticCodes.InvalidAirOperandSchema, static air => air.AppendInstructions([new Instruction(UOpCode.Jmp, ["bad-target"])])));
        cases.Add(AirCase("AIR-02", "air-structure", ModuleContractDiagnosticCodes.MissingAirBranchTarget, static air => air.Jmp(Guid.NewGuid())));
        cases.Add(AirCase("AIR-03", "air-structure", ModuleContractDiagnosticCodes.DuplicateAirLabel, static air => { var id = Guid.NewGuid(); air.AppendInstructions([new Instruction(UOpCode.Label, [id]), new Instruction(UOpCode.Label, [id])]); }));
        cases.Add(AirCase("AIR-04", "air-structure", ModuleContractDiagnosticCodes.InvalidAirStackDiscipline, static air => air.AppendInstructions([new Instruction(UOpCode.Drop)])));
        cases.Add(AirCase("AIR-05", "air-structure", ModuleContractDiagnosticCodes.InvalidAirStackDiscipline, static air => { var id = Guid.NewGuid(); air.Push(1); air.JmpIf(id); air.AppendInstructions([new Instruction(UOpCode.Label, [id])]); }));
        cases.Add(AirCase("AIR-06", "air-structure", ModuleContractDiagnosticCodes.InvalidAirStackDiscipline, static air => { air.Push(1); air.Push(2); }));
        cases.Add(AirCase("AIR-07", "air-structure", ModuleContractDiagnosticCodes.InvalidAirOperandSchema, static air => air.AppendInstructions([new Instruction((UOpCode)255)])));
        cases.Add(AirCase("AIR-08", "air-structure", ModuleContractDiagnosticCodes.InvalidAirOperandSchema, static air => air.AppendInstructions([new Instruction(UOpCode.Push, [])])));
    }

    private static void AddCapabilityCases(List<MutationCase> cases)
    {
        for (var index = 1; index <= 8; index++)
        {
            var id = $"CAP-{index:00}";
            var variant = index;
            var expected = variant switch
            {
                1 or 2 => ModuleContractDiagnosticCodes.UnknownBackendCapability,
                3 or 4 => ModuleContractDiagnosticCodes.UnsupportedAirIntrinsic,
                5 or 6 => ModuleContractDiagnosticCodes.MissingBackendCapability,
                _ => ModuleContractDiagnosticCodes.InterpreterBackendIntrinsicViolation
            };
            var operatorId = $"CAP-{(index + 1) / 2:00}";
            cases.Add(new MutationCase(id, operatorId, "primary", "capability-target", expected, mode => ExecuteCapabilityMutation(mode, id, variant, expected)));
        }
    }

    private static MutationCase TableCase(
        string id,
        string family,
        string expectedCode,
        Func<IReadOnlyList<ToolchainDiagnostic>> diagnosticsFactory,
        string? operatorId = null,
        string studySet = "primary") =>
        new(id, operatorId ?? id, studySet, family, expectedCode, mode => Timed("contract-table", () =>
        {
            if (mode == ExperimentMode.B0)
                return (false, (string?)null);
            var diagnostic = diagnosticsFactory().FirstOrDefault(x => x.Code == expectedCode);
            return (diagnostic != null, diagnostic?.Code);
        }));

    private static MutationCase AstOwnershipCase(string id, string expectedCode, bool duplicate, bool zero) =>
        new(id, id, "primary", "ownership", expectedCode, mode => Timed("ast-ownership", () =>
        {
            if (mode == ExperimentMode.B0)
                return (false, (string?)null);
            var node = new AstNodeKind($"experiment.{id.ToLowerInvariant()}.node");
            var builder = new ModuleContractTableBuilder();
            if (!zero)
            {
                var a = new ModuleId($"experiment.{id.ToLowerInvariant()}.a");
                builder.AddFacet(AstFacet(a, node.Value));
                if (duplicate)
                {
                    var b = new ModuleId($"experiment.{id.ToLowerInvariant()}.b");
                    builder.AddFacet(AstFacet(b, node.Value));
                }
            }
            var diagnostics = AstOwnershipRegistry.FromTable(builder.Build()).ValidateNodeOwnership(node);
            var diagnostic = diagnostics.FirstOrDefault(x => x.Code == expectedCode);
            return (diagnostic != null, diagnostic?.Code);
        }));

    private static ExperimentOutcome ExecuteBytecodeMutation(ExperimentMode mode, string id, int variant, string expectedCode) =>
        Timed("bytecode", () =>
        {
            if (mode == ExperimentMode.B0)
                return (false, (string?)null);
            var module = new ModuleId($"experiment.{id.ToLowerInvariant()}");
            var node = new AstNodeKind($"experiment.{id.ToLowerInvariant()}.node");
            var declaredPattern = new BytecodePatternId($"experiment.{id.ToLowerInvariant()}.declared-pattern");
            var declaredTag = new BytecodeTagId($"experiment.{id.ToLowerInvariant()}.declared-tag");
            var emittedPattern = variant is 3 or 4 or 5 or 6 ? new BytecodePatternId($"experiment.{id.ToLowerInvariant()}.other-pattern") : declaredPattern;
            var emittedTag = variant is 1 or 2 or 5 or 6 ? new BytecodeTagId($"experiment.{id.ToLowerInvariant()}.other-tag") : declaredTag;
            var declaredStack = variant >= 7 ? new StackEffect(1, 1) : StackEffect.Unknown;
            var table = new ModuleContractTableBuilder()
                .AddFacet(new BytecodeContractFacet(module,
                [new BytecodeEmissionContract(node, [declaredTag], [declaredPattern], declaredStack, SideEffectPolicy.Pure)]))
                .Build();
            var instruction = new BytecodeInstruction(new AbstractMethodImpl("experiment-op", (_, _) => { }))
                .WithContract(module, node, emittedPattern, emittedTag);
            var bytecode = new Bytecode([instruction]);
            var observed = new BytecodeObservedEmissionReader().Read(bytecode).ToArray();
            if (variant >= 7)
            {
                observed = [new ObservedBytecodeEmission(module, node, [emittedTag], [emittedPattern], new StackEffect(0, 1))];
            }
            var result = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(bytecode, table, VerificationSeverityProfile.Strict, observed));
            var diagnostic = result.Diagnostics.FirstOrDefault(x => x.Code == expectedCode);
            return (diagnostic != null, diagnostic?.Code);
        });

    private enum PipelineMutation
    {
        MissingRequirement,
        ConsumerBeforeProducer,
        PreserveUnavailable,
        MissingOrder,
        InvalidateAirVerified,
        InvalidateBytecodeVerified,
        InvalidateAirStackBalanced,
        InvalidateAirIntrinsics
    }

    private static MutationCase PipelineCase(string id, string expectedCode, PipelineMutation mutation) =>
        new(id, id, "primary", "facts-order-reverification", expectedCode, mode => Timed("pipeline-effects", () =>
        {
            if (mode == ExperimentMode.B0)
                return (false, (string?)null);
            var result = RunPipelineMutation(id, mutation);
            var diagnostic = result.Diagnostics.FirstOrDefault(x => x.Code == expectedCode);
            if (diagnostic != null)
                return (true, diagnostic.Code);
            if (mode == ExperimentMode.B2 && result.ReverificationRequests.Count > 0)
                return (true, ModuleContractDiagnosticCodes.CompilerFactReverificationRequired);
            return (false, (string?)null);
        }));

    private static PipelineEffectValidationResult RunPipelineMutation(string id, PipelineMutation mutation)
    {
        var owner = new ModuleId($"experiment.{id.ToLowerInvariant()}.owner");
        var consumer = new ModuleId($"experiment.{id.ToLowerInvariant()}.consumer");
        var fact = mutation switch
        {
            PipelineMutation.InvalidateAirVerified => KnownCoreCompilerFacts.AirVerified,
            PipelineMutation.InvalidateBytecodeVerified => KnownCoreCompilerFacts.BytecodeVerified,
            PipelineMutation.InvalidateAirStackBalanced => KnownCoreCompilerFacts.AirStackBalanced,
            PipelineMutation.InvalidateAirIntrinsics => KnownCoreCompilerFacts.AirIntrinsicsSupported,
            _ => new CompilerFactId($"experiment.{id.ToLowerInvariant()}.fact")
        };
        var stage = mutation == PipelineMutation.InvalidateBytecodeVerified ? CompilerPipelineStage.Bytecode : CompilerPipelineStage.Air;
        var builder = new ModuleContractTableBuilder();
        if (!fact.Value.StartsWith("core.", StringComparison.Ordinal))
            builder.AddFacet(new CompilerFactOwnershipFacet(owner, [new CompilerFactOwnershipContract(fact, owner)]));
        var effects = new List<(ModuleId Module, PipelineEffectContract Effect)>();
        switch (mutation)
        {
            case PipelineMutation.MissingRequirement:
                effects.Add((consumer, new PipelineEffectContract(new CompilerEffectId($"experiment.{id}.consume"), stage, [fact], [], [], [])));
                break;
            case PipelineMutation.ConsumerBeforeProducer:
                effects.Add((consumer, new PipelineEffectContract(new CompilerEffectId($"experiment.{id}.consume"), stage, [fact], [], [], [])));
                effects.Add((owner, new PipelineEffectContract(new CompilerEffectId($"experiment.{id}.produce"), stage, [], [fact], [], [])));
                break;
            case PipelineMutation.PreserveUnavailable:
                effects.Add((consumer, new PipelineEffectContract(new CompilerEffectId($"experiment.{id}.preserve"), stage, [], [], [fact], [])));
                break;
            case PipelineMutation.MissingOrder:
                effects.Add((owner, new PipelineEffectContract(new CompilerEffectId($"experiment.{id}.effect"), stage, [], [], [], [])));
                break;
            default:
                effects.Add((consumer, new PipelineEffectContract(new CompilerEffectId($"experiment.{id}.invalidate"), stage, [], [], [], [fact])));
                break;
        }
        foreach (var group in effects.GroupBy(static x => x.Module))
            builder.AddFacet(new PipelineEffectFacet(group.Key, group.Select(static x => x.Effect).ToArray()));
        var table = builder.Build();
        var initial = mutation is PipelineMutation.InvalidateAirVerified or PipelineMutation.InvalidateBytecodeVerified or PipelineMutation.InvalidateAirStackBalanced or PipelineMutation.InvalidateAirIntrinsics
            ? new CompilerFactState(new HashSet<CompilerFactId> { fact }, new HashSet<CompilerFactId>())
            : CompilerFactState.Empty;
        IReadOnlyList<ModuleId>? order = mutation switch
        {
            PipelineMutation.MissingOrder => null,
            PipelineMutation.ConsumerBeforeProducer => [consumer, owner],
            _ => effects.Select(static x => x.Module).Distinct().ToArray()
        };
        return new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(table, stage, initial, CompilerFactVerifierRegistry.Core, order));
    }

    private static MutationCase AirCase(string id, string family, string expectedCode, Action<AbstractIR> mutate) =>
        new(id, id, "primary", family, expectedCode, mode => Timed("air-structure", () =>
        {
            var table = CleanCapabilityTable(id);
            var selection = BackendCapabilitySelection.FromContracts(table, [new BackendCapabilityId($"experiment.{id.ToLowerInvariant()}.cap")]);
            var air = new AbstractIR();
            mutate(air);
            var result = new AirVerifier().Verify(new AirVerificationRequest(air, table, selection, VerificationSeverityProfile.Strict));
            var diagnostic = result.Diagnostics.FirstOrDefault(x => x.Code == expectedCode);
            return (diagnostic != null, diagnostic?.Code);
        }));

    private static ExperimentOutcome ExecuteCapabilityMutation(ExperimentMode mode, string id, int variant, string expectedCode) =>
        Timed("capability-target", () =>
        {
            var module = new ModuleId($"experiment.{id.ToLowerInvariant()}");
            var selectedCapability = new BackendCapabilityId($"experiment.{id.ToLowerInvariant()}.selected");
            var requiredCapability = new BackendCapabilityId($"experiment.{id.ToLowerInvariant()}.required");
            var intrinsic = new IntrinsicSymbolId($"experiment.{id.ToLowerInvariant()}.intrinsic");
            var table = new ModuleContractTableBuilder()
                .AddFacet(new AirContractFacet(module,
                [new AirEmissionContract(new BytecodePatternId($"experiment.{id}.source"), [new AirPatternId($"experiment.{id}.pattern")], [intrinsic], [requiredCapability])]))
                .AddFacet(new BackendCapabilityFacet(module,
                [new BackendCapabilityContract(selectedCapability, variant is 3 or 4 ? [] : [intrinsic]),
                 new BackendCapabilityContract(requiredCapability, [intrinsic])]))
                .Build();
            BackendCapabilitySelection selection = variant switch
            {
                1 or 2 => new BackendCapabilitySelection([new BackendCapabilityId($"experiment.{id}.unknown")], [intrinsic]),
                3 or 4 => new BackendCapabilitySelection([selectedCapability, requiredCapability], []),
                5 or 6 => new BackendCapabilitySelection([selectedCapability], [intrinsic]),
                _ => new BackendCapabilitySelection([selectedCapability, requiredCapability], [intrinsic], AirBackendPolicy.UniversalInterpreter)
            };
            var air = new AbstractIR();
            air.AppendInstructions([new Instruction(UOpCode.Intrinsic, [intrinsic.Value])]);
            var result = new AirVerifier().Verify(new AirVerificationRequest(air, table, selection, VerificationSeverityProfile.Strict));
            var diagnostic = result.Diagnostics.FirstOrDefault(x => x.Code == expectedCode);
            // B0 retains existing structural/target AIR verification; B1/B2 add module-level contracts.
            return (diagnostic != null, diagnostic?.Code);
        });


    private static void AddChallengeCases(List<MutationCase> cases)
    {
        cases.Add(TableCase(
            "CH-NS-01",
            "challenge-contract-selection",
            ModuleContractDiagnosticCodes.InvalidNamespaceOwnership,
            static () =>
            {
                var module = new ModuleId("wist.challenge.namespace");
                return new ModuleContractTableBuilder()
                    .AddFacet(new BackendCapabilityFacet(
                        module,
                        [new BackendCapabilityContract(new BackendCapabilityId("core.backend.challenge"), [])]))
                    .AddNamespaceOwners(module, [ContractNamespaceOwner.Wist])
                    .Build()
                    .Diagnostics;
            },
            studySet: "challenge"));

        cases.Add(TableCase(
            "CH-SCHEMA-01",
            "challenge-contract-selection",
            ModuleContractDiagnosticCodes.SchemaDowngrade,
            static () => new ModuleContractTableBuilder
                {
                    SupportedSchemaVersion = new ContractSchemaVersion(1, 0)
                }
                .AddFacet(new AstContractFacet(new ModuleId("core.challenge.schema"), [])
                {
                    SchemaVersion = new ContractSchemaVersion(2, 0)
                })
                .Build()
                .Diagnostics,
            studySet: "challenge"));

        cases.Add(ChallengeSelectionCase(
            "CH-SELECT-01",
            ModuleContractDiagnosticCodes.NewModuleMissingDescriptor,
            static () => ModuleContractEnforcementPolicy.EnforceNewModules([])));
        cases.Add(ChallengeSelectionCase(
            "CH-SELECT-02",
            ModuleContractDiagnosticCodes.DeclaredModuleMissingDescriptor,
            static () => ModuleContractEnforcementPolicy.EnforceNewModules(
                [new ModuleContractStatusDeclaration(
                    new ModuleId("challenge.selection"),
                    ModuleContractCompatibilityStatus.Declared)])));
        cases.Add(ChallengeSelectionCase(
            "CH-SELECT-03",
            ModuleContractDiagnosticCodes.UndeclaredModule,
            static () => ModuleContractEnforcementPolicy.EnforceNewModules(
                [new ModuleContractStatusDeclaration(
                    new ModuleId("challenge.selection"),
                    ModuleContractCompatibilityStatus.Undeclared)])));

        cases.Add(new MutationCase(
            "CH-LOWER-01",
            "CH-LOWER-01",
            "challenge",
            "challenge-ownership",
            ModuleContractDiagnosticCodes.LowererOwnershipMismatch,
            mode => Timed("challenge-lowerer", () =>
            {
                if (mode == ExperimentMode.B0)
                    return (false, (string?)null);
                var node = new AstNodeKind("challenge.lowerer.node");
                var owner = new ModuleId("challenge.lowerer.owner");
                var lowerer = new ChallengeLowerer(new ModuleId("challenge.lowerer.other"), node);
                var table = new ModuleContractTableBuilder()
                    .AddFacet(AstFacet(owner, node.Value))
                    .Build();
                var diagnostic = AstOwnershipRegistry.FromTable(table)
                    .ValidateLowerer(lowerer)
                    .FirstOrDefault(static x => x.Code == ModuleContractDiagnosticCodes.LowererOwnershipMismatch);
                return (diagnostic != null, diagnostic?.Code);
            })));

        cases.Add(TableCase(
            "CH-FACT-01",
            "challenge-facts",
            ModuleContractDiagnosticCodes.UnknownCompilerFact,
            static () =>
            {
                var module = new ModuleId("challenge.fact.module");
                var unknown = new CompilerFactId("challenge.fact.unknown");
                return new ModuleContractTableBuilder()
                    .AddFacet(new PipelineEffectFacet(
                        module,
                        [new PipelineEffectContract(
                            new CompilerEffectId("challenge.fact.effect"),
                            CompilerPipelineStage.Air,
                            [unknown],
                            [],
                            [],
                            [])]))
                    .Build()
                    .Diagnostics;
            },
            studySet: "challenge"));

        cases.Add(ChallengeMetadataCase("CH-META-01", BytecodeContractMetadata.ProducerModulePrefix));
        cases.Add(ChallengeMetadataCase("CH-META-02", BytecodeContractMetadata.SourceNodePrefix));

        cases.Add(new MutationCase(
            "CH-CAP-01",
            "CH-CAP-01",
            "challenge",
            "challenge-capability-selection",
            ModuleContractDiagnosticCodes.MultipleBackendCapabilityFacets,
            mode => Timed("challenge-capability-selection", () =>
            {
                var first = new ModuleId("backend.challenge.first");
                var second = new ModuleId("backend.challenge.second");
                var table = new ModuleContractTableBuilder()
                    .AddFacet(new BackendCapabilityFacet(
                        first,
                        [new BackendCapabilityContract(new BackendCapabilityId("backend.challenge.first.cap"), [])]))
                    .AddFacet(new BackendCapabilityFacet(
                        second,
                        [new BackendCapabilityContract(new BackendCapabilityId("backend.challenge.second.cap"), [])]))
                    .Build();
                try
                {
                    _ = new BackendCapabilitySelectionFactory(AirBackendPolicy.CapabilityGated)
                        .Create(table, []);
                    return (false, (string?)null);
                }
                catch (InvalidOperationException)
                {
                    return (true, ModuleContractDiagnosticCodes.MultipleBackendCapabilityFacets);
                }
            })));
    }

    private static MutationCase ChallengeSelectionCase(
        string id,
        string expectedCode,
        Func<ModuleContractEnforcementPolicy> policyFactory) =>
        new(
            id,
            id,
            "challenge",
            "challenge-contract-selection",
            expectedCode,
            mode => Timed("challenge-selection", () =>
            {
                if (mode == ExperimentMode.B0)
                    return (false, (string?)null);
                var module = new ModuleId("challenge.selection");
                var report = new ModuleContractSelectionBuilder().Build([module], [], policyFactory());
                var diagnostic = report.Diagnostics.FirstOrDefault(x => x.Code == expectedCode);
                return (diagnostic != null, diagnostic?.Code);
            }));

    private static MutationCase ChallengeMetadataCase(string id, string prefix) =>
        new(
            id,
            id,
            "challenge",
            "challenge-bytecode-metadata",
            ModuleContractDiagnosticCodes.InvalidBytecodeContractMetadata,
            mode => Timed("challenge-bytecode-metadata", () =>
            {
                if (mode == ExperimentMode.B0)
                    return (false, (string?)null);
                var instruction = new BytecodeInstruction(new AbstractMethodImpl("challenge-metadata", (_, _) => { }));
                instruction.Tags.Add(prefix + "first");
                instruction.Tags.Add(prefix + "second");
                var diagnostic = BytecodeContractMetadata.Validate(instruction)
                    .FirstOrDefault(static x => x.Code == ModuleContractDiagnosticCodes.InvalidBytecodeContractMetadata);
                return (diagnostic != null, diagnostic?.Code);
            }));

    private sealed class ChallengeLowerer(ModuleId moduleId, AstNodeKind nodeKind) : IAstNodeLowerer
    {
        public ModuleId ModuleId { get; } = moduleId;

        public AstNodeKind NodeKind { get; } = nodeKind;

        public LoweringResult Lower(AstNode node, AstNodeLoweringContext context) =>
            new(new Bytecode([]), []);
    }

    private static AstContractFacet AstFacet(ModuleId module, string node) =>
        new(module, [new AstOwnershipContract(new AstNodeKind(node), AstOwnershipMode.Exclusive, module, [])]);

    private static SyntaxContractFacet SyntaxFacet(ModuleId module, string lexeme) =>
        new(module, [new LexemeContract(lexeme, "experiment")], []);

    private static SelectedModuleContractTable CleanCapabilityTable(string id)
    {
        var module = new ModuleId($"experiment.{id.ToLowerInvariant()}");
        var capability = new BackendCapabilityId($"experiment.{id.ToLowerInvariant()}.cap");
        return new ModuleContractTableBuilder()
            .AddFacet(new BackendCapabilityFacet(module, [new BackendCapabilityContract(capability, [])]))
            .Build();
    }

    private static ExperimentOutcome Timed(string boundary, Func<(bool Detected, string? Code)> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = action();
        stopwatch.Stop();
        return new ExperimentOutcome(result.Detected, result.Code, boundary, stopwatch.ElapsedTicks);
    }

    private static void ValidateTriplets(IReadOnlyList<ResultRecord> results, IReadOnlyList<MutationCase> cases)
    {
        foreach (var mutation in cases)
        {
            foreach (var mode in Enum.GetNames<ExperimentMode>())
            {
                var group = results.Where(x => x.MutationId == mutation.Id && x.Mode == mode).ToArray();
                if (group.Length != Repetitions)
                    throw new InvalidOperationException($"Incomplete triplet for {mutation.Id}/{mode}.");
                if (group.Select(static x => (x.Detected, x.DiagnosticCode, x.Boundary)).Distinct().Count() != 1)
                    throw new InvalidOperationException($"Flaky classification for {mutation.Id}/{mode}.");
            }
        }

        foreach (var group in results
                     .Where(static x => x.StudySet is "primary" or "challenge")
                     .GroupBy(static x => (x.StudySet, x.OperatorId, x.Mode)))
        {
            if (group.Select(static x => x.Detected).Distinct().Count() != 1)
                throw new InvalidOperationException($"Operator shape {group.Key} has inconsistent instance classifications.");
        }
    }

    private static IReadOnlyList<ResultRecord> RunCleanCorpus(string commit)
    {
        var records = new List<ResultRecord>();
        var families = new (string Name, Func<int, ExperimentMode, ExperimentOutcome> Execute)[]
        {
            ("clean-ownership", RunCleanOwnership),
            ("clean-bytecode", RunCleanBytecode),
            ("clean-facts", RunCleanFacts),
            ("clean-air", RunCleanAir),
            ("clean-capability", RunCleanCapability)
        };

        foreach (var family in families)
        {
            for (var sample = 1; sample <= 20; sample++)
            {
                foreach (var mode in Enum.GetValues<ExperimentMode>())
                {
                    var outcome = family.Execute(sample, mode);
                    records.Add(new ResultRecord(
                        commit,
                        $"CLEAN-{family.Name[6..].ToUpperInvariant()}-{sample:00}",
                        $"CLEAN-{family.Name[6..].ToUpperInvariant()}-{sample:00}",
                        "control",
                        family.Name,
                        mode.ToString(),
                        1,
                        outcome.Detected,
                        outcome.DiagnosticCode,
                        outcome.Boundary,
                        outcome.ElapsedTicks));
                }
            }
        }

        return records;
    }

    private static ExperimentOutcome RunCleanOwnership(int sample, ExperimentMode mode) =>
        Timed("clean-ownership", () =>
        {
            if (mode == ExperimentMode.B0)
                return (false, (string?)null);
            var module = new ModuleId($"experiment.clean.ownership.{sample}");
            var node = new AstNodeKind($"experiment.clean.ownership.{sample}.node");
            var table = new ModuleContractTableBuilder()
                .AddFacet(AstFacet(module, node.Value))
                .Build();
            var diagnostics = AstOwnershipRegistry.FromTable(table)
                .ValidateLowerer(new ChallengeLowerer(module, node));
            return (diagnostics.Count != 0, diagnostics.FirstOrDefault()?.Code);
        });

    private static ExperimentOutcome RunCleanBytecode(int sample, ExperimentMode mode) =>
        Timed("clean-bytecode", () =>
        {
            if (mode == ExperimentMode.B0)
                return (false, (string?)null);
            var module = new ModuleId($"experiment.clean.bytecode.{sample}");
            var node = new AstNodeKind($"experiment.clean.bytecode.{sample}.node");
            var pattern = new BytecodePatternId($"experiment.clean.bytecode.{sample}.pattern");
            var tag = new BytecodeTagId($"experiment.clean.bytecode.{sample}.tag");
            var table = new ModuleContractTableBuilder()
                .AddFacet(new BytecodeContractFacet(
                    module,
                    [new BytecodeEmissionContract(node, [tag], [pattern], StackEffect.Unknown, SideEffectPolicy.Pure)]))
                .Build();
            var instruction = new BytecodeInstruction(new AbstractMethodImpl("clean-bytecode", (_, _) => { }))
                .WithContract(module, node, pattern, tag);
            var bytecode = new Bytecode([instruction]);
            var metadata = BytecodeContractMetadata.Validate(instruction);
            if (metadata.Count != 0)
                return (true, metadata[0].Code);
            var observed = new BytecodeObservedEmissionReader().Read(bytecode);
            var verification = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
                bytecode,
                table,
                VerificationSeverityProfile.Strict,
                observed));
            return (verification.Diagnostics.Count != 0, verification.Diagnostics.FirstOrDefault()?.Code);
        });

    private static ExperimentOutcome RunCleanFacts(int sample, ExperimentMode mode) =>
        Timed("clean-facts", () =>
        {
            if (mode == ExperimentMode.B0)
                return (false, (string?)null);
            var module = new ModuleId($"experiment.clean.facts.{sample}");
            var fact = new CompilerFactId($"experiment.clean.facts.{sample}.fact");
            var table = new ModuleContractTableBuilder()
                .AddFacet(new CompilerFactOwnershipFacet(module, [new CompilerFactOwnershipContract(fact, module)]))
                .AddFacet(new PipelineEffectFacet(
                    module,
                    [new PipelineEffectContract(
                        new CompilerEffectId($"experiment.clean.facts.{sample}.effect"),
                        CompilerPipelineStage.Air,
                        [],
                        [fact],
                        [],
                        [])]))
                .Build();
            if (table.Diagnostics.Count != 0)
                return (true, table.Diagnostics[0].Code);
            var validation = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
                table,
                CompilerPipelineStage.Air,
                CompilerFactState.Empty,
                CompilerFactVerifierRegistry.Core,
                [module]));
            var detected = validation.Diagnostics.Count != 0 ||
                           (mode == ExperimentMode.B2 && validation.ReverificationRequests.Count != 0);
            return (detected, validation.Diagnostics.FirstOrDefault()?.Code);
        });

    private static ExperimentOutcome RunCleanAir(int sample, ExperimentMode mode) =>
        Timed("clean-air", () =>
        {
            var table = CleanCapabilityTable($"clean-air-{sample}");
            var selection = BackendCapabilitySelection.FromContracts(
                table,
                [new BackendCapabilityId($"experiment.clean-air-{sample}.cap")]);
            var air = new AbstractIR();
            air.Push(sample);
            var result = new AirVerifier().Verify(new AirVerificationRequest(
                air,
                table,
                selection,
                VerificationSeverityProfile.Strict));
            return (result.Diagnostics.Count != 0, result.Diagnostics.FirstOrDefault()?.Code);
        });

    private static ExperimentOutcome RunCleanCapability(int sample, ExperimentMode mode) =>
        Timed("clean-capability", () =>
        {
            var module = new ModuleId($"experiment.clean.capability.{sample}");
            var capability = new BackendCapabilityId($"experiment.clean.capability.{sample}.cap");
            var intrinsic = new IntrinsicSymbolId("load_i32");
            var table = new ModuleContractTableBuilder()
                .AddFacet(new AirContractFacet(
                    module,
                    [new AirEmissionContract(
                        new BytecodePatternId($"experiment.clean.capability.{sample}.source"),
                        [new AirPatternId($"experiment.clean.capability.{sample}.pattern")],
                        [intrinsic],
                        [capability])]))
                .AddFacet(new BackendCapabilityFacet(
                    module,
                    [new BackendCapabilityContract(capability, [intrinsic])]))
                .Build();
            var selection = new BackendCapabilitySelection([capability], [intrinsic]);
            var air = new AbstractIR();
            air.AppendInstructions([
                new Instruction(
                    UOpCode.Intrinsic,
                    [IntrinsicInvocationFactory.ForCapability(intrinsic.Value, [sample])])
            ]);
            var result = new AirVerifier().Verify(new AirVerificationRequest(
                air,
                table,
                selection,
                VerificationSeverityProfile.Strict));
            return (result.Diagnostics.Count != 0, result.Diagnostics.FirstOrDefault()?.Code);
        });

    private sealed record PerformanceSummary(
        int Samples,
        int IterationsPerSample,
        IReadOnlyDictionary<string, double> MedianTicksPerIteration,
        IReadOnlyDictionary<string, double> MedianOverheadPercent);

    private static PerformanceSummary MeasurePerformance()
    {
        const int samples = 33;
        const int iterations = 2_000;
        var measurements = Enum.GetNames<ExperimentMode>()
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
            new[] { ExperimentMode.B0, ExperimentMode.B1, ExperimentMode.B2 },
            new[] { ExperimentMode.B1, ExperimentMode.B2, ExperimentMode.B0 },
            new[] { ExperimentMode.B2, ExperimentMode.B0, ExperimentMode.B1 }
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
                    if (mode == ExperimentMode.B0)
                        continue;
                    var effectResult = effectVerifier.Validate(effectRequest);
                    if (effectResult.Diagnostics.Count != 0)
                        throw new InvalidOperationException("Clean effect validation unexpectedly failed during performance measurement.");
                    if (mode == ExperimentMode.B2 && effectResult.ReverificationRequests.Count != 0)
                        throw new InvalidOperationException("Clean B2 validation unexpectedly requested reverification.");
                }
                stopwatch.Stop();
                measurements[mode.ToString()].Add((double)stopwatch.ElapsedTicks / iterations);
            }
        }

        var medians = measurements.ToDictionary(
            static pair => pair.Key,
            static pair => Median(pair.Value));
        var baseline = medians[nameof(ExperimentMode.B0)];
        var overhead = medians.ToDictionary(
            static pair => pair.Key,
            pair => pair.Key == nameof(ExperimentMode.B0) ? 0.0 : (pair.Value / baseline - 1.0) * 100.0);
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
            .GroupBy(static x => (x.MutationId, x.Mode))
            .Select(static g => g.First())
            .ToArray();
        var controls = allResults.Where(static x => x.StudySet == "control").ToArray();

        static object SummarizeSet(IReadOnlyList<ResultRecord> rows)
        {
            var operatorRows = rows
                .GroupBy(static x => (x.OperatorId, x.Mode))
                .Select(static g => g.First())
                .ToArray();
            var byMode = operatorRows.GroupBy(static x => x.Mode).ToDictionary(
                static g => g.Key,
                static g => new
                {
                    Operators = g.Count(),
                    Detected = g.Count(static x => x.Detected),
                    Localized = g.Count(static x => x.Detected && x.DiagnosticCode != null)
                });
            var byFamily = operatorRows
                .GroupBy(static x => (x.Family, x.Mode))
                .OrderBy(static g => g.Key.Family)
                .ThenBy(static g => g.Key.Mode)
                .Select(static g => new
                {
                    g.Key.Family,
                    g.Key.Mode,
                    Operators = g.Count(),
                    Detected = g.Count(static x => x.Detected)
                })
                .ToArray();
            return new
            {
                InstanceCount = rows.Select(static x => x.MutationId).Distinct().Count(),
                OperatorCount = operatorRows.Select(static x => x.OperatorId).Distinct().Count(),
                FamilyCount = operatorRows.Select(static x => x.Family).Distinct().Count(),
                ByMode = byMode,
                ByFamily = byFamily
            };
        }

        var primary = stable.Where(static x => x.StudySet == "primary").ToArray();
        var challenge = stable.Where(static x => x.StudySet == "challenge").ToArray();
        var cleanByMode = controls.GroupBy(static x => x.Mode).ToDictionary(
            static g => g.Key,
            static g => new
            {
                Runs = g.Count(),
                FalsePositives = g.Count(static x => x.Detected),
                Families = g.Select(static x => x.Family).Distinct().Count()
            });
        var cleanByFamily = controls
            .GroupBy(static x => (x.Family, x.Mode))
            .OrderBy(static g => g.Key.Family)
            .ThenBy(static g => g.Key.Mode)
            .Select(static g => new
            {
                g.Key.Family,
                g.Key.Mode,
                Runs = g.Count(),
                FalsePositives = g.Count(static x => x.Detected)
            })
            .ToArray();

        return new
        {
            Repetitions,
            Primary = SummarizeSet(primary),
            Challenge = SummarizeSet(challenge),
            Clean = cleanByMode,
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
