namespace UniversalToolchain.Dialects.Parsing;

internal sealed class ParserState
{
    private readonly IReadOnlyList<DialectToken> _tokens;
    private int _index;

    public ParserState(IReadOnlyList<DialectToken> tokens, IList<DialectDiagnostic> diagnostics)
    {
        _tokens = tokens;
        Diagnostics = diagnostics;
    }

    public IList<DialectDiagnostic> Diagnostics { get; }

    public DialectToken Current => _tokens[_index];

    public DialectToken PreviousToken { get; private set; }

    public bool IsEnd => Current.Kind == DialectTokenKind.EndOfInput;

    public void SkipNewLines()
    {
        while (Current.Kind == DialectTokenKind.NewLine)
            Advance();
    }

    public bool MatchKeyword(string keyword)
    {
        if (Current.Kind != DialectTokenKind.Identifier)
            return false;

        if (!string.Equals(Current.Text, keyword, StringComparison.OrdinalIgnoreCase))
            return false;

        Advance();
        return true;
    }

    public void ExpectKeyword(string keyword)
    {
        if (!MatchKeyword(keyword))
            AddError("P200", $"Expected keyword '{keyword}' at line {Current.Line}, column {Current.Column}.");
    }

    public string ExpectIdentifier()
    {
        if (Current.Kind == DialectTokenKind.Identifier)
        {
            var text = Current.Text;
            Advance();
            return text;
        }

        AddError("P201", $"Expected identifier at line {Current.Line}, column {Current.Column}.");
        return string.Empty;
    }

    public string ExpectString()
    {
        if (Current.Kind == DialectTokenKind.StringLiteral)
        {
            var text = Current.Text;
            Advance();
            return text;
        }

        AddError("P202", $"Expected string literal at line {Current.Line}, column {Current.Column}.");
        return string.Empty;
    }

    public void ExpectArrow()
    {
        if (Current.Kind == DialectTokenKind.Arrow)
        {
            Advance();
            return;
        }

        AddError("P203", $"Expected '->' at line {Current.Line}, column {Current.Column}.");
    }

    public void ExpectEquals()
    {
        if (Current.Kind == DialectTokenKind.Equals)
        {
            Advance();
            return;
        }

        AddError("P204", $"Expected '=' at line {Current.Line}, column {Current.Column}.");
    }

    public bool ExpectEnableDisable()
    {
        if (MatchKeyword("enable"))
            return true;

        if (MatchKeyword("disable"))
            return false;

        AddError("P205", $"Expected 'enable' or 'disable' at line {Current.Line}, column {Current.Column}.");
        return false;
    }

    public bool ExpectBoolean()
    {
        if (MatchKeyword("true"))
            return true;

        if (MatchKeyword("false"))
            return false;

        AddError("P206", $"Expected 'true' or 'false' at line {Current.Line}, column {Current.Column}.");
        return false;
    }

    public DialectBackendId ExpectBackendId()
    {
        if (Current.Kind == DialectTokenKind.Identifier)
        {
            var value = Current.Text;
            Advance();
            return new DialectBackendId(value);
        }

        AddError("P207", $"Expected backend identifier at line {Current.Line}, column {Current.Column}.");
        return new DialectBackendId("invalid-backend");
    }

    public DialectBackendSelector ExpectBackendSelector(bool allowAny)
    {
        if (Current.Kind == DialectTokenKind.Identifier)
        {
            var value = Current.Text;
            Advance();

            if (DialectBackendSelectorText.TryParseSelector(value, allowAny, out var selector))
                return selector;
        }

        AddError(
            "P207",
            allowAny
                ? $"Expected backend identifier or wildcard 'any' at line {Current.Line}, column {Current.Column}."
                : $"Expected backend identifier at line {Current.Line}, column {Current.Column}.");
        return allowAny ? DialectBackendSelector.Any : DialectBackendSelector.For(new DialectBackendId("invalid-backend"));
    }

    public SecurityProfile ExpectSecurityProfile()
    {
        if (MatchKeyword("trusted"))
            return SecurityProfile.Trusted;

        if (MatchKeyword("restricted"))
            return SecurityProfile.Restricted;

        AddError("P208", $"Expected security profile 'trusted' or 'restricted' at line {Current.Line}, column {Current.Column}.");
        return SecurityProfile.Restricted;
    }

    public void ExpectLineEnd()
    {
        if (Current.Kind is DialectTokenKind.NewLine or DialectTokenKind.EndOfInput)
        {
            SkipNewLines();
            return;
        }

        AddError("P209", $"Expected end of directive at line {Current.Line}, column {Current.Column}.");
        SkipLine();
    }

    public void SkipLine()
    {
        while (Current.Kind is not DialectTokenKind.NewLine and not DialectTokenKind.EndOfInput)
            Advance();

        SkipNewLines();
    }

    public void AddError(string code, string message)
    {
        Diagnostics.Add(new DialectDiagnostic(code, message, DialectDiagnosticSeverity.Error));
    }

    private void Advance()
    {
        PreviousToken = Current;
        if (_index < _tokens.Count - 1)
            _index++;
    }
}