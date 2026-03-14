namespace ConditionsModule.Creators;

public class IfNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("If");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("If"))
            return false;

        var ifNode = scope[childIndex];

        // Next node should be a condition (scope)
        if (scope.SafeGet(childIndex + 1) == null)
            return false;

        // Add condition as child node
        ifNode.Children.Add(scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);

        // Add if body (next scope)
        if (scope.SafeGet(childIndex + 1) != null)
        {
            ifNode.Children.Add(scope[childIndex + 1]);
            scope.Children.RemoveAt(childIndex + 1);
        }

        return true;
    }
}