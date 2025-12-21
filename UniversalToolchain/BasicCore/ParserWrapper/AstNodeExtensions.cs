namespace BasicCore.ParserWrapper;

public static class AstNodeExtensions
{
    public static readonly string ParserHandledTag = "ParserHandled_" + Guid.NewGuid();

    public static bool IsParserHandled(this AstNode node)
    {
        return node.CurrentTags.Contains(ParserHandledTag);
    }

    public static void MarkAsParserHandled(this AstNode node)
    {
        node.AddTag(ParserHandledTag);
    }
}