namespace CSharpInteropModule.Creators;

public class CSharpFunctionCallsNodeCreator : IAstNodeCreator
{
    private readonly IMethodResolver _methodResolver;

    public CSharpFunctionCallsNodeCreator(IMethodResolver methodResolver)
    {
        _methodResolver = methodResolver.ArgNotNull();
    }

    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("CSharpFunctionCall");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (childIndex < 0 || childIndex >= scope.Children.Count)
            return false;

        var child = scope.Children[childIndex];
        if (child.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier"))
            return false;

        // Classify a syntactically qualified call as CLR interop as soon as its declaring type
        // belongs to the explicit catalog. Method visibility and overload validity are checked
        // by the visitor so unsupported calls fail with a deterministic ImportException instead
        // of silently falling through to variable resolution.
        if (!_methodResolver.CanResolveDeclaringType(child.Text))
            return false;

        if (childIndex + 1 >= scope.Children.Count)
            return false;

        child.NodeType = AstNodeType;

        child.Children.Add(scope.Children[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
}
