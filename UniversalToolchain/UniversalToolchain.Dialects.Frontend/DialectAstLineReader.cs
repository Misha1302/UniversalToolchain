using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;
using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstLineReader
{
    private static readonly AstNodeType DialectLineType = AstNodeType.CreateOrGet("DialectLine");

    public static List<List<LexemeValue>> ReadLines(AstNode astRoot)
    {
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        var lines = EnumerateNodes(astRoot)
            .Where(node => node.NodeType == DialectLineType)
            .Select(ReadLine)
            .ToList();

        if (lines.Count == 0)
        {
            DialectDefinitionSliceParseErrors.Fail("Dialect parser did not produce any directive line nodes.", null);
        }

        return lines;
    }

    private static IEnumerable<AstNode> EnumerateNodes(AstNode root)
    {
        yield return root;
        foreach (var child in root.Children)
        {
            foreach (var nested in EnumerateNodes(child))
            {
                yield return nested;
            }
        }
    }

    private static List<LexemeValue> ReadLine(AstNode lineNode)
    {
        return lineNode.Children
            .Select(x => x.LexemeValue)
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();
    }
}
