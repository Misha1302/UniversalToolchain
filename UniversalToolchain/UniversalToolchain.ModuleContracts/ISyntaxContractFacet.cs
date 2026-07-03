namespace UniversalToolchain.ModuleContracts;

public interface ISyntaxContractFacet : IModuleContractFacet
{
    IReadOnlyList<LexemeContract> Lexemes { get; }

    IReadOnlyList<ParserNodeContract> ParserNodes { get; }
}
