namespace DynamicMethodCalling;

internal sealed class ExecutionBoundNativePointerEnvironmentAdapter : IExecutionEnvironment
{
    private readonly IExecutionEnvironment _innerEnvironment;
    private readonly object?[] _currentArguments;
    private ExternalRuntimeCallProvider? _externalRuntimeCallProvider;

    public ExecutionBoundNativePointerEnvironmentAdapter(IExecutionEnvironment innerEnvironment, int argumentCount)
    {
        _innerEnvironment = innerEnvironment.ArgNotNull();
        _currentArguments = new object?[argumentCount];
    }

    public object? GetExternalValue(int slot)
    {
        if ((uint)slot < (uint)_currentArguments.Length)
            return _currentArguments[slot];

        return _innerEnvironment.GetExternalValue(slot);
    }

    public void SetExternalValue(int slot, object? value)
    {
        if ((uint)slot < (uint)_currentArguments.Length)
        {
            _currentArguments[slot] = value;
            return;
        }

        _innerEnvironment.SetExternalValue(slot, value);
    }

    public void SetCurrentArgument(int slot, object? value)
    {
        if ((uint)slot >= (uint)_currentArguments.Length)
            Thrower.ArgumentOutOfRange<object>(nameof(slot), $"Argument slot '{slot}' is out of range [0, {_currentArguments.Length - 1}].");

        _currentArguments[slot] = value;
    }

    public TContext GetOrCreate<TContext>(RuntimeContextKey key, Func<TContext> factory) where TContext : class =>
        _innerEnvironment.GetOrCreate(key, factory);

    public object GetRequiredProvider(Type providerType)
    {
        providerType = providerType.ArgNotNull();

        if (providerType == typeof(ExternalRuntimeCallProvider))
            return _externalRuntimeCallProvider ??= new ExternalRuntimeCallProvider(this);

        return _innerEnvironment.GetRequiredProvider(providerType);
    }
}
