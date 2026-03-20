namespace UniversalToolchain.Dialects.Frontend;

public static class DialectTokenLineSplitter
{
    public static List<List<LexemeValue>> Split(IReadOnlyList<LexemeValue> tokens)
    {
        if (tokens == null)
            Thrower.ArgumentNull(nameof(tokens));

        var result = new List<List<LexemeValue>>();
        var current = new List<LexemeValue>();

        foreach (var token in tokens)
        {
            if (DialectLexemeTags.IsTag(token, DialectLexemeTags.NewLine))
            {
                if (current.Count > 0)
                {
                    result.Add(current);
                    current = [];
                }

                continue;
            }

            current.Add(token);
        }

        if (current.Count > 0)
            result.Add(current);

        return result;
    }
}