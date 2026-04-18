using SettableGettableModule.Core;

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
        // Reset only mutable global test hooks so each test observes a clean runtime state.
        VariablesContainerTestHooks.ResetAllForTests();
    }
}