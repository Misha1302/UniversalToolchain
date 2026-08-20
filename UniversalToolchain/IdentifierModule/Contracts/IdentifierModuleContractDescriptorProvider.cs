using UniversalToolchain.ModuleContracts;

namespace IdentifierModule.Contracts;

public sealed class IdentifierModuleContractDescriptorProvider : IModuleContractDescriptorProvider
{
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => [ContractNamespaceOwner.Reserved("wist", "wist")];

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
                    IdentifierFacts.IdentifiersAvailable,
                    IdentifierContractIds.Module)
            ]),
        new PipelineEffectFacet(
            IdentifierContractIds.Module,
            [
                new PipelineEffectContract(
                    IdentifierEffects.RegisterIdentifierSyntax,
                    CompilerPipelineStage.Bytecode,
                    [KnownCoreCompilerFacts.LexemesGenerated],
                    [IdentifierFacts.IdentifiersAvailable],
                    [],
                    [])
            ])
    ];
}
