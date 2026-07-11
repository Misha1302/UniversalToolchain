using System.Runtime.CompilerServices;
using BasicCilCompiler.Execution;
using DynamicMethodCalling.Core;

namespace UniversalToolchain.Wist;

public sealed class WistFunc<TArg0, TResult>
{
    private readonly Func<TArg0, TResult> _invoke;

    internal WistFunc(CilCompilationOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (output.HasConstantPool)
        {
            _invoke = (Func<TArg0, TResult>)output.Method.CreateDelegate(
                typeof(Func<TArg0, TResult>),
                output.ConstantPool);
            return;
        }

        var invoker = new DynamicMethodInvoker<TArg0, TResult>(output.Method);
        _invoke = invoker.Invoke;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Invoke(TArg0 arg0) => _invoke(arg0);
}

public sealed class WistFunc<TArg0, TArg1, TResult>
{
    private readonly Func<TArg0, TArg1, TResult> _invoke;

    internal WistFunc(CilCompilationOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (output.HasConstantPool)
        {
            _invoke = (Func<TArg0, TArg1, TResult>)output.Method.CreateDelegate(
                typeof(Func<TArg0, TArg1, TResult>),
                output.ConstantPool);
            return;
        }

        var invoker = new DynamicMethodInvoker<TArg0, TArg1, TResult>(output.Method);
        _invoke = invoker.Invoke;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Invoke(TArg0 arg0, TArg1 arg1) => _invoke(arg0, arg1);
}

public sealed class WistFunc<TArg0, TArg1, TArg2, TResult>
{
    private readonly Func<TArg0, TArg1, TArg2, TResult> _invoke;

    internal WistFunc(CilCompilationOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (output.HasConstantPool)
        {
            _invoke = (Func<TArg0, TArg1, TArg2, TResult>)output.Method.CreateDelegate(
                typeof(Func<TArg0, TArg1, TArg2, TResult>),
                output.ConstantPool);
            return;
        }

        var invoker = new DynamicMethodInvoker<TArg0, TArg1, TArg2, TResult>(output.Method);
        _invoke = invoker.Invoke;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Invoke(TArg0 arg0, TArg1 arg1, TArg2 arg2) => _invoke(arg0, arg1, arg2);
}
