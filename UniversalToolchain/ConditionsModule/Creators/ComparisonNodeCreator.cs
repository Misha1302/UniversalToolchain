namespace ConditionsModule.Creators;

public class ComparisonNodeCreator(string nodeType) : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType);

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != AstNodeType)
            return false;

        if (childIndex <= 0 || childIndex >= scope.Children.Count - 1)
            return false;

        var opNode = scope[childIndex];
        var leftOperand = scope.SafeGet(childIndex - 1);
        var rightOperand = scope.SafeGet(childIndex + 1);
        if (leftOperand == null || rightOperand == null)
            return false;

        opNode.Children.Add(leftOperand);
        opNode.Children.Add(rightOperand);
        scope.Children.RemoveAt(childIndex + 1);
        scope.Children.RemoveAt(childIndex - 1);

        opNode.NodeType = AstNodeType;
        return true;
    }
}