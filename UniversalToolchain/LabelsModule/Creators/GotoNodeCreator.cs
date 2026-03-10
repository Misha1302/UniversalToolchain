namespace LabelsModule.Creators;

public class GotoNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("Goto");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope[childIndex].NodeType != AstNodeType) return false;
        if (scope.Children.Count <= childIndex + 1) return false;
        if (scope[childIndex + 1].NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier")) return false;

        scope[childIndex].Children.Add(scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
}