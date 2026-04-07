namespace SettableGettableModule.Core;

internal static class VariablesContainerTestHooks
{
    private static readonly Lock _gate = new();
    private static readonly List<Action> _resetActions = [];

    internal static void RegisterReset(Action reset)
    {
        lock (_gate)
        {
            _resetActions.Add(reset);
        }
    }

    internal static void ResetAllForTests()
    {
        Action[] resetActions;

        lock (_gate)
        {
            resetActions = _resetActions.ToArray();
        }

        foreach (var reset in resetActions)
            reset();
    }
}
