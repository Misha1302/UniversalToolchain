using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace LoopsModule;

public class ForNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("For");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != AstNodeType)
            return false;

        var forNode = scope[childIndex];

        if (scope.SafeGet(childIndex + 1)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope") ||
            scope.SafeGet(childIndex + 2)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope") ||
            scope.SafeGet(childIndex + 3)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope") ||
            scope.SafeGet(childIndex + 4)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"))
            return false;

        for (var i = 0; i < 4; i++)
        {
            forNode.Children.Add(scope[childIndex + 1]);
            scope.Children.RemoveAt(childIndex + 1);
        }

        return true;
    }
}