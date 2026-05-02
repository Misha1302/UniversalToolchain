namespace BasicCore.Execution;

public interface IRuntimeContextStore
{
    TContext GetOrCreate<TContext>(
        RuntimeContextKey key,
        Func<TContext> factory)
        where TContext : class;
}