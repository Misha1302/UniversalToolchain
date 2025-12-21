namespace BasicCore.ParserWrapper;

public interface IAstNodeCreator
{
    public AstNodeType AstNodeType { get; }

    public bool TryCreateNode(AstNode scope, int childIndex);
}