namespace UniversalToolchain.ModuleContracts;

public interface IAstNodeLowerer
{
    ModuleId ModuleId { get; }

    AstNodeKind NodeKind { get; }

    LoweringResult Lower(AstNode node, AstNodeLoweringContext context);
}
