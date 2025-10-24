// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Text.RegularExpressions;
using BasicCore.LexerWrapper;
using BasicTypesExtensions;
using ExceptionsManager;

namespace BasicLexer;

public class BasicLexerImpl(LexerConfiguration configuration) : ILexer
{
    public BasicLexerImpl() : this(new LexerConfiguration([], []))
    {
    }

    public LexerConfiguration Configuration { get; } = configuration;

    // Method that performs lexical analysis on the input code and returns a list of tokens.
    public List<LexemeValue> Lexemize(string code)
    {
        var allMatches = new List<LexemeValue>(); // Initialize a list to store all matches found by regex patterns.

        // Iterate over each pattern defined in the configuration.
        foreach (var pattern in Configuration.Patterns)
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
            .ThenBy(x => Configuration.Patterns.IndexOf(x.LexemePattern))
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

            // Handle unrecognized text.
            Thrower.AssertAlways(
                index == lexeme.StartIndex,
                $"Unknown substr '{Regex.Escape(code[index..lexeme.StartIndex])}'"
            );

            // Update the current processing position to just past the matched token.
            index = lexeme.StartIndex + lexeme.Text.Length;

            // If the token type is not one to be ignored, add it to the result list.
            if (!Configuration.LexemesToIgnore.Contains(lexeme.LexemePattern.LexemeType))
                result.Add(lexeme);
        }

        return result; // Return the list of filtered tokens.
    }
}