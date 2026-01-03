using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace DynamicMethodCalling;

/// <summary>
///     Обертка для максимально быстрого вызова динамических методов.
///     Использует нестандартные подходы для обхода ограничений DynamicMethod
/// </summary>
public unsafe class DynamicMethodInvoker<TReturn>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly MethodInfo _getMethodDescriptorMethod;
    private readonly delegate*<TReturn> _functionPointer;

    static DynamicMethodInvoker()
    {
        _getMethodDescriptorMethod = typeof(DynamicMethod).GetMethod(
            "GetMethodDescriptor",
            BindingFlags.Instance | BindingFlags.NonPublic
        ).NotNull();
    }

    public DynamicMethodInvoker(DynamicMethod dynamicMethod)
    {
        dynamicMethod.NotNull();
        ForceAggressiveJitCompilation(dynamicMethod);
        _functionPointer = GetFunctionPointerInternal(dynamicMethod);
    }

    public TReturn Invoke() => _functionPointer();

    /// <summary>
    ///     Получаем указатель на функцию через нестандартные методы
    /// </summary>
    private delegate*<TReturn> GetFunctionPointerInternal(DynamicMethod dynamicMethod)
    {
        var handle = (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(dynamicMethod, null)!;
        var functionPtr = handle.GetFunctionPointer();

        Thrower.AssertAlways(functionPtr != IntPtr.Zero);

        return (delegate* <TReturn>)functionPtr;
    }


    private void ForceAggressiveJitCompilation(DynamicMethod dynamicMethod)
    {
        var handle = (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(dynamicMethod, null)!;

        // TODO: check for usefulness
        for (var i = 0; i < 1000; i++)
        {
            RuntimeHelpers.PrepareMethod(handle);
        }
    }
}