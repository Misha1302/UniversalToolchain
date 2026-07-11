using CommonExceptions;

namespace UniversalToolchain.Wist;

/// <summary>
///     Reports a facade preflight resource limit violation.
/// </summary>
public sealed class WistResourceLimitException : WistException
{
    internal WistResourceLimitException(string diagnosticCode, string message)
        : base(message)
    {
        DiagnosticCode = diagnosticCode;
        Stage = "Policy";
    }

    public string DiagnosticCode { get; }
}
