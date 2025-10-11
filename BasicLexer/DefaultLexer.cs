using System.Text.RegularExpressions;
using BasicTypesExtensions;

namespace BasicLexer;

public class DefaultLexer(LexerConfiguration configuration)
{
    // Method that performs lexical analysis on the input code and returns a list of tokens.
    public List<LexemeValue> Lexemize(string code)
    {
        var allMatches = new List<LexemeValue>(); // Initialize a list to store all matches found by regex patterns.

        // Iterate over each pattern defined in the configuration.
        foreach (var pattern in configuration.Patterns)
            // Find all occurrences of the current pattern in the input code using regular expressions.

            allMatches.AddRange(
                Regex.Matches(code, pattern.Pattern)
                    .Select(match =>
                        // Create a LexemeValue object for each match.
                        new LexemeValue(match.Value, pattern, match.Index, code))
            );

        // Sort the matches first by their starting position in the code, then by the order of the corresponding pattern in the configuration.
        allMatches = allMatches
            .OrderBy(x => x.StartIndex)
            .ThenBy(x => configuration.Patterns.IndexOf(x.LexemePattern))
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

            // Update the current processing position to just past the matched token.
            index = lexeme.StartIndex + lexeme.Text.Length;

            // If the token type is not one to be ignored, add it to the result list.
            if (!configuration.LexemesToIgnore.Contains(lexeme.LexemePattern.LexemeType))
                result.Add(lexeme);
        }

        return result; // Return the list of filtered tokens.
    }
}