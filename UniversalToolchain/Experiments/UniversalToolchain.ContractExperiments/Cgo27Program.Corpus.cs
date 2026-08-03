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
{    private static void AddOwnershipCases(List<MutationCase> cases)
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


    private static void AddDemandBaselineCases(List<MutationCase> cases)
    {
        cases.Add(PipelineCase(
            "DEMAND-01",
            ModuleContractDiagnosticCodes.CompilerFactReverificationRequired,
            PipelineMutation.InvalidateAirVerified,
            studySet: "demand-v4",
            explicitDemand: true));
        cases.Add(PipelineCase(
            "DEMAND-02",
            ModuleContractDiagnosticCodes.CompilerFactReverificationRequired,
            PipelineMutation.InvalidateAirVerified,
            studySet: "demand-v4",
            explicitDemand: false));
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
        new(id, operatorId ?? id, studySet, family, expectedCode, mode => Timed(mode, "contract-table", () =>
        {
            if (mode == ExperimentPolicy.P0_STRUCTURAL)
                return (false, (string?)null);
            var diagnostic = InvokeVerifier(
                "core.verifier.module-contract-table",
                () => diagnosticsFactory().FirstOrDefault(x => x.Code == expectedCode));
            return (diagnostic != null, diagnostic?.Code);
        }));

    private static MutationCase AstOwnershipCase(string id, string expectedCode, bool duplicate, bool zero) =>
        new(id, id, "primary", "ownership", expectedCode, mode => Timed(mode, "ast-ownership", () =>
        {
            if (mode == ExperimentPolicy.P0_STRUCTURAL)
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
            var diagnostics = InvokeVerifier(
                "core.verifier.ast-ownership",
                () => AstOwnershipRegistry.FromTable(builder.Build()).ValidateNodeOwnership(node));
            var diagnostic = diagnostics.FirstOrDefault(x => x.Code == expectedCode);
            return (diagnostic != null, diagnostic?.Code);
        }));


}
