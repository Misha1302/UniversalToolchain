namespace Tests.Infrastructure;

internal sealed class GlobalTestStateScope : IDisposable
{
    private GlobalTestStateScope()
    {
        Reset();
    }

    public void Dispose()
    {
        Reset();
    }

    public static GlobalTestStateScope Create() => new();

    private static void Reset()
    {
        // Intentionally left blank. Global mutable runtime containers were removed.
    }
}