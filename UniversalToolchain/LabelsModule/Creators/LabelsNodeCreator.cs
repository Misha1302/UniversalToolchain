namespace LabelsModule.Creators;

public class LabelsNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("Label");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope[childIndex].NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier"))
            return false;
        if (scope.SafeGet(childIndex + 1)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Colon"))
            return false;
        if (!scope[childIndex].Text.StartsWith('@'))
            return false;

        scope[childIndex].NodeType = AstNodeType;
        scope[childIndex].Children.Add(scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
}