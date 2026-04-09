using DotnetAirHelper;
using SettableGettableModule.Core;

namespace Tests.Infrastructure;

internal sealed class GlobalTestStateScope : IDisposable
{
    private GlobalTestStateScope()
    {
        Reset();
    }

    public static GlobalTestStateScope Create() => new();

    public void Dispose()
    {
        Reset();
    }

    private static void Reset()
    {
        // Reset only mutable global test hooks so each test observes a clean compatibility/runtime state.
        AirTypes.ResetToDefaultsForTests();
        VariablesContainerTestHooks.ResetAllForTests();
    }
}
