namespace BasicLexer.Core;

public class BasicLexerImpl(LexerConfiguration configuration) : ILexer
{
    public BasicLexerImpl() : this(new LexerConfiguration([]))
    {
    }

    public LexerConfiguration Configuration { get; } = configuration;

    // Method that performs lexical analysis on the input code and returns a list of tokens.
    public List<LexemeValue> Lexemize(string code)
    {
        // Lexer patterns and source locations use LF as the canonical line break.
        // Normalize Windows CRLF and legacy CR inputs before matching so the
        // same source text has identical lexical behavior on every platform.
        if (code.Contains('\r'))
            code = code.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

        var patterns = Configuration.Patterns.ToList();

        var allMatches = new List<LexemeValue>(); // Initialize a list to store all matches found by regex patterns.

        // Iterate over each pattern defined in the configuration.
        foreach (var pattern in patterns)
            // Find all occurrences of the current pattern in the input code using regular expressions.

        {
            allMatches.AddRange(
                Regex.Matches(code, pattern.Pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000))
                    .Select(match =>
                        // Create a LexemeValue object for each match.
                        new LexemeValue(match.Value, pattern, match.Index, code))
            );
        }

        // Sort the matches first by their starting position in the code, then by the order of the corresponding pattern in the configuration.
        allMatches = allMatches
            .OrderBy(x => x.StartIndex)
            .ThenBy(x => patterns.IndexOf(x.LexemePattern.NotNull()))
            .ToList();

        var result =
            new List<LexemeValue>(); // Initialize a list to store the final tokens after filtering out ignored ones.
        var index = 0; // Current character position being processed in the input code.
        var prevFoundIndex = 0; // Used to optimize search for next matching token.

        // Process the input code until reaching its end.
        while (index < code.Length)
        {
            // Find the next valid token starting from the current position.
            // ReSharper disable once AccessToModifiedClosure
            (var lexeme, prevFoundIndex) = allMatches.FirstStarts(x => x.StartIndex >= index, prevFoundIndex);

            if (index != lexeme?.StartIndex)
            {
                var invalidSegment = code[index..(lexeme?.StartIndex ?? code.Length)];
                var invalidLexeme = new LexemeValue(invalidSegment, null, index, code);
                ToolchainThrower.Lexer(
                    $"Invalid token '{invalidSegment}'.",
                    new SourceLocation { Line = invalidLexeme.LineNumber, Column = invalidLexeme.CharNumber }
                );
            }

            Thrower.AssertAlways(lexeme != null, "Internal lexer invariant violated: expected lexeme at current index.");

            // Update the current processing position to just past the matched token.
            index = lexeme.StartIndex + lexeme.Text.Length;

            // If the token type is not one to be ignored, add it to the result list.
            if (!Configuration.LexemesToIgnore.Contains(lexeme.LexemePattern.NotNull().LexemeType))
                result.Add(lexeme);
        }

        return result; // Return the list of filtered tokens.
    }
}