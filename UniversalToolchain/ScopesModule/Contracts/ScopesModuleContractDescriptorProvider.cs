using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Wist.Contracts;

namespace ScopesModule.Contracts;

public sealed class ScopesModuleContractDescriptorProvider : IModuleContractDescriptorProvider
{
    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        new SyntaxContractFacet(
            ScopesContractIds.Module,
            [
                new LexemeContract("OpenPar", "Open parenthesis lexeme registered by ScopesModuleImpl."),
                new LexemeContract("ClosePar", "Close parenthesis lexeme registered by ScopesModuleImpl.")
            ],
            []),
        new CompilerFactOwnershipFacet(
            ScopesContractIds.Module,
            [
                new CompilerFactOwnershipContract(
                    WistScopesFacts.ScopesLocalsBound,
                    ScopesContractIds.Module)
            ]),
        new PipelineEffectFacet(
            ScopesContractIds.Module,
            [
                new PipelineEffectContract(
                    ScopesEffects.BindScopeLocals,
                    CompilerPipelineStage.Bytecode,
                    [],
                    [WistScopesFacts.ScopesLocalsBound],
                    [],
                    [])
            ])
    ];
}
