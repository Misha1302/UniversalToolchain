namespace DynamicMethodCalling;

/// <summary>
///     Creates and caches native delegates for a single <see cref="DynamicMethod" />.
/// </summary>
public sealed class NativeDelegateInvoker : INativeDelegateInvoker
{
    private readonly ConcurrentDictionary<Type, Delegate> _delegateCache = new();
    private readonly DynamicMethod _dynamicMethod;

    public NativeDelegateInvoker(DynamicMethod dynamicMethod)
    {
        dynamicMethod = dynamicMethod.ArgNotNull();

        _dynamicMethod = dynamicMethod;
    }

    public TDelegate GetDelegate<TDelegate>() where TDelegate : Delegate => (TDelegate)GetDelegate(typeof(TDelegate));

    public Delegate GetDelegate(Type delegateType)
    {
        delegateType = delegateType.ArgNotNull();

        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            Thrower.Argument(nameof(delegateType), $"Type '{delegateType}' must be a delegate type.");

        return _delegateCache.GetOrAdd(delegateType, CreateDelegate);
    }

    private Delegate CreateDelegate(Type delegateType) => _dynamicMethod.CreateDelegate(delegateType);
}