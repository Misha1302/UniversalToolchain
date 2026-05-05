namespace ConditionsModule.Creators;

[AutoRegisterService]
public class BooleanNodeCreator(string nodeType, BooleanNodeCreator.BooleanStatementType type) : IAstNodeCreator
{
    public enum BooleanStatementType
    {
        Constant,
        UnaryOperation,
        BinaryOperation
    }

    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType);

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var currentNode = scope.SafeGet(childIndex);
        if (currentNode?.NodeType != AstNodeType)
            return false;

        return type switch
        {
            BooleanStatementType.UnaryOperation => TryCreateUnaryOperation(scope, childIndex, currentNode),
            BooleanStatementType.BinaryOperation => TryCreateBinaryOperation(scope, childIndex, currentNode),
            BooleanStatementType.Constant => true,
            _ => false
        };
    }

    private static bool TryCreateUnaryOperation(AstNode scope, int childIndex, AstNode node)
    {
        var operand = scope.SafeGet(childIndex + 1);
        if (operand == null)
            return false;

        node.Children.Add(operand);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }

    private static bool TryCreateBinaryOperation(AstNode scope, int childIndex, AstNode node)
    {
        var leftOperand = scope.SafeGet(childIndex - 1);
        var rightOperand = scope.SafeGet(childIndex + 1);
        if (leftOperand == null || rightOperand == null)
            return false;

        node.Children.Add(leftOperand);
        node.Children.Add(rightOperand);
        scope.Children.RemoveAt(childIndex + 1);
        scope.Children.RemoveAt(childIndex - 1);
        return true;
    }
}