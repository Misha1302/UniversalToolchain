using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ConditionsModule;

public class ComparisonNodeCreator(string nodeType) : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType);

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != AstNodeType)
            return false;

        var opNode = scope[childIndex];

        // Left operand
        if (scope.SafeGet(childIndex - 1) != null)
        {
            opNode.Children.Add(scope[childIndex - 1]);
            scope.Children.RemoveAt(childIndex - 1);
            childIndex--; // Index changed after removal
        }

        // Right operand
        if (scope.SafeGet(childIndex + 1) != null)
        {
            opNode.Children.Add(scope[childIndex + 1]);
            scope.Children.RemoveAt(childIndex + 1);
        }

        opNode.NodeType = AstNodeType;
        return true;
    }
}