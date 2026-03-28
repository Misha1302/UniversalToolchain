namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
/// Specifies the backend scope targeted by parser directives.
/// </summary>
public enum DialectBackendTarget
{
    Interpreter = 0,
    Cil = 1,
    Any = 2
}
