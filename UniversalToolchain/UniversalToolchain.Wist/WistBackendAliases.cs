namespace UniversalToolchain.Wist;

internal static class WistBackendAliases
{
    public static string ToAlias(WistBackend backend)
    {
        return backend switch
        {
            WistBackend.Compiler => "compiler",
            WistBackend.Interpreter => "interpreter",
            _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unsupported Wist backend.")
        };
    }
}
