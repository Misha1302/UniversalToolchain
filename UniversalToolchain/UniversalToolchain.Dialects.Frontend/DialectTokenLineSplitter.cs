using BasicCore.LexerWrapper;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectTokenLineSplitter
{
    public static List<List<LexemeValue>> Split(IReadOnlyList<LexemeValue> tokens)
    {
        var lines = new List<List<LexemeValue>>();
        var current = new List<LexemeValue>();

        foreach (var token in tokens)
        {
            if (DialectLexemeTags.IsTag(token, DialectLexemeTags.NewLine))
            {
                lines.Add(current);
                current = new List<LexemeValue>();
                continue;
            }

            current.Add(token);
        }

        if (current.Count > 0)
        {
            lines.Add(current);
        }

        return lines;
    }
}
