using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Parsing;

internal static class DialectLexer
{
    public static IReadOnlyList<DialectToken> Lex(string sourceText, IList<DialectDiagnostic> diagnostics)
    {
        if (sourceText == null)
            Thrower.ArgumentNull(nameof(sourceText));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        var tokens = new List<DialectToken>();
        var index = 0;
        var line = 1;
        var column = 1;

        while (index < sourceText.Length)
        {
            var current = sourceText[index];

            if (current == '\r')
            {
                index++;
                continue;
            }

            if (current == '\n')
            {
                tokens.Add(new DialectToken(DialectTokenKind.NewLine, "\\n", line, column));
                index++;
                line++;
                column = 1;
                continue;
            }

            if (char.IsWhiteSpace(current))
            {
                index++;
                column++;
                continue;
            }

            if (current == '-' && Peek(sourceText, index + 1) == '>')
            {
                tokens.Add(new DialectToken(DialectTokenKind.Arrow, "->", line, column));
                index += 2;
                column += 2;
                continue;
            }

            if (current == '=')
            {
                tokens.Add(new DialectToken(DialectTokenKind.Equals, "=", line, column));
                index++;
                column++;
                continue;
            }

            if (current == '"')
            {
                var startColumn = column;
                index++;
                column++;
                var value = "";
                var closed = false;

                while (index < sourceText.Length)
                {
                    var ch = sourceText[index];
                    if (ch == '\r')
                    {
                        index++;
                        continue;
                    }

                    if (ch == '"')
                    {
                        closed = true;
                        index++;
                        column++;
                        break;
                    }

                    if (ch == '\n')
                    {
                        break;
                    }

                    value += ch;
                    index++;
                    column++;
                }

                if (!closed)
                {
                    diagnostics.Add(new DialectDiagnostic(
                        "P001",
                        $"Unterminated string literal at line {line}, column {startColumn}.",
                        DialectDiagnosticSeverity.Error));
                    SkipToNextLine(sourceText, ref index, ref line, ref column, tokens);
                    continue;
                }

                tokens.Add(new DialectToken(DialectTokenKind.StringLiteral, value, line, startColumn));
                continue;
            }

            if (IsIdentifierChar(current))
            {
                var start = index;
                var startColumn = column;
                while (index < sourceText.Length && IsIdentifierChar(sourceText[index]))
                {
                    index++;
                    column++;
                }

                var text = sourceText.Substring(start, index - start);
                tokens.Add(new DialectToken(DialectTokenKind.Identifier, text, line, startColumn));
                continue;
            }

            diagnostics.Add(new DialectDiagnostic(
                "P002",
                $"Unexpected character '{current}' at line {line}, column {column}.",
                DialectDiagnosticSeverity.Error));
            index++;
            column++;
        }

        tokens.Add(new DialectToken(DialectTokenKind.EndOfInput, string.Empty, line, column));
        return tokens;
    }

    private static void SkipToNextLine(string sourceText, ref int index, ref int line, ref int column, IList<DialectToken> tokens)
    {
        while (index < sourceText.Length && sourceText[index] != '\n')
        {
            index++;
            column++;
        }

        if (index < sourceText.Length && sourceText[index] == '\n')
        {
            tokens.Add(new DialectToken(DialectTokenKind.NewLine, "\\n", line, column));
            index++;
            line++;
            column = 1;
        }
    }

    private static char Peek(string source, int index) => index < source.Length ? source[index] : '\0';

    private static bool IsIdentifierChar(char value) =>
        char.IsLetterOrDigit(value) || value is '_' or '-' or '.';
}
