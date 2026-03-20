namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Stable backend identifiers used by the Wist runtime integration.
/// </summary>
public static class WistDialectBackendIds
{
    public static DialectBackendId Cil { get; } = new("cil");

    public static DialectBackendId Interpreter { get; } = new("interpreter");
}
