namespace CSharpInteropModule.Creators;

public class CSharpFunctionCallsNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("CSharpFunctionCall");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (childIndex < 0 || childIndex >= scope.Children.Count)
            return false;

        var child = scope.Children[childIndex];
        if (child.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier"))
            return false;

        if (!MethodsFinder.ContainsAnyMethod(child.Text)) return false;

        if (childIndex + 1 >= scope.Children.Count)
            return false;

        child.NodeType = AstNodeType;

        child.Children.Add(scope.Children[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
}