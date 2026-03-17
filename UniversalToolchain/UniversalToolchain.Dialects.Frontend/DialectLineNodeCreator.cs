using BasicCore.ParserWrapper;
using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectLineNodeCreator : IAstNodeCreator
{
    public AstNodeType AstNodeType { get; } = AstNodeType.CreateOrGet("DialectLine");


    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        if (scope == null)
        {
            return false;
        }

        if (childIndex < 0 || childIndex >= scope.Children.Count)
        {
            return false;
        }

        if (scope.NodeType != AstNodeType.CreateOrGet("Scope"))
        {
            return false;
        }

        var current = scope.Children[childIndex];
        if (current.NodeType == AstNodeType)
        {
            return false;
        }

        if (current.LexemeValue == null)
        {
            return false;
        }
        if (IsNewLineToken(current))
        {
            scope.Children.RemoveAt(childIndex);
            return true;
        }

        if (childIndex > 0 && !IsNewLineToken(scope.Children[childIndex - 1]))
        {
            return false;
        }

        var end = childIndex;
        while (end < scope.Children.Count && !IsNewLineToken(scope.Children[end]))
        {
            end++;
        }

        var lineChildren = new List<AstNode>();
        for (var i = childIndex; i < end; i++)
        {
            lineChildren.Add(scope.Children[i]);
        }

        var lineNode = new AstNode(AstNodeType, null, lineChildren);
        var removeCount = end - childIndex;
        if (end < scope.Children.Count && IsNewLineToken(scope.Children[end]))
        {
            removeCount++;
        }

        scope.Children.RemoveRange(childIndex, removeCount);
        scope.Children.Insert(childIndex, lineNode);
        return true;
    }

    private static bool IsNewLineToken(AstNode node)
    {
        return DialectLexemeTags.IsTag(node.LexemeValue, DialectLexemeTags.NewLine);
    }
}
