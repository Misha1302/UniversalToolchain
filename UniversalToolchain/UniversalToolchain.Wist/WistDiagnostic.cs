namespace UniversalToolchain.Wist;

/// <summary>
///     Stable public diagnostic emitted by the Wist facade.
/// </summary>
public sealed record WistDiagnostic(
    string Code,
    WistDiagnosticSeverity Severity,
    string Stage,
    string SourceName,
    string Message,
    WistSourceSpan? Span,
    IReadOnlyList<WistDiagnosticHint> Hints);
