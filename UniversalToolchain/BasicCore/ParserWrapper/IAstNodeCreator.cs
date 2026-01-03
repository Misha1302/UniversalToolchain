namespace BasicCore.ParserWrapper;

public interface IAstNodeCreator
{
    public AstNodeType AstNodeType { get; }
    public Predicate<AstNode>? NeedToVisitPredicate => null;

    public bool TryCreateNode(AstNode scope, int childIndex);
}