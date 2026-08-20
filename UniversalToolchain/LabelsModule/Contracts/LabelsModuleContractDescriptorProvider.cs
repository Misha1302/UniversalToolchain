using UniversalToolchain.ModuleContracts;

namespace LabelsModule.Contracts;

public sealed class LabelsModuleContractDescriptorProvider : IModuleContractDescriptorProvider
{
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => [ContractNamespaceOwner.Reserved("wist", "wist")];

    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        new SyntaxContractFacet(
            LabelsContractIds.Module,
            [
                new LexemeContract("Colon", "Label separator registered by LabelsModuleImpl."),
                new LexemeContract("Goto", "Goto keyword registered by LabelsModuleImpl.")
            ],
            [
                new ParserNodeContract(
                    LabelsContractIds.LabelNode,
                    -2d,
                    [new AstNodeKind("core.ast.identifier")]),
                new ParserNodeContract(
                    LabelsContractIds.GotoNode,
                    -2d,
                    [new AstNodeKind("core.ast.identifier")])
            ]),
        new AstContractFacet(
            LabelsContractIds.Module,
            [
                new AstOwnershipContract(
                    LabelsContractIds.LabelNode,
                    AstOwnershipMode.Exclusive,
                    LabelsContractIds.Module,
                    []),
                new AstOwnershipContract(
                    LabelsContractIds.GotoNode,
                    AstOwnershipMode.Exclusive,
                    LabelsContractIds.Module,
                    [])
            ]),
        new BytecodeContractFacet(
            LabelsContractIds.Module,
            [
                new BytecodeEmissionContract(
                    LabelsContractIds.LabelNode,
                    [],
                    [LabelsContractIds.Label],
                    new StackEffect(0, 0),
                    SideEffectPolicy.ControlFlow),
                new BytecodeEmissionContract(
                    LabelsContractIds.GotoNode,
                    [],
                    [LabelsContractIds.Goto],
                    new StackEffect(0, 0),
                    SideEffectPolicy.ControlFlow)
            ]),
        new AirContractFacet(
            LabelsContractIds.Module,
            [
                new AirEmissionContract(
                    LabelsContractIds.Label,
                    [KnownCoreAirPatterns.Label],
                    [],
                    []),
                new AirEmissionContract(
                    LabelsContractIds.Goto,
                    [KnownCoreAirPatterns.Jump],
                    [],
                    [KnownCoreBackendCapabilities.UnconditionalBranches])
            ]),
        new CompilerFactOwnershipFacet(
            LabelsContractIds.Module,
            [
                new CompilerFactOwnershipContract(
                    LabelsFacts.LabelsDeclared,
                    LabelsContractIds.Module),
                new CompilerFactOwnershipContract(
                    LabelsFacts.GotosResolved,
                    LabelsContractIds.Module)
            ]),
        new PipelineEffectFacet(
            LabelsContractIds.Module,
            [
                new PipelineEffectContract(
                    LabelsEffects.LowerLabelControlFlow,
                    CompilerPipelineStage.Air,
                    [],
                    [
                        LabelsFacts.LabelsDeclared,
                        LabelsFacts.GotosResolved
                    ],
                    [],
                    [
                        KnownCoreCompilerFacts.AirVerified,
                        KnownCoreCompilerFacts.AirStackBalanced,
                        KnownCoreCompilerFacts.AirBranchStackCompatible
                    ])
            ])
    ];
}
