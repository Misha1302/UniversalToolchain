namespace UniversalToolchain.Wist;

/// <summary>
///     Stable diagnostic codes produced by the Wist public facade.
/// </summary>
public static class WistDiagnosticCodes
{
    public const string SourceLimitExceeded = "UTC-WIST-001";
    public const string ParameterLimitExceeded = "UTC-WIST-002";
    public const string LexerFailure = "UTC-WIST-LEX-001";
    public const string ParserFailure = "UTC-WIST-PARSE-001";
    public const string DialectFailure = "UTC-WIST-DIALECT-001";
    public const string TypeResolutionFailure = "UTC-WIST-RESOLVE-001";
    public const string AmbiguousResolution = "UTC-WIST-RESOLVE-002";
    public const string CompilationFailure = "UTC-WIST-COMPILE-001";
    public const string SsaRouteFailure = "UTC-WIST-SSA-001";
    public const string ExecutionFailure = "UTC-WIST-EXEC-001";
    public const string ValidationFailure = "UTC-WIST-VALIDATE-001";
    public const string UnexpectedFailure = "UTC-WIST-999";
}
