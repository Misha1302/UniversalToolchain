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

        var lines = astRoot.Children
            .Where(node => node.NodeType == DialectLineType)
            .Select(ReadLine)
            .ToList();

        if (lines.Count == 0)
        {
            var tokens = astRoot.Children
                .Select(node => node.LexemeValue)
                .Where(lexeme => lexeme != null)
                .Select(lexeme => lexeme!)
                .ToList();

            return DialectTokenLineSplitter.Split(tokens);
        }

        return lines;
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
