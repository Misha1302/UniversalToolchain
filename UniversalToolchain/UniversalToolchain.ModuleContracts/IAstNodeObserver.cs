namespace UniversalToolchain.ModuleContracts;

public interface IAstNodeObserver
{
    ModuleId ModuleId { get; }

    IReadOnlyList<AstNodeKind> ObservedNodeKinds { get; }

    void Observe(AstNode node);
}
