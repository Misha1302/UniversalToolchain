namespace CommonExceptions;

public static class CompilerAssert
{
    public static void Unreachable(string message)
    {
        ToolchainThrower.InternalCompiler($"Unreachable code reached: {message}");
    }
}