namespace StringsModule.Parsing;

public static class StringLiteralDecoder
{
    public static string Decode(string literalText)
    {
        if (literalText.Length < 2 || !literalText.StartsWith('"') || !literalText.EndsWith('"'))
            Thrower.InvalidOpEx($"String literal '{literalText}' must be wrapped in double quotes.");

        var source = literalText[1..^1];
        var builder = new StringBuilder(source.Length);

        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];
            if (current != '\\')
            {
                builder.Append(current);
                continue;
            }

            if (i == source.Length - 1)
                Thrower.InvalidOpEx($"String literal '{literalText}' ends with incomplete escape sequence.");

            i++;
            var escaped = source[i] switch
            {
                '"' => '"',
                '\\' => '\\',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                '0' => '\0',
                _ => Thrower.InvalidOpEx<char>($"Escape sequence '\\{source[i]}' is not supported.")
            };

            builder.Append(escaped);
        }

        return builder.ToString();
    }
}
