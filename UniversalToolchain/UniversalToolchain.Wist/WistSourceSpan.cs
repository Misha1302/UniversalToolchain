namespace UniversalToolchain.Wist;

/// <summary>
///     Source location associated with a public Wist diagnostic.
/// </summary>
public sealed record WistSourceSpan(
    string SourceName,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
