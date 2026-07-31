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
{    private static ExperimentOutcome ExecuteBytecodeMutation(ExperimentPolicy mode, string id, int variant, string expectedCode) =>
        Timed(mode, "bytecode", () =>
        {
            if (mode == ExperimentPolicy.P0_STRUCTURAL)
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
            var result = InvokeVerifier(
                KnownCoreVerifierRules.BytecodeContract.Value,
                () => new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
                    bytecode,
                    table,
                    VerificationSeverityProfile.Strict,
                    observed)));
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

    private sealed record PipelineMutationExecution(
        PipelineEffectValidationResult Validation,
        CompilerPipelineStage Stage,
        PipelineMutation Mutation,
        string CaseId);

    private static MutationCase PipelineCase(string id, string expectedCode, PipelineMutation mutation) =>
        new(id, id, "primary", "facts-order-reverification", expectedCode, mode => Timed(mode, "pipeline-effects", () =>
        {
            if (mode == ExperimentPolicy.P0_STRUCTURAL)
                return (false, (string?)null);
            var execution = RunPipelineMutation(id, mutation);
            var result = execution.Validation;
            _activeTelemetry?.RecordPipeline(result);
            var diagnostic = result.Diagnostics.FirstOrDefault(x => x.Code == expectedCode);
            if (diagnostic != null)
                return (true, diagnostic.Code);

            if (mode == ExperimentPolicy.P1_INVALIDATION)
                return (false, (string?)null);

            if (result.ReverificationRequests.Count > 0)
            {
                foreach (var request in result.ReverificationRequests)
                {
                    var succeeded = ExecuteSemanticReverification(
                        request.RuleId,
                        execution.CaseId,
                        execution.Mutation,
                        invalidArtifact: true);
                    _activeTelemetry?.RecordReverification(request.InvalidatedFacts.Count, succeeded);
                    if (!succeeded)
                        return (true, ModuleContractDiagnosticCodes.CompilerFactReverificationRequired);
                }
            }
            else if (mode == ExperimentPolicy.P3_ALWAYS)
            {
                var rule = execution.Stage == CompilerPipelineStage.Bytecode
                    ? KnownCoreVerifierRules.BytecodeContract
                    : KnownCoreVerifierRules.AirContract;
                if (!ExecuteSemanticReverification(rule, execution.CaseId, execution.Mutation, invalidArtifact: false))
                    return (true, ModuleContractDiagnosticCodes.CompilerFactReverificationRequired);
            }

            return (false, (string?)null);
        }));

    private static PipelineMutationExecution RunPipelineMutation(string id, PipelineMutation mutation)
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
        var validation = InvokeVerifier(
            "core.verifier.pipeline-effects",
            () => new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
                table,
                stage,
                initial,
                CompilerFactVerifierRegistry.Core,
                order)));
        return new PipelineMutationExecution(validation, stage, mutation, id);
    }

    private static bool ExecuteSemanticReverification(
        VerifierRuleId ruleId,
        string caseId,
        PipelineMutation mutation,
        bool invalidArtifact)
    {
        if (ruleId == KnownCoreVerifierRules.BytecodeContract)
        {
            return InvokeVerifier(ruleId.Value, () =>
            {
                var module = new ModuleId($"experiment.reverify.{caseId.ToLowerInvariant()}.bytecode");
                var node = new AstNodeKind($"experiment.reverify.{caseId.ToLowerInvariant()}.node");
                var declaredPattern = new BytecodePatternId($"experiment.reverify.{caseId.ToLowerInvariant()}.declared");
                var emittedPattern = invalidArtifact
                    ? new BytecodePatternId($"experiment.reverify.{caseId.ToLowerInvariant()}.mutated")
                    : declaredPattern;
                var tag = new BytecodeTagId($"experiment.reverify.{caseId.ToLowerInvariant()}.tag");
                var table = new ModuleContractTableBuilder()
                    .AddFacet(new BytecodeContractFacet(
                        module,
                        [new BytecodeEmissionContract(node, [tag], [declaredPattern], StackEffect.Unknown, SideEffectPolicy.Pure)]))
                    .Build();
                var instruction = new BytecodeInstruction(new AbstractMethodImpl("reverification-bytecode", (_, _) => { }))
                    .WithContract(module, node, emittedPattern, tag);
                var bytecode = new Bytecode([instruction]);
                var observed = new BytecodeObservedEmissionReader().Read(bytecode);
                return new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
                    bytecode,
                    table,
                    VerificationSeverityProfile.Strict,
                    observed)).IsValid;
            });
        }

        if (ruleId == KnownCoreVerifierRules.AirContract)
        {
            return InvokeVerifier(ruleId.Value, () =>
            {
                var table = CleanCapabilityTable($"reverify-{caseId}");
                var capability = new BackendCapabilityId($"experiment.reverify-{caseId.ToLowerInvariant()}.cap");
                var selection = BackendCapabilitySelection.FromContracts(table, [capability]);
                var air = new AbstractIR();
                if (!invalidArtifact)
                {
                    air.Push(1);
                }
                else if (mutation == PipelineMutation.InvalidateAirIntrinsics)
                {
                    air.AppendInstructions([
                        new Instruction(UOpCode.Intrinsic, [$"experiment.reverify.{caseId.ToLowerInvariant()}.unsupported"])
                    ]);
                }
                else
                {
                    air.AppendInstructions([new Instruction(UOpCode.Drop)]);
                }
                return new AirVerifier().Verify(new AirVerificationRequest(
                    air,
                    table,
                    selection,
                    VerificationSeverityProfile.Strict)).IsValid;
            });
        }

        return false;
    }


}
