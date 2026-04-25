namespace FunctionCallsModule;

public sealed class FunctionCallsNodeCreator : IAstNodeCreator
{
    public AstNodeType AstNodeType { get; } = AstNodeType.CreateOrGet("FunctionCall");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var identifier = scope.SafeGet(childIndex);
        if (identifier?.NodeType != AstNodeType.CreateOrGet("Identifier"))
            return false;

        var argumentsScope = scope.SafeGet(childIndex + 1);
        if (argumentsScope?.NodeType != AstNodeType.CreateOrGet("Scope"))
            return false;

        identifier.NodeType = AstNodeType;
        identifier.Children.Add(argumentsScope);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
}
