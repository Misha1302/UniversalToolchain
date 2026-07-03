namespace UniversalToolchain.ModuleContracts;

public sealed record SyntaxContractFacet(
    ModuleId ModuleId,
    IReadOnlyList<LexemeContract> Lexemes,
    IReadOnlyList<ParserNodeContract> ParserNodes) : ISyntaxContractFacet
{
    public ContractFacetKind Kind => ContractFacetKind.Syntax;

    public ContractSchemaVersion SchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;
}
