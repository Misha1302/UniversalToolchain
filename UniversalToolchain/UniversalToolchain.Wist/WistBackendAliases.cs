using ExceptionsManager;
namespace UniversalToolchain.Wist;

internal static class WistBackendAliases
{
    public const string CompilerAlias = "compiler";
    public const string InterpreterAlias = "interpreter";

    public static string ToAlias(WistBackend backend)
    {
        return backend switch
        {
            WistBackend.Compiler => CompilerAlias,
            WistBackend.Interpreter => InterpreterAlias,
            _ => Thrower.Argument(nameof(backend), "Unsupported Wist backend.")
        };
    }
}
