namespace DynamicMethodCalling;

/// <summary>
///     Provides cached access to delegates created from a native <see cref="DynamicMethod" />.
/// </summary>
public interface INativeDelegateInvoker
{
    /// <summary>
    ///     Gets a cached delegate instance for the requested delegate type.
    /// </summary>
    /// <typeparam name="TDelegate">Delegate signature type.</typeparam>
    /// <returns>Cached delegate instance.</returns>
    TDelegate GetDelegate<TDelegate>() where TDelegate : Delegate;

    /// <summary>
    ///     Gets a cached delegate instance for the requested delegate type.
    /// </summary>
    /// <param name="delegateType">Delegate signature type.</param>
    /// <returns>Cached delegate instance.</returns>
    Delegate GetDelegate(Type delegateType);
}