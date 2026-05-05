namespace UniversalToolchain.Wist;

/// <summary>
///     Selects the Wist execution backend exposed by the public facade.
/// </summary>
public enum WistBackend
{
    /// <summary>
    ///     Uses the CIL compiler backend. This is the default for hot compiled functions.
    /// </summary>
    Compiler,

    /// <summary>
    ///     Uses the interpreter backend. This is useful for reference execution and diagnostics.
    /// </summary>
    Interpreter
}