namespace EqualityModule;

public class ValuesSetNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("Set");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope.SafeGet(childIndex)?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality")) return false;
        if (scope.SafeGet(childIndex - 1)?.NodeType == null) return false;
        if (scope.SafeGet(childIndex + 1)?.NodeType == null) return false;

        var eqNode = scope[childIndex];
        eqNode.Children.AddRange(scope[childIndex - 1], scope[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);
        scope.Children.RemoveAt(childIndex - 1);

        return true;
    }
}
