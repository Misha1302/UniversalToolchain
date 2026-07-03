namespace UniversalToolchain.ModuleContracts;

public interface IAstNodeValidator
{
    ModuleId ModuleId { get; }

    IReadOnlyList<AstNodeKind> ValidatedNodeKinds { get; }

    IReadOnlyList<ToolchainDiagnostic> Validate(AstNode node);
}
