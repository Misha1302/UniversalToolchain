using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Wist.Contracts;

namespace IdentifierModule.Contracts;

public sealed class IdentifierModuleContractDescriptorProvider : IModuleContractDescriptorProvider
{
    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        new SyntaxContractFacet(
            IdentifierContractIds.Module,
            [
                new LexemeContract(
                    "Identifier",
                    "Identifier lexeme registered by IdentifierModuleImpl.")
            ],
            []),
        new CompilerFactOwnershipFacet(
            IdentifierContractIds.Module,
            [
                new CompilerFactOwnershipContract(
                    WistIdentifierFacts.IdentifiersAvailable,
                    IdentifierContractIds.Module)
            ]),
        new PipelineEffectFacet(
            IdentifierContractIds.Module,
            [
                new PipelineEffectContract(
                    IdentifierEffects.RegisterIdentifierSyntax,
                    CompilerPipelineStage.Bytecode,
                    [KnownCoreCompilerFacts.LexemesGenerated],
                    [WistIdentifierFacts.IdentifiersAvailable],
                    [],
                    [])
            ])
    ];
}
