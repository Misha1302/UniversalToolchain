using System.Text;
using ExceptionsManager;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class WistRuleDeclarationExtractor
{
    public RuleDeclarationExtractionResult Extract(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return RuleDeclarationExtractionResult.Failure(["Rule source must not be empty."]);

        var diagnostics = new List<string>();
        var rules = new List<RuleDeclarationModel>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var position = 0;

        while (TryFindKeyword(source, "rule", position, out var keywordIndex))
        {
            var cursor = keywordIndex + "rule".Length;
            SkipWhiteSpace(source, ref cursor);
            var name = ReadIdentifier(source, ref cursor);
            if (string.IsNullOrWhiteSpace(name))
            {
                diagnostics.Add($"Expected rule name at offset {cursor}.");
                break;
            }

            SkipWhiteSpace(source, ref cursor);
            if (!Consume(source, ref cursor, '('))
            {
                diagnostics.Add($"Expected '(' after rule '{name}'.");
                break;
            }

            var parametersText = ReadUntilMatching(source, ref cursor, '(', ')');
            if (parametersText == null)
            {
                diagnostics.Add($"Rule '{name}' has an unterminated parameter list.");
                break;
            }

            SkipWhiteSpace(source, ref cursor);
            if (!ConsumeArrow(source, ref cursor))
            {
                diagnostics.Add($"Expected '->' after rule '{name}' parameter list.");
                break;
            }

            SkipWhiteSpace(source, ref cursor);
            var returnTypeName = ReadIdentifier(source, ref cursor);
            if (string.IsNullOrWhiteSpace(returnTypeName))
            {
                diagnostics.Add($"Expected return type for rule '{name}'.");
                break;
            }

            SkipWhiteSpace(source, ref cursor);
            if (!Consume(source, ref cursor, '{'))
            {
                diagnostics.Add($"Expected '{{' before rule '{name}' body.");
                break;
            }

            var body = ReadUntilMatching(source, ref cursor, '{', '}');
            if (body == null)
            {
                diagnostics.Add($"Rule '{name}' has an unterminated body.");
                break;
            }

            if (!names.Add(name))
                diagnostics.Add($"Duplicate rule declaration '{name}'.");

            var parameters = ParseParameters(name, parametersText, diagnostics);
            var returnType = ParseType(returnTypeName, diagnostics, $"rule '{name}' return type");
            if (returnType != null)
                rules.Add(new RuleDeclarationModel(name, parameters, returnType, body.Trim()));

            position = cursor;
        }

        if (rules.Count == 0 && diagnostics.Count == 0)
            diagnostics.Add("No rule declarations were found.");

        return diagnostics.Count == 0
            ? RuleDeclarationExtractionResult.Success(rules)
            : new RuleDeclarationExtractionResult(false, rules, diagnostics);
    }

    private static IReadOnlyList<RuleParameterModel> ParseParameters(
        string ruleName,
        string parametersText,
        List<string> diagnostics)
    {
        var parameters = new List<RuleParameterModel>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(parametersText))
            return parameters;

        foreach (var rawParameter in parametersText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = rawParameter.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                diagnostics.Add($"Invalid parameter declaration '{rawParameter}' in rule '{ruleName}'. Expected 'name: type'.");
                continue;
            }

            if (!names.Add(parts[0]))
            {
                diagnostics.Add($"Duplicate parameter '{parts[0]}' in rule '{ruleName}'.");
                continue;
            }

            var type = ParseType(parts[1], diagnostics, $"parameter '{parts[0]}' in rule '{ruleName}'");
            if (type != null)
                parameters.Add(new RuleParameterModel(parts[0], type));
        }

        return parameters;
    }

    private static RuleTypeDescriptor? ParseType(string typeName, List<string> diagnostics, string owner)
    {
        if (string.Equals(typeName, "number", StringComparison.Ordinal) || string.Equals(typeName, "bool", StringComparison.Ordinal))
            return new RuleTypeDescriptor(typeName);

        diagnostics.Add($"Unsupported type '{typeName}' for {owner}. Supported MVP types: number, bool.");
        return null;
    }

    private static bool TryFindKeyword(string source, string keyword, int start, out int index)
    {
        index = source.IndexOf(keyword, start, StringComparison.Ordinal);
        while (index >= 0)
        {
            var beforeOk = index == 0 || !IsIdentifierChar(source[index - 1]);
            var after = index + keyword.Length;
            var afterOk = after >= source.Length || !IsIdentifierChar(source[after]);
            if (beforeOk && afterOk)
                return true;

            index = source.IndexOf(keyword, index + keyword.Length, StringComparison.Ordinal);
        }

        return false;
    }

    private static void SkipWhiteSpace(string source, ref int cursor)
    {
        while (cursor < source.Length && char.IsWhiteSpace(source[cursor]))
            cursor++;
    }

    private static string ReadIdentifier(string source, ref int cursor)
    {
        var start = cursor;
        while (cursor < source.Length && IsIdentifierChar(source[cursor]))
            cursor++;

        return source[start..cursor];
    }

    private static bool Consume(string source, ref int cursor, char expected)
    {
        if (cursor >= source.Length || source[cursor] != expected)
            return false;

        cursor++;
        return true;
    }

    private static bool ConsumeArrow(string source, ref int cursor)
    {
        if (cursor + 1 >= source.Length || source[cursor] != '-' || source[cursor + 1] != '>')
            return false;

        cursor += 2;
        return true;
    }

    private static string? ReadUntilMatching(string source, ref int cursor, char opening, char closing)
    {
        var start = cursor;
        var depth = 1;
        var builder = new StringBuilder();

        while (cursor < source.Length)
        {
            var current = source[cursor++];
            if (current == opening)
                depth++;
            else if (current == closing)
            {
                depth--;
                if (depth == 0)
                    return builder.ToString();
            }

            builder.Append(current);
        }

        cursor = start;
        return null;
    }

    private static bool IsIdentifierChar(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }
}
