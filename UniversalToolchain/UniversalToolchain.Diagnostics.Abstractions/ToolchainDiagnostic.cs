namespace UniversalToolchain.Diagnostics.Abstractions;

public sealed record ToolchainDiagnostic(
    string Code,
    ToolchainDiagnosticSeverity Severity,
    string Message,
    SourceSpan? Span,
    IReadOnlyList<ToolchainDiagnosticHint> Hints);