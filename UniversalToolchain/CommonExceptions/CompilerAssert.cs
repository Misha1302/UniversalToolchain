namespace CommonExceptions;

public static class CompilerAssert
{
    public static void Unreachable(string message)
    {
        WistThrower.InternalCompiler($"Unreachable code reached: {message}");
    }
}
