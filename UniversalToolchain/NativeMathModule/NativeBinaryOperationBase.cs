using CommonExceptions;

namespace NativeMathModule;

// Base class for binary operations (similar to ArithmeticModule).
public abstract class NativeBinaryOperationBase(string enumStr) : IAstNodeCreator
{
    public virtual ExtensibleEnum<AstNodeTag> AstNodeType =>
        ExtensibleEnum<AstNodeTag>.CreateOrGet(enumStr);

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var child = scope.Children[childIndex];
        if (child.NodeType != AstNodeType)
            return false;

        if (childIndex <= 0 || childIndex >= scope.Children.Count - 1)
            throw new ParserException("Binary operator requires operands on both sides.");

        child.Children.Add(scope.Children[childIndex - 1]);
        child.Children.Add(scope.Children[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        scope.Children.RemoveAt(childIndex - 1);
        return true;
    }
}
