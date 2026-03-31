namespace ArithmeticModule.Creators;

public class UnaryMinusOperationNodeCreator : IAstNodeCreator
{
    private static readonly ExtensibleEnum<AstNodeTag> _substractionNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Substraction");
    private static readonly ExtensibleEnum<AstNodeTag> _unaryMinusNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("UnaryMinus");

    private static readonly HashSet<ExtensibleEnum<AstNodeTag>> _nonOperandNodeTypes =
    [
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Addition"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Substraction"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Multiplication"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Division"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("OpenPar"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("ClosePar"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Let"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("If"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Else"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("While"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality"),
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Colon")
    ];

    public ExtensibleEnum<AstNodeTag> AstNodeType => _substractionNodeType;

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var currentNode = scope.SafeGet(childIndex);
        if (currentNode?.NodeType != _substractionNodeType)
            return false;

        if (HasCompletedLeftOperand(scope, childIndex))
            return false;

        var rightOperand = scope.SafeGet(childIndex + 1);
        if (rightOperand == null || !IsOperandNode(rightOperand))
            return false;

        currentNode.NodeType = _unaryMinusNodeType;
        currentNode.Children.Add(rightOperand);
        scope.Children.RemoveAt(childIndex + 1);

        return true;
    }

    private bool HasCompletedLeftOperand(AstNode scope, int childIndex)
    {
        if (childIndex <= 0)
            return false;

        var leftNode = scope.SafeGet(childIndex - 1);
        if (leftNode == null)
            return false;

        return IsOperandNode(leftNode);
    }

    private static bool IsOperandNode(AstNode node)
    {
        return !_nonOperandNodeTypes.Contains(node.NodeType);
    }
}
