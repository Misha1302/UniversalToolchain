using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace ConditionsModule.Creators;

public class ElseNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("Else");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Else"))
            return false;

        var elseNode = scope[childIndex];

        // Add else body (next scope)
        if (scope.SafeGet(childIndex + 1)?.NodeType != null)
        {
            elseNode.Children.Add(scope[childIndex + 1]);
            scope.Children.RemoveAt(childIndex + 1);
        }

        return true;
    }
}