using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules.Syntax;

public sealed class WistRuleBodySyntaxAnalyzer
{
    public WistRuleBodySyntaxInfo Analyze(string bodySourceText, int bodyStartOffset)
    {
        bodySourceText = bodySourceText.ArgNotNull();

        var locals = new List<LocalBindingDeclarationModel>();
        var diagnostics = new List<ToolchainDiagnostic>();
        var declarationOrder = 0;
        var cursor = 0;

        while (cursor < bodySourceText.Length)
        {
            SkipTrivia(bodySourceText, ref cursor);
            if (cursor >= bodySourceText.Length)
                break;

            if (!TryReadKeyword(bodySourceText, ref cursor, "let"))
            {
                cursor++;
                continue;
            }

            SkipTrivia(bodySourceText, ref cursor);
            if (!TryReadIdentifier(bodySourceText, ref cursor, out var identifier, out var start))
            {
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, "Invalid local binding declaration after 'let'."));
                continue;
            }

            SkipTrivia(bodySourceText, ref cursor);
            if (cursor < bodySourceText.Length && bodySourceText[cursor] == ':')
            {
                cursor++;
                SkipTrivia(bodySourceText, ref cursor);
                if (!TryReadIdentifier(bodySourceText, ref cursor, out _, out _))
                {
                    diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Invalid typed local declaration for '{identifier}'."));
                    continue;
                }

                SkipTrivia(bodySourceText, ref cursor);
            }

            if (cursor >= bodySourceText.Length || bodySourceText[cursor] != '=')
            {
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected '=' in local declaration for '{identifier}'."));
                continue;
            }

            locals.Add(new LocalBindingDeclarationModel(
                identifier,
                declarationOrder++,
                new RuleScopeId(0),
                new SourceSpan(bodyStartOffset + start, identifier.Length)));

            cursor++;
        }

        return new WistRuleBodySyntaxInfo(locals, diagnostics);
    }

    private static void SkipTrivia(string source, ref int cursor)
    {
        while (cursor < source.Length)
        {
            var current = source[cursor];
            if (char.IsWhiteSpace(current))
            {
                cursor++;
                continue;
            }

            if (cursor + 1 < source.Length && current == '/' && source[cursor + 1] == '/')
            {
                cursor += 2;
                while (cursor < source.Length && source[cursor] != '\n')
                    cursor++;
                continue;
            }

            if (cursor + 1 < source.Length && current == '/' && source[cursor + 1] == '*')
            {
                cursor += 2;
                while (cursor + 1 < source.Length && !(source[cursor] == '*' && source[cursor + 1] == '/'))
                    cursor++;

                if (cursor + 1 < source.Length)
                    cursor += 2;

                continue;
            }

            if (current == '"')
            {
                cursor++;
                while (cursor < source.Length)
                {
                    if (source[cursor] == '\\' && cursor + 1 < source.Length)
                    {
                        cursor += 2;
                        continue;
                    }

                    if (source[cursor] == '"')
                    {
                        cursor++;
                        break;
                    }

                    cursor++;
                }

                continue;
            }

            break;
        }
    }

    private static bool TryReadKeyword(string source, ref int cursor, string keyword)
    {
        if (cursor + keyword.Length > source.Length)
            return false;

        if (!string.Equals(source.Substring(cursor, keyword.Length), keyword, StringComparison.Ordinal))
            return false;

        var beforeOk = cursor == 0 || !IsIdentifierChar(source[cursor - 1]);
        var afterIndex = cursor + keyword.Length;
        var afterOk = afterIndex >= source.Length || !IsIdentifierChar(source[afterIndex]);
        if (!beforeOk || !afterOk)
            return false;

        cursor += keyword.Length;
        return true;
    }

    private static bool TryReadIdentifier(string source, ref int cursor, out string identifier, out int start)
    {
        start = cursor;
        if (cursor >= source.Length || !(char.IsLetter(source[cursor]) || source[cursor] == '_'))
        {
            identifier = string.Empty;
            return false;
        }

        cursor++;
        while (cursor < source.Length && IsIdentifierChar(source[cursor]))
            cursor++;

        identifier = source[start..cursor];
        return true;
    }

    private static bool IsIdentifierChar(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private static ToolchainDiagnostic CreateDiagnostic(string code, string message)
    {
        return new ToolchainDiagnostic(code, ToolchainDiagnosticSeverity.Error, message, null, []);
    }
}
