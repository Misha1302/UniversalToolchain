// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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