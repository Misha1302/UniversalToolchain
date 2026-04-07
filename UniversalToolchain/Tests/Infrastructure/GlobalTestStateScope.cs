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
        AirTypes.ResetToDefaultsForTests();
        VariablesContainerTestHooks.ResetAllForTests();
    }
}
