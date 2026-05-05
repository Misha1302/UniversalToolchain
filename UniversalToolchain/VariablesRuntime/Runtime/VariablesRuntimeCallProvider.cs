namespace VariablesRuntime.Runtime;

public sealed class VariablesRuntimeCallProvider
{
    private static readonly RuntimeContextKey _contextKey = new("Variables");

    private readonly IRuntimeContextStore _contextStore;

    public VariablesRuntimeCallProvider(IRuntimeContextStore contextStore)
    {
        _contextStore = contextStore.ArgNotNull();
    }

    public VariablesContext LoadVariablesContext()
    {
        return _contextStore.GetOrCreate(
            _contextKey,
            static () => new VariablesContext());
    }
}