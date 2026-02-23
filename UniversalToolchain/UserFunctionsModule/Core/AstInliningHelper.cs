using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace UserFunctionsModule.Core;

internal static class AstInliningHelper
{
    public static AstNode CloneWithSubstitution(AstNode node, IReadOnlyDictionary<string, AstNode> substitutions)
    {
        if (node.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable") && substitutions.TryGetValue(node.Text, out var replacement))
            return CloneAst(replacement);

        var clone = new AstNode(node.NodeType, node.LexemeValue, []);
        foreach (var tag in node.CurrentTags)
            clone.AddTag(tag);

        foreach (var child in node.Children)
            clone.Children.Add(CloneWithSubstitution(child, substitutions));

        return clone;
    }

    public static AstNode CloneAst(AstNode node)
    {
        var clone = new AstNode(node.NodeType, node.LexemeValue, []);
        foreach (var tag in node.CurrentTags)
            clone.AddTag(tag);

        foreach (var child in node.Children)
            clone.Children.Add(CloneAst(child));

        return clone;
    }

    public static IReadOnlyList<AstNode> ExtractArguments(AstNode argsScope)
    {
        return argsScope.Children.Where(x => x.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Comma")).ToList();
    }

    public static IReadOnlyList<string> ExtractParameters(AstNode paramsScope)
    {
        return paramsScope.Children
            .Where(x => x.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable"))
            .Select(x => x.Text)
            .ToList();
    }
}