namespace CSharpInteropModule.Creators;

public class CSharpFunctionCallsNodeCreator : IAstNodeCreator
{
    public CSharpFunctionCallsNodeCreator(IMethodResolver methodResolver)
    {
        _ = methodResolver.ArgNotNull();
    }

    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("CSharpFunctionCall");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (childIndex < 0 || childIndex >= scope.Children.Count)
            return false;

        var child = scope.Children[childIndex];
        if (child.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier"))
            return false;

        // A qualified identifier followed by an argument scope is CLR-interop syntax. The
        // explicit type catalog and method resolver remain the authority for whether it is
        // allowed; denied types and missing methods must fail as interop errors instead of
        // falling through to variable binding. A dotted type annotation is not a call.
        if (!child.Text.Contains('.', StringComparison.Ordinal))
            return false;

        var argumentsScope = scope.SafeGet(childIndex + 1);
        if (argumentsScope?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"))
            return false;

        child.NodeType = AstNodeType;
        child.Children.Add(argumentsScope);
        scope.Children.RemoveAt(childIndex + 1);
        return true;
    }
}
