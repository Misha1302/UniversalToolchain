using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace LoopsModule;

public class WhileNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("While");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != AstNodeType)
            return false;

        var whileNode = scope[childIndex];
        if (scope.SafeGet(childIndex + 1) == null || scope.SafeGet(childIndex + 2) == null)
            return false;

        whileNode.Children.Add(scope[childIndex + 1]); // condition
        scope.Children.RemoveAt(childIndex + 1);

        whileNode.Children.Add(scope[childIndex + 1]); // body
        scope.Children.RemoveAt(childIndex + 1);

        return true;
    }
}