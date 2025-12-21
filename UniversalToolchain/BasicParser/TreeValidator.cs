using BasicCore.ParserWrapper;

namespace BasicParser;

public class TreeValidator
{
    public bool IsValidTree(AstNode root)
    {
        return root.Children.All(x => x.NodeType != AstNodeType.Get("Unknown")) && root.Children.All(IsValidTree);
    }
}