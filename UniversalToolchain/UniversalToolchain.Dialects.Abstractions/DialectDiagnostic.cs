namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Represents one validation or planning diagnostic for the dialect subsystem.
/// </summary>
public sealed record DialectDiagnostic
{
    public DialectDiagnostic(string code, string message, DialectDiagnosticSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(code))
            Thrower.Argument(nameof(code), "Diagnostic code must not be empty.");

        if (string.IsNullOrWhiteSpace(message))
            Thrower.Argument(nameof(message), "Diagnostic message must not be empty.");

        if (!Enum.IsDefined(severity))
            Thrower.Argument(nameof(severity), "Diagnostic severity is not defined.");

        Code = code;
        Message = message;
        Severity = severity;
    }

    public string Code { get; }

    public string Message { get; }

    public DialectDiagnosticSeverity Severity { get; }
}