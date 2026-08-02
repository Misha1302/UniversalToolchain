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
{    private static MutationCase AirCase(string id, string family, string expectedCode, Action<AbstractIR> mutate) =>
        new(id, id, "primary", family, expectedCode, mode => Timed(mode, "air-structure", () =>
        {
            var table = CleanCapabilityTable(id);
            var selection = BackendCapabilitySelection.FromContracts(table, [new BackendCapabilityId($"experiment.{id.ToLowerInvariant()}.cap")]);
            var air = new AbstractIR();
            mutate(air);
            var result = InvokeVerifier(
                KnownCoreVerifierRules.AirContract.Value,
                () => new AirVerifier().Verify(new AirVerificationRequest(
                    air,
                    table,
                    selection,
                    VerificationSeverityProfile.Strict)));
            var diagnostic = result.Diagnostics.FirstOrDefault(x => x.Code == expectedCode);
            return (diagnostic != null, diagnostic?.Code);
        }));

    private static ExperimentOutcome ExecuteCapabilityMutation(ExperimentPolicy mode, string id, int variant, string expectedCode) =>
        Timed(mode, "capability-target", () =>
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
            var result = InvokeVerifier(
                KnownCoreVerifierRules.AirContract.Value,
                () => new AirVerifier().Verify(new AirVerificationRequest(
                    air,
                    table,
                    selection,
                    VerificationSeverityProfile.Strict)));
            var diagnostic = result.Diagnostics.FirstOrDefault(x => x.Code == expectedCode);
            // P0 retains existing structural/target AIR verification; later policies add semantic obligations.
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
            mode => Timed(mode, "challenge-lowerer", () =>
            {
                if (mode == ExperimentPolicy.P0_STRUCTURAL)
                    return (false, (string?)null);
                var node = new AstNodeKind("challenge.lowerer.node");
                var owner = new ModuleId("challenge.lowerer.owner");
                var lowerer = new ChallengeLowerer(new ModuleId("challenge.lowerer.other"), node);
                var table = new ModuleContractTableBuilder()
                    .AddFacet(AstFacet(owner, node.Value))
                    .Build();
                var diagnostic = InvokeVerifier(
                    "core.verifier.ast-ownership",
                    () => AstOwnershipRegistry.FromTable(table)
                        .ValidateLowerer(lowerer)
                        .FirstOrDefault(static x => x.Code == ModuleContractDiagnosticCodes.LowererOwnershipMismatch));
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
            mode => Timed(mode, "challenge-capability-selection", () =>
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
                    _ = InvokeVerifier(
                        "core.verifier.backend-capability-selection",
                        () => new BackendCapabilitySelectionFactory(AirBackendPolicy.CapabilityGated)
                            .Create(table, []));
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
            mode => Timed(mode, "challenge-selection", () =>
            {
                if (mode == ExperimentPolicy.P0_STRUCTURAL)
                    return (false, (string?)null);
                var module = new ModuleId("challenge.selection");
                var report = InvokeVerifier(
                    "core.verifier.module-selection",
                    () => new ModuleContractSelectionBuilder().Build([module], [], policyFactory()));
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
            mode => Timed(mode, "challenge-bytecode-metadata", () =>
            {
                if (mode == ExperimentPolicy.P0_STRUCTURAL)
                    return (false, (string?)null);
                var instruction = new BytecodeInstruction(new AbstractMethodImpl("challenge-metadata", (_, _) => { }));
                instruction.Tags.Add(prefix + "first");
                instruction.Tags.Add(prefix + "second");
                var diagnostic = InvokeVerifier(
                    "core.verifier.bytecode-metadata",
                    () => BytecodeContractMetadata.Validate(instruction)
                        .FirstOrDefault(static x => x.Code == ModuleContractDiagnosticCodes.InvalidBytecodeContractMetadata));
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


}
