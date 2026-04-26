using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules.Syntax;

public sealed class WistRuleSetSyntaxParser
{
    public WistRuleSetSyntaxParseResult Parse(string source)
    {
        source = source.ArgNotNull();

        if (string.IsNullOrWhiteSpace(source))
        {
            return new WistRuleSetSyntaxParseResult(false, null,
            [
                CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, "Rule source must not be empty.")
            ]);
        }

        var cursor = new Cursor(source);
        var diagnostics = new List<ToolchainDiagnostic>();
        var rules = new List<WistRuleDeclarationSyntax>();

        cursor.SkipTrivia();
        while (!cursor.End)
        {
            var rule = ParseRule(cursor, diagnostics);
            if (rule != null)
                rules.Add(rule);

            if (diagnostics.Count > 0)
                break;

            cursor.SkipTrivia();
        }

        if (rules.Count == 0 && diagnostics.Count == 0)
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, "No rule declarations were found."));

        return diagnostics.Count == 0
            ? new WistRuleSetSyntaxParseResult(true, new WistRuleSetSyntax(rules), [])
            : new WistRuleSetSyntaxParseResult(false, new WistRuleSetSyntax(rules), diagnostics);
    }

    private static WistRuleDeclarationSyntax? ParseRule(Cursor cursor, List<ToolchainDiagnostic> diagnostics)
    {
        var start = cursor.Position;
        if (!cursor.TryReadKeyword("rule"))
        {
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected 'rule' keyword at offset {cursor.Position}."));
            return null;
        }

        cursor.SkipTrivia();
        if (!cursor.TryReadIdentifier(out var ruleName, out var ruleNameSpan))
        {
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected rule name at offset {cursor.Position}."));
            return null;
        }

        cursor.SkipTrivia();
        if (!cursor.TryConsume('('))
        {
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected '(' after rule '{ruleName}'."));
            return null;
        }

        var parameters = ParseParameters(cursor, ruleName, diagnostics);
        if (diagnostics.Count > 0)
            return null;

        cursor.SkipTrivia();
        if (!cursor.TryConsume('-') || !cursor.TryConsume('>'))
        {
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected '->' after rule '{ruleName}' parameters."));
            return null;
        }

        cursor.SkipTrivia();
        if (!cursor.TryReadIdentifier(out var returnTypeName, out var returnTypeSpan))
        {
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected return type for rule '{ruleName}'."));
            return null;
        }

        cursor.SkipTrivia();
        if (!cursor.TryConsume('{'))
        {
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected '{{' before rule '{ruleName}' body."));
            return null;
        }

        var bodyStart = cursor.Position;
        if (!cursor.TryReadBody(out var bodyText, out var bodyEndExclusive))
        {
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Rule '{ruleName}' has an unterminated body."));
            return null;
        }

        return new WistRuleDeclarationSyntax(
            ruleName,
            parameters,
            new WistRuleTypeSyntax(returnTypeName, returnTypeSpan),
            bodyText,
            new SourceSpan(start, bodyEndExclusive - start + 1),
            new SourceSpan(bodyStart, bodyEndExclusive - bodyStart));
    }

    private static IReadOnlyList<WistRuleParameterSyntax> ParseParameters(Cursor cursor, string ruleName, List<ToolchainDiagnostic> diagnostics)
    {
        var parameters = new List<WistRuleParameterSyntax>();
        cursor.SkipTrivia();

        if (cursor.TryConsume(')'))
            return parameters;

        while (true)
        {
            cursor.SkipTrivia();
            var parameterStart = cursor.Position;
            if (!cursor.TryReadIdentifier(out var parameterName, out _))
            {
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected parameter name in rule '{ruleName}'."));
                return parameters;
            }

            cursor.SkipTrivia();
            if (!cursor.TryConsume(':'))
            {
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected ':' after parameter '{parameterName}' in rule '{ruleName}'."));
                return parameters;
            }

            cursor.SkipTrivia();
            if (!cursor.TryReadIdentifier(out var typeName, out var typeSpan))
            {
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected type name for parameter '{parameterName}' in rule '{ruleName}'."));
                return parameters;
            }

            parameters.Add(new WistRuleParameterSyntax(
                parameterName,
                new WistRuleTypeSyntax(typeName, typeSpan),
                new SourceSpan(parameterStart, cursor.Position - parameterStart)));

            cursor.SkipTrivia();
            if (cursor.TryConsume(')'))
                return parameters;

            if (!cursor.TryConsume(','))
            {
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleInvalidBody, $"Expected ',' or ')' in parameter list of rule '{ruleName}'."));
                return parameters;
            }
        }
    }

    private static ToolchainDiagnostic CreateDiagnostic(string code, string message)
    {
        return new ToolchainDiagnostic(code, ToolchainDiagnosticSeverity.Error, message, null, []);
    }

    private sealed class Cursor
    {
        private readonly string _source;

        public Cursor(string source)
        {
            _source = source;
        }

        public int Position { get; private set; }

        public bool End => Position >= _source.Length;

        public bool TryConsume(char expected)
        {
            if (End || _source[Position] != expected)
                return false;

            Position++;
            return true;
        }

        public bool TryReadIdentifier(out string identifier, out SourceSpan span)
        {
            var start = Position;
            if (End || !(char.IsLetter(_source[Position]) || _source[Position] == '_'))
            {
                identifier = string.Empty;
                span = default;
                return false;
            }

            Position++;
            while (!End && (char.IsLetterOrDigit(_source[Position]) || _source[Position] == '_'))
                Position++;

            identifier = _source[start..Position];
            span = new SourceSpan(start, Position - start);
            return true;
        }

        public bool TryReadKeyword(string keyword)
        {
            if (Position + keyword.Length > _source.Length)
                return false;

            if (!string.Equals(_source.Substring(Position, keyword.Length), keyword, StringComparison.Ordinal))
                return false;

            var beforeOk = Position == 0 || !IsIdentifierChar(_source[Position - 1]);
            var afterIndex = Position + keyword.Length;
            var afterOk = afterIndex >= _source.Length || !IsIdentifierChar(_source[afterIndex]);
            if (!beforeOk || !afterOk)
                return false;

            Position += keyword.Length;
            return true;
        }

        public void SkipTrivia()
        {
            while (!End)
            {
                if (char.IsWhiteSpace(_source[Position]))
                {
                    Position++;
                    continue;
                }

                if (Position + 1 < _source.Length && _source[Position] == '/' && _source[Position + 1] == '/')
                {
                    Position += 2;
                    while (!End && _source[Position] != '\n')
                        Position++;
                    continue;
                }

                if (Position + 1 < _source.Length && _source[Position] == '/' && _source[Position + 1] == '*')
                {
                    Position += 2;
                    while (!End && !(Position + 1 < _source.Length && _source[Position] == '*' && _source[Position + 1] == '/'))
                        Position++;

                    if (Position + 1 < _source.Length)
                        Position += 2;

                    continue;
                }

                break;
            }
        }

        public bool TryReadBody(out string bodyText, out int bodyEndExclusive)
        {
            var start = Position;
            var depth = 1;
            var inString = false;

            while (!End)
            {
                var current = _source[Position];

                if (inString)
                {
                    if (current == '\\' && Position + 1 < _source.Length)
                    {
                        Position += 2;
                        continue;
                    }

                    if (current == '"')
                        inString = false;

                    Position++;
                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    Position++;
                    continue;
                }

                if (Position + 1 < _source.Length && current == '/' && _source[Position + 1] == '/')
                {
                    Position += 2;
                    while (!End && _source[Position] != '\n')
                        Position++;
                    continue;
                }

                if (Position + 1 < _source.Length && current == '/' && _source[Position + 1] == '*')
                {
                    Position += 2;
                    while (!End && !(Position + 1 < _source.Length && _source[Position] == '*' && _source[Position + 1] == '/'))
                        Position++;

                    if (Position + 1 < _source.Length)
                        Position += 2;

                    continue;
                }

                if (current == '{')
                {
                    depth++;
                    Position++;
                    continue;
                }

                if (current == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        bodyText = _source[start..Position];
                        bodyEndExclusive = Position;
                        Position++;
                        return true;
                    }

                    Position++;
                    continue;
                }

                Position++;
            }

            bodyText = string.Empty;
            bodyEndExclusive = start;
            return false;
        }

        private static bool IsIdentifierChar(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }
    }
}
