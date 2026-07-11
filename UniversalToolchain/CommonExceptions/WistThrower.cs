namespace CommonExceptions;

[Obsolete("Use ToolchainThrower in framework/runtime layers. WistThrower remains only as a compatibility alias.")]
public static class WistThrower
{
    public static void Lexer(string message, SourceLocation location) => ToolchainThrower.Lexer(message, location);

    public static void Parser(string message) => ToolchainThrower.Parser(message);

    public static void Parser(string message, SourceLocation location) => ToolchainThrower.Parser(message, location);

    public static void Import(string message) => ToolchainThrower.Import(message);

    public static void Runtime(string message, Exception inner) => ToolchainThrower.Runtime(message, inner);

    public static void InternalCompiler(string message) => ToolchainThrower.InternalCompiler(message);
}
