namespace DynamicMethodCalling;

internal sealed class ExecutionBoundNativePointerEnvironmentAdapter : IExecutionEnvironment
{
    private readonly IExecutionBoundArgument[]? _currentArguments;
    private readonly object?[]? _fallbackCurrentArguments;
    private readonly int _argumentCount;
    private readonly IExecutionEnvironment _innerEnvironment;
    private ExternalRuntimeCallProvider? _externalRuntimeCallProvider;

    internal ExecutionBoundNativePointerEnvironmentAdapter(IExecutionEnvironment innerEnvironment, int argumentCount)
    {
        _innerEnvironment = innerEnvironment.ArgNotNull();
        _argumentCount = argumentCount;
        _fallbackCurrentArguments = new object?[argumentCount];
    }

    internal ExecutionBoundNativePointerEnvironmentAdapter(
        IExecutionEnvironment innerEnvironment,
        IExecutionBoundArgument[] currentArguments)
    {
        _innerEnvironment = innerEnvironment.ArgNotNull();
        _currentArguments = currentArguments.ArgNotNull();
        _argumentCount = currentArguments.Length;
    }

    public object? GetExternalValue(int slot)
    {
        if ((uint)slot < (uint)_argumentCount)
        {
            if (_currentArguments is not null)
                return _currentArguments[slot].GetValue();

            return _fallbackCurrentArguments![slot];
        }

        return _innerEnvironment.GetExternalValue(slot);
    }

    public void SetExternalValue(int slot, object? value)
    {
        if ((uint)slot < (uint)_argumentCount)
        {
            if (_currentArguments is not null)
            {
                _currentArguments[slot].SetValue(value);
                return;
            }

            _fallbackCurrentArguments![slot] = value;
            return;
        }

        _innerEnvironment.SetExternalValue(slot, value);
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

    public void SetCurrentArgument(int slot, object? value)
    {
        if ((uint)slot >= (uint)_argumentCount)
            Thrower.ArgumentOutOfRange<object>(nameof(slot), $"Argument slot '{slot}' is out of range [0, {_argumentCount - 1}].");

        if (_currentArguments is not null)
        {
            _currentArguments[slot].SetValue(value);
            return;
        }

        _fallbackCurrentArguments![slot] = value;
    }
}

internal interface IExecutionBoundArgument
{
    object? GetValue();

    void SetValue(object? value);
}

internal sealed class ExecutionBoundArgument<T> : IExecutionBoundArgument
{
    public T Value { get; set; } = default!;

    public object? GetValue() => Value;

    public void SetValue(object? value)
    {
        Value = (T)value!;
    }
}
