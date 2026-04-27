namespace UniversalToolchain.Diagnostics.Abstractions;

public sealed record SourceSpan(
    string SourceName,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
