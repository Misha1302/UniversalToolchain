namespace ConditionsModule.Creators;

public sealed class IfExpressionNodeCreator : IAstNodeCreator
{
    private static readonly ExtensibleEnum<AstNodeTag> IfNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("If");
    private static readonly ExtensibleEnum<AstNodeTag> ThenNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Then");
    private static readonly ExtensibleEnum<AstNodeTag> ElseNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Else");

    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } = ExtensibleEnum<AstNodeTag>.CreateOrGet("IfExpression");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        scope = scope.ArgNotNull();

        var ifToken = scope.SafeGet(childIndex);
        if (ifToken?.NodeType != IfNodeType)
            return false;

        var thenIndex = FindNext(scope, childIndex + 1, ThenNodeType);
        if (thenIndex < 0)
            return false;

        var elseIndex = FindNext(scope, thenIndex + 1, ElseNodeType);
        if (elseIndex < 0)
            return false;

        var condition = ReadSingleExpression(scope, childIndex + 1, thenIndex, "if-expression condition");
        var trueExpression = ReadSingleExpression(scope, thenIndex + 1, elseIndex, "if-expression true branch");
        var falseExpression = ReadSingleExpression(scope, elseIndex + 1, elseIndex + 2, "if-expression false branch");

        var ifExpression = new AstNode(AstNodeType, ifToken.LexemeValue, [condition, trueExpression, falseExpression]);
        var removeCount = elseIndex + 1 - childIndex + 1;
        scope.Children.RemoveRange(childIndex, removeCount);
        scope.Children.Insert(childIndex, ifExpression);
        return true;
    }

    private static int FindNext(AstNode scope, int startIndex, ExtensibleEnum<AstNodeTag> nodeType)
    {
        for (var i = startIndex; i < scope.Children.Count; i++)
        {
            if (scope.Children[i].NodeType == nodeType)
                return i;
        }

        return -1;
    }

    private static AstNode ReadSingleExpression(AstNode scope, int startIndex, int endIndex, string segmentName)
    {
        scope = scope.ArgNotNull();

        if (startIndex < 0 || endIndex > scope.Children.Count || startIndex >= endIndex)
            Thrower.InvalidOpEx($"Expected exactly one expression in {segmentName}.");

        var count = endIndex - startIndex;
        if (count != 1)
            Thrower.InvalidOpEx($"Expected exactly one expression in {segmentName}.");

        return scope.Children[startIndex];
    }
}