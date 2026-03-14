
namespace DynamicMethodCalling.Core;

public unsafe class DynamicMethodInvoker<TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke() => ((delegate*<TReturn>)FunctionPointer)();
}

public unsafe class DynamicMethodInvoker<TArg0, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0) => ((delegate*<TArg0, TReturn>)FunctionPointer)(a0);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1) => ((delegate*<TArg0, TArg1, TReturn>)FunctionPointer)(a0, a1);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TArg2, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1, TArg2 a2) => ((delegate*<TArg0, TArg1, TArg2, TReturn>)FunctionPointer)(a0, a1, a2);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TArg2, TArg3, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1, TArg2 a2, TArg3 a3) => ((delegate*<TArg0, TArg1, TArg2, TArg3, TReturn>)FunctionPointer)(a0, a1, a2, a3);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TArg2, TArg3, TArg4, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1, TArg2 a2, TArg3 a3, TArg4 a4) => ((delegate*<TArg0, TArg1, TArg2, TArg3, TArg4, TReturn>)FunctionPointer)(a0, a1, a2, a3, a4);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1, TArg2 a2, TArg3 a3, TArg4 a4, TArg5 a5) => ((delegate*<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TReturn>)FunctionPointer)(a0, a1, a2, a3, a4, a5);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1, TArg2 a2, TArg3 a3, TArg4 a4, TArg5 a5, TArg6 a6) => ((delegate*<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TReturn>)FunctionPointer)(a0, a1, a2, a3, a4, a5, a6);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1, TArg2 a2, TArg3 a3, TArg4 a4, TArg5 a5, TArg6 a6, TArg7 a7) => ((delegate*<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TReturn>)FunctionPointer)(a0, a1, a2, a3, a4, a5, a6, a7);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1, TArg2 a2, TArg3 a3, TArg4 a4, TArg5 a5, TArg6 a6, TArg7 a7, TArg8 a8) => ((delegate*<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TReturn>)FunctionPointer)(a0, a1, a2, a3, a4, a5, a6, a7, a8);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1, TArg2 a2, TArg3 a3, TArg4 a4, TArg5 a5, TArg6 a6, TArg7 a7, TArg8 a8, TArg9 a9) => ((delegate*<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TReturn>)FunctionPointer)(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9);
}

public unsafe class DynamicMethodInvoker<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TReturn>(DynamicMethod dynamicMethod) : DynamicMethodInvokerBase<TReturn>(dynamicMethod)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TReturn Invoke(TArg0 a0, TArg1 a1, TArg2 a2, TArg3 a3, TArg4 a4, TArg5 a5, TArg6 a6, TArg7 a7, TArg8 a8, TArg9 a9, TArg10 a10) => ((delegate*<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TReturn>)FunctionPointer)(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10);
}