namespace UniversalToolchain.ModuleContracts;

public sealed record ParserNodeContract(
    AstNodeKind Produces,
    double Priority,
    IReadOnlyList<AstNodeKind> MayConsume);
