using BasicCore.Attributes;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;

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

        if (type == BooleanStatementType.UnaryOperation)
        {
            var operand = scope.SafeGet(childIndex + 1);
            if (operand == null)
                return false;

            // Unary NOT operation
            node.Children.Add(operand);
            scope.Children.RemoveAt(childIndex + 1);
            return true;
        }

        if (type == BooleanStatementType.BinaryOperation)
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

        return true;
    }
}