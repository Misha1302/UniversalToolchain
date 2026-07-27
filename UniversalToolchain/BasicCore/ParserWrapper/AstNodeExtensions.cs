namespace BasicCore.ParserWrapper;

public static class AstNodeExtensions
{
    public const string ParserHandledTag = "universal-toolchain.parser.handled";

    public static bool IsParserHandled(this AstNode node) => node.CurrentTags.Contains(ParserHandledTag);

    public static void MarkAsParserHandled(this AstNode node)
    {
        node.AddTag(ParserHandledTag);
    }
}