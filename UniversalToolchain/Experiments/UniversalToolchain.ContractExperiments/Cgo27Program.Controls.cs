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
{    private static IReadOnlyList<ResultRecord> RunCleanCorpus(string runId, string commit)
    {
        var records = new List<ResultRecord>();
        var families = new (string Name, Func<int, ExperimentPolicy, ExperimentOutcome> Execute)[]
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
                foreach (var mode in Enum.GetValues<ExperimentPolicy>())
                {
                    var outcome = family.Execute(sample, mode);
                    var caseId = $"CLEAN-{family.Name[6..].ToUpperInvariant()}-{sample:00}";
                    records.Add(CreateControlRecord(runId, commit, caseId, family.Name, mode, outcome));
                }
            }
        }

        return records;
    }

    private static ExperimentOutcome RunCleanOwnership(int sample, ExperimentPolicy mode) =>
        Timed(mode, "clean-ownership", () =>
        {
            if (mode == ExperimentPolicy.P0_STRUCTURAL)
                return (false, (string?)null);
            var module = new ModuleId($"experiment.clean.ownership.{sample}");
            var node = new AstNodeKind($"experiment.clean.ownership.{sample}.node");
            var table = new ModuleContractTableBuilder()
                .AddFacet(AstFacet(module, node.Value))
                .Build();
            var diagnostics = InvokeVerifier(
                "core.verifier.ast-ownership",
                () => AstOwnershipRegistry.FromTable(table)
                    .ValidateLowerer(new ChallengeLowerer(module, node)));
            return (diagnostics.Count != 0, diagnostics.FirstOrDefault()?.Code);
        });

    private static ExperimentOutcome RunCleanBytecode(int sample, ExperimentPolicy mode) =>
        Timed(mode, "clean-bytecode", () =>
        {
            if (mode == ExperimentPolicy.P0_STRUCTURAL)
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
            var metadata = InvokeVerifier(
                "core.verifier.bytecode-metadata",
                () => BytecodeContractMetadata.Validate(instruction));
            if (metadata.Count != 0)
                return (true, metadata[0].Code);
            var observed = new BytecodeObservedEmissionReader().Read(bytecode);
            var verification = InvokeVerifier(
                KnownCoreVerifierRules.BytecodeContract.Value,
                () => new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
                    bytecode,
                    table,
                    VerificationSeverityProfile.Strict,
                    observed)));
            return (verification.Diagnostics.Count != 0, verification.Diagnostics.FirstOrDefault()?.Code);
        });

    private static ExperimentOutcome RunCleanFacts(int sample, ExperimentPolicy mode) =>
        Timed(mode, "clean-facts", () =>
        {
            if (mode == ExperimentPolicy.P0_STRUCTURAL)
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
            var validation = InvokeVerifier(
                "core.verifier.pipeline-effects",
                () => new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
                    table,
                    CompilerPipelineStage.Air,
                    CompilerFactState.Empty,
                    CompilerFactVerifierRegistry.Core,
                    [module])));
            _activeTelemetry?.RecordPipeline(validation);
            if (validation.Diagnostics.Count != 0)
                return (true, validation.Diagnostics[0].Code);
            var scheduled = VerificationPolicyScheduler.Schedule(
                mode,
                AvailableRoutes(CompilerPipelineStage.Air),
                validation.ReverificationRequests);
            foreach (var invocation in scheduled)
            {
                var succeeded = ExecuteSemanticReverification(
                    invocation.RuleId,
                    $"clean-facts-{sample}",
                    PipelineMutation.MissingRequirement,
                    invalidArtifact: invocation.IsObligationDriven);
                _activeTelemetry?.RecordReverification(invocation.InvalidatedFacts.Count, succeeded);
                if (!succeeded)
                    return (true, ModuleContractDiagnosticCodes.CompilerFactReverificationRequired);
            }
            return (false, (string?)null);
        });

    private static ExperimentOutcome RunCleanAir(int sample, ExperimentPolicy mode) =>
        Timed(mode, "clean-air", () =>
        {
            var table = CleanCapabilityTable($"clean-air-{sample}");
            var selection = BackendCapabilitySelection.FromContracts(
                table,
                [new BackendCapabilityId($"experiment.clean-air-{sample}.cap")]);
            var air = new AbstractIR();
            air.Push(sample);
            var result = InvokeVerifier(
                KnownCoreVerifierRules.AirContract.Value,
                () => new AirVerifier().Verify(new AirVerificationRequest(
                    air,
                    table,
                    selection,
                    VerificationSeverityProfile.Strict)));
            return (result.Diagnostics.Count != 0, result.Diagnostics.FirstOrDefault()?.Code);
        });

    private static ExperimentOutcome RunCleanCapability(int sample, ExperimentPolicy mode) =>
        Timed(mode, "clean-capability", () =>
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
            var result = InvokeVerifier(
                KnownCoreVerifierRules.AirContract.Value,
                () => new AirVerifier().Verify(new AirVerificationRequest(
                    air,
                    table,
                    selection,
                    VerificationSeverityProfile.Strict)));
            return (result.Diagnostics.Count != 0, result.Diagnostics.FirstOrDefault()?.Code);
        });


}
