using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DynamicMethodCalling.Core;

namespace UniversalToolchain.Wist;

/// <summary>
///     Represents a compiled one-argument Wist function with typed fast invocation.
/// </summary>
public sealed class WistFunc<TArg0, TResult>
{
    private readonly DynamicMethodInvoker<TArg0, TResult> _invoker;

    internal WistFunc(DynamicMethod dynamicMethod)
    {
        _invoker = new DynamicMethodInvoker<TArg0, TResult>(dynamicMethod);
    }

    /// <summary>
    ///     Invokes the compiled function without dictionary, reflection, or session overhead.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Invoke(TArg0 arg0) => _invoker.Invoke(arg0);
}

/// <summary>
///     Represents a compiled two-argument Wist function with typed fast invocation.
/// </summary>
public sealed class WistFunc<TArg0, TArg1, TResult>
{
    private readonly DynamicMethodInvoker<TArg0, TArg1, TResult> _invoker;

    internal WistFunc(DynamicMethod dynamicMethod)
    {
        _invoker = new DynamicMethodInvoker<TArg0, TArg1, TResult>(dynamicMethod);
    }

    /// <summary>
    ///     Invokes the compiled function without dictionary, reflection, or session overhead.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Invoke(TArg0 arg0, TArg1 arg1) => _invoker.Invoke(arg0, arg1);
}

/// <summary>
///     Represents a compiled three-argument Wist function with typed fast invocation.
/// </summary>
public sealed class WistFunc<TArg0, TArg1, TArg2, TResult>
{
    private readonly DynamicMethodInvoker<TArg0, TArg1, TArg2, TResult> _invoker;

    internal WistFunc(DynamicMethod dynamicMethod)
    {
        _invoker = new DynamicMethodInvoker<TArg0, TArg1, TArg2, TResult>(dynamicMethod);
    }

    /// <summary>
    ///     Invokes the compiled function without dictionary, reflection, or session overhead.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Invoke(TArg0 arg0, TArg1 arg1, TArg2 arg2) => _invoker.Invoke(arg0, arg1, arg2);
}
