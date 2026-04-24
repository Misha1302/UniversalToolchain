namespace ConditionsModule.Creators;

public sealed class IfExpressionNodeCreator : IAstNodeCreator
{
    private static readonly ExtensibleEnum<AstNodeTag> ElseType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Else");
    private static readonly ExtensibleEnum<AstNodeTag> IfExpressionType = ExtensibleEnum<AstNodeTag>.CreateOrGet("IfExpression");
    private static readonly ExtensibleEnum<AstNodeTag> IfType = ExtensibleEnum<AstNodeTag>.CreateOrGet("If");
    private static readonly ExtensibleEnum<AstNodeTag> ScopeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope");
    private static readonly ExtensibleEnum<AstNodeTag> ThenType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Then");

    public ExtensibleEnum<AstNodeTag> AstNodeType => IfExpressionType;

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var ifNode = scope.SafeGet(childIndex);
        if (ifNode?.NodeType != IfType)
        {
            return false;
        }

        var thenIndex = FindThenIndex(scope, childIndex + 1);
        if (thenIndex < 0)
        {
            return false;
        }

        var elseIndex = FindElseIndex(scope, thenIndex + 1);
        if (elseIndex < 0)
        {
            return false;
        }

        if (thenIndex == childIndex + 1 || elseIndex == thenIndex + 1 || elseIndex == scope.Children.Count - 1)
        {
            return false;
        }

        var conditionScope = CreateExpressionScope(scope.Children.Skip(childIndex + 1).Take(thenIndex - childIndex - 1));
        var thenScope = CreateExpressionScope(scope.Children.Skip(thenIndex + 1).Take(elseIndex - thenIndex - 1));
        var elseScope = CreateExpressionScope(scope.Children.Skip(elseIndex + 1));

        ifNode.NodeType = IfExpressionType;
        ifNode.Children.Add(conditionScope);
        ifNode.Children.Add(thenScope);
        ifNode.Children.Add(elseScope);

        scope.Children.RemoveRange(childIndex + 1, scope.Children.Count - childIndex - 1);
        return true;
    }

    private static int FindThenIndex(AstNode scope, int startIndex)
    {
        var nestedIfDepth = 0;
        for (var i = startIndex; i < scope.Children.Count; i++)
        {
            var nodeType = scope.Children[i].NodeType;
            if (nodeType == IfType)
            {
                nestedIfDepth++;
                continue;
            }

            if (nodeType == ElseType && nestedIfDepth > 0)
            {
                nestedIfDepth--;
                continue;
            }

            if (nodeType == ThenType && nestedIfDepth == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindElseIndex(AstNode scope, int startIndex)
    {
        var nestedIfDepth = 0;
        for (var i = startIndex; i < scope.Children.Count; i++)
        {
            var nodeType = scope.Children[i].NodeType;
            if (nodeType == IfType)
            {
                nestedIfDepth++;
                continue;
            }

            if (nodeType == ElseType)
            {
                if (nestedIfDepth == 0)
                {
                    return i;
                }

                nestedIfDepth--;
            }
        }

        return -1;
    }

    private static AstNode CreateExpressionScope(IEnumerable<AstNode> nodes)
    {
        return new AstNode(ScopeType, null, nodes.ToList());
    }
}
