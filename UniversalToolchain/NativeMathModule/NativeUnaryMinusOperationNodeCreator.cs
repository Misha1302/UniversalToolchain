namespace NativeMathModule;

public class NativeUnaryMinusOperationNodeCreator : IAstNodeCreator
{
    private static readonly ExtensibleEnum<AstNodeTag> NativeSubtraction = ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeSubtraction");
    private static readonly ExtensibleEnum<AstNodeTag> NativeAddition = ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeAddition");
    private static readonly ExtensibleEnum<AstNodeTag> NativeMultiplication = ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeMultiplication");
    private static readonly ExtensibleEnum<AstNodeTag> NativeDivision = ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeDivision");
    public ExtensibleEnum<AstNodeTag> AstNodeType => NativeSubtraction;

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (childIndex < 0 || childIndex >= scope.Children.Count)
            return false;

        var child = scope.Children[childIndex];
        if (child.NodeType != NativeSubtraction)
            return false;

        if (!IsUnaryPosition(scope, childIndex))
            return false;

        if (childIndex >= scope.Children.Count - 1)
            return false;

        child.NodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeUnaryMinus");
        child.Children.Add(scope.Children[childIndex + 1]);
        scope.Children.RemoveAt(childIndex + 1);

        return true;
    }

    private static bool IsUnaryPosition(AstNode scope, int childIndex)
    {
        if (childIndex == 0)
            return true;

        var previous = scope.Children[childIndex - 1].NodeType;
        return previous == NativeAddition ||
               previous == NativeSubtraction ||
               previous == NativeMultiplication ||
               previous == NativeDivision;
    }
}