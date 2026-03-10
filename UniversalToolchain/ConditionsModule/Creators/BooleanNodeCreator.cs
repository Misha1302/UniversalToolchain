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

        var node = scope[childIndex];

        if (type == BooleanStatementType.UnaryOperation && scope.SafeGet(childIndex + 1) != null)
        {
            // Unary NOT operation
            node.Children.Add(scope[childIndex + 1]);
            scope.Children.RemoveAt(childIndex + 1);
        }
        else if (type == BooleanStatementType.BinaryOperation)
        {
            // Binary operations
            if (scope.SafeGet(childIndex - 1) != null)
            {
                node.Children.Add(scope[childIndex - 1]);
                scope.Children.RemoveAt(childIndex - 1);
                childIndex--;
            }

            if (scope.SafeGet(childIndex + 1) != null)
            {
                node.Children.Add(scope[childIndex + 1]);
                scope.Children.RemoveAt(childIndex + 1);
            }
        }

        return true;
    }
}