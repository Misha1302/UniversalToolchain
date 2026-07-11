using IdentifierModule.Contracts;
using ScopesModule.Contracts;
using UniversalToolchain.ModuleContracts;

namespace VariablesModule.Contracts;

public sealed class VariablesModuleContractDescriptorProvider : IModuleContractDescriptorProvider
{
    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        new SyntaxContractFacet(
            VariablesContractIds.Module,
            [
                new LexemeContract("Let", "Variable declaration keyword registered by VariablesModuleImpl."),
                new LexemeContract("Colon", "Typed variable declaration separator shared with label syntax.")
            ],
            [
                new ParserNodeContract(
                    VariablesContractIds.VariableNode,
                    -1.5d,
                    [new AstNodeKind("core.ast.identifier")])
            ]),
        new AstContractFacet(
            VariablesContractIds.Module,
            [
                new AstOwnershipContract(
                    VariablesContractIds.VariableNode,
                    AstOwnershipMode.Exclusive,
                    VariablesContractIds.Module,
                    [])
            ]),
        new BytecodeContractFacet(
            VariablesContractIds.Module,
            [
                new BytecodeEmissionContract(
                    VariablesContractIds.VariableNode,
                    [VariablesContractIds.WriteTargetTypeInference],
                    [
                        VariablesContractIds.LocalRead,
                        VariablesContractIds.ExternalRead,
                        VariablesContractIds.WriteTypeInference,
                        VariablesContractIds.DefineArgument
                    ],
                    StackEffect.Unknown,
                    SideEffectPolicy.ReadsAndWritesState)
            ]),
        new AirContractFacet(
            VariablesContractIds.Module,
            [
                new AirEmissionContract(
                    VariablesContractIds.LocalRead,
                    [KnownCoreAirPatterns.UniversalCall],
                    [KnownCoreIntrinsicSymbols.CallCSharp],
                    [
                        KnownCoreBackendCapabilities.UniversalCall,
                        KnownCoreBackendCapabilities.LocalVariables,
                        KnownCoreBackendCapabilities.MutableState
                    ]),
                new AirEmissionContract(
                    VariablesContractIds.ExternalRead,
                    [KnownCoreAirPatterns.UniversalCall],
                    [KnownCoreIntrinsicSymbols.CallCSharp],
                    [
                        KnownCoreBackendCapabilities.UniversalCall,
                        KnownCoreBackendCapabilities.ExternalBindings,
                        KnownCoreBackendCapabilities.MutableState
                    ])
            ]),
        new CompilerFactOwnershipFacet(
            VariablesContractIds.Module,
            [
                new CompilerFactOwnershipContract(
                    VariablesFacts.LocalsDeclared,
                    VariablesContractIds.Module),
                new CompilerFactOwnershipContract(
                    VariablesFacts.ExternalBindingsReferenced,
                    VariablesContractIds.Module)
            ]),
        new PipelineEffectFacet(
            VariablesContractIds.Module,
            [
                new PipelineEffectContract(
                    VariablesEffects.LowerVariableAccess,
                    CompilerPipelineStage.Bytecode,
                    [
                        IdentifierFacts.IdentifiersAvailable,
                        ScopesFacts.ScopesLocalsBound
                    ],
                    [
                        VariablesFacts.LocalsDeclared,
                        VariablesFacts.ExternalBindingsReferenced
                    ],
                    [],
                    [KnownCoreCompilerFacts.BytecodeVerified])
            ])
    ];
}
