using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ConditionsModule.Creators;

public class ElifNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("Elif");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Elif"))
            return false;

        var elifNode = scope[childIndex];

        // Next node should be a condition (scope)
        if (scope.SafeGet(childIndex + 1) == null)
            return false;

        // Add condition as child node
        elifNode.Children.Add(scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);

        // Add elif body (next scope)
        if (scope.SafeGet(childIndex + 1) != null)
        {
            elifNode.Children.Add(scope[childIndex + 1]);
            scope.Children.RemoveAt(childIndex + 1);
        }

        return true;
    }
}