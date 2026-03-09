using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule.Core;

public abstract class BinaryOperationBase(string enumStr) : IAstNodeCreator
{
    public virtual ExtensibleEnum<AstNodeTag> AstNodeType => ExtensibleEnum<AstNodeTag>.CreateOrGet(enumStr);

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (childIndex <= 0 || childIndex >= scope.Children.Count - 1)
            return false;

        var child = scope.Children[childIndex];
        if (child.NodeType != AstNodeType) return false;

        var leftOperand = scope.Children[childIndex - 1];
        var rightOperand = scope.Children[childIndex + 1];

        child.Children.Add(leftOperand);
        child.Children.Add(rightOperand);
        scope.Children.RemoveAt(childIndex + 1);
        scope.Children.RemoveAt(childIndex - 1);

        return true;
    }
}
