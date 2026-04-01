namespace DynamicMethodCalling;

/// <summary>
/// Creates and caches native delegates for a single <see cref="DynamicMethod"/>.
/// </summary>
public sealed class NativeDelegateInvoker : INativeDelegateInvoker
{
    private readonly DynamicMethod _dynamicMethod;
    private readonly ConcurrentDictionary<Type, Delegate> _delegateCache = new();

    public NativeDelegateInvoker(DynamicMethod dynamicMethod)
    {
        if (dynamicMethod is null)
            Thrower.ArgumentNull(nameof(dynamicMethod));

        _dynamicMethod = dynamicMethod;
    }

    public TDelegate GetDelegate<TDelegate>() where TDelegate : Delegate
    {
        return (TDelegate)GetDelegate(typeof(TDelegate));
    }

    public Delegate GetDelegate(Type delegateType)
    {
        if (delegateType is null)
            Thrower.ArgumentNull(nameof(delegateType));

        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            Thrower.Argument(nameof(delegateType), $"Type '{delegateType}' must be a delegate type.");

        return _delegateCache.GetOrAdd(delegateType, CreateDelegate);
    }

    private Delegate CreateDelegate(Type delegateType)
    {
        return _dynamicMethod.CreateDelegate(delegateType);
    }
}
