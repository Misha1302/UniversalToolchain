namespace VariablesModule.Runtime;

public sealed class VariablesRuntimeCallProvider
{
    private static readonly RuntimeContextKey ContextKey = new("Variables");

    private readonly IRuntimeContextStore _contextStore;

    public VariablesRuntimeCallProvider(IRuntimeContextStore contextStore)
    {
        _contextStore = contextStore.ArgNotNull();
    }

    public VariablesContext LoadVariablesContext()
    {
        return _contextStore.GetOrCreate(
            ContextKey,
            static () => new VariablesContext());
    }
}
