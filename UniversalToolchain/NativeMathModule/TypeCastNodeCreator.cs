using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using ExceptionsManager;

namespace NativeMathModule;

public class TypeCastNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } =
        ExtensibleEnum<AstNodeTag>.CreateOrGet("TypeCast");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var node = scope.SafeGet(childIndex);
        if (node?.NodeType != AstNodeType)
            return false;

        var expressionNode = new AstNode(
            ExtensibleEnum<AstNodeTag>.CreateOrGet("Expression"),
            null,
            [scope.SafeGet(childIndex + 1).NotNull()]
        );

        scope.Children.RemoveAt(childIndex + 1);

        node.Children.Add(expressionNode);
        return true;
    }
}