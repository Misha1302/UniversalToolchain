namespace UniversalToolchain.Rules.Abstractions;

public sealed record RuleDiagnostic(
    string Code,
    RuleDiagnosticSeverity Severity,
    string Message,
    SourceSpan? Span,
    IReadOnlyList<RuleDiagnosticHint> Hints);
