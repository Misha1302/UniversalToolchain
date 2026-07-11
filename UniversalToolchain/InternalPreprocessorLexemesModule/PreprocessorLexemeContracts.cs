using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace InternalPreprocessorLexemesModule;

public static class PreprocessorLexemeContracts
{
    public const string NodeTypeName = "Preprocessor lexeme";

    private static readonly ExtensibleEnum<AstNodeTag> NodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet(NodeTypeName);

    public static bool IsPreprocessorLexeme(AstNode node) => node.NodeType == NodeType;

    public static bool TryReadDefineDirective(AstNode node, out PreprocessorDefineDirective directive)
    {
        directive = null!;

        if (!IsPreprocessorLexeme(node))
            return false;

        var body = ReadBody(node.Text);
        if (body.Length == 0)
            return false;

        var parts = body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            return false;

        if (!string.Equals(parts[0], "define", StringComparison.Ordinal)
            || !string.Equals(parts[2], "as", StringComparison.Ordinal))
            return false;

        directive = new PreprocessorDefineDirective(parts[1], parts[3]);
        return true;
    }

    private static string ReadBody(string text)
    {
        if (text.StartsWith("#![", StringComparison.Ordinal) && text.EndsWith(']'))
            return text[3..^1].Trim();

        if (text.StartsWith("#![", StringComparison.Ordinal))
            return text[3..].Trim();

        return text.Trim();
    }
}
