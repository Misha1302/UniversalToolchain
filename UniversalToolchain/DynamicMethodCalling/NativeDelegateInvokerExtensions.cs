namespace DynamicMethodCalling;

/// <summary>
///     Provides typed delegate and invoke helpers for <see cref="INativeDelegateInvoker" />.
/// </summary>
public static class NativeDelegateInvokerExtensions
{
    public static Func<TResult> AsFunc<TResult>(this INativeDelegateInvoker invoker)
    {
        invoker = invoker.ArgNotNull();

        return invoker.GetDelegate<Func<TResult>>();
    }

    public static Func<T1, TResult> AsFunc<T1, TResult>(this INativeDelegateInvoker invoker)
    {
        invoker = invoker.ArgNotNull();

        return invoker.GetDelegate<Func<T1, TResult>>();
    }

    public static Func<T1, T2, TResult> AsFunc<T1, T2, TResult>(this INativeDelegateInvoker invoker)
    {
        invoker = invoker.ArgNotNull();

        return invoker.GetDelegate<Func<T1, T2, TResult>>();
    }

    public static Func<T1, T2, T3, TResult> AsFunc<T1, T2, T3, TResult>(this INativeDelegateInvoker invoker)
    {
        invoker = invoker.ArgNotNull();

        return invoker.GetDelegate<Func<T1, T2, T3, TResult>>();
    }

    public static Func<T1, T2, T3, T4, TResult> AsFunc<T1, T2, T3, T4, TResult>(this INativeDelegateInvoker invoker)
    {
        invoker = invoker.ArgNotNull();

        return invoker.GetDelegate<Func<T1, T2, T3, T4, TResult>>();
    }

    public static TResult Invoke<TResult>(this INativeDelegateInvoker invoker)
    {
        invoker = invoker.ArgNotNull();

        return invoker.AsFunc<TResult>()();
    }

    public static TResult Invoke<T1, TResult>(this INativeDelegateInvoker invoker, T1 arg1)
    {
        invoker = invoker.ArgNotNull();

        return invoker.AsFunc<T1, TResult>()(arg1);
    }

    public static TResult Invoke<T1, T2, TResult>(this INativeDelegateInvoker invoker, T1 arg1, T2 arg2)
    {
        invoker = invoker.ArgNotNull();

        return invoker.AsFunc<T1, T2, TResult>()(arg1, arg2);
    }

    public static TResult Invoke<T1, T2, T3, TResult>(this INativeDelegateInvoker invoker, T1 arg1, T2 arg2, T3 arg3)
    {
        invoker = invoker.ArgNotNull();

        return invoker.AsFunc<T1, T2, T3, TResult>()(arg1, arg2, arg3);
    }

    public static TResult Invoke<T1, T2, T3, T4, TResult>(this INativeDelegateInvoker invoker, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        invoker = invoker.ArgNotNull();

        return invoker.AsFunc<T1, T2, T3, T4, TResult>()(arg1, arg2, arg3, arg4);
    }
}