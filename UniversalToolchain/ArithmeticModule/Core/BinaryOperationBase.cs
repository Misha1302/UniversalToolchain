using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule;

public abstract class BinaryOperationBase(string enumStr) : IAstNodeCreator
{
    public virtual ExtensibleEnum<AstNodeTag> AstNodeType => ExtensibleEnum<AstNodeTag>.CreateOrGet(enumStr);

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var child = scope.Children[childIndex];
        if (child.NodeType != AstNodeType) return false;

        child.Children.Add(scope.Children[childIndex - 1]);
        child.Children.Add(scope.Children[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        scope.Children.RemoveAt(childIndex - 1);

        return true;
    }
}