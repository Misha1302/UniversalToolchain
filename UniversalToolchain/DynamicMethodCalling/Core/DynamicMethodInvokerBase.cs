using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ExceptionsManager;
using ObjectExtensions;

namespace DynamicMethodCalling;

public class DynamicMethodInvokerBase<TReturn>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly MethodInfo _getMethodDescriptorMethod;
    protected readonly nint FunctionPointer;

    static DynamicMethodInvokerBase()
    {
        _getMethodDescriptorMethod = typeof(DynamicMethod).GetMethod(
            "GetMethodDescriptor",
            BindingFlags.Instance | BindingFlags.NonPublic
        ).NotNull();
    }

    public DynamicMethodInvokerBase(DynamicMethod dynamicMethod)
    {
        dynamicMethod.NotNull();

        Thrower.AssertAlways(dynamicMethod.ReturnType == typeof(TReturn), $"Return type must be {typeof(TReturn)} but it is {dynamicMethod.ReturnType}");

        CompileMethod(dynamicMethod);
        FunctionPointer = GetFunctionPointerInternal(dynamicMethod);
    }

    private nint GetFunctionPointerInternal(DynamicMethod dynamicMethod)
    {
        var handle = (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(dynamicMethod, null)!;
        var functionPtr = handle.GetFunctionPointer();

        Thrower.AssertAlways(functionPtr != IntPtr.Zero);

        dynamicMethod.MakeImmortal();
        handle.MakeImmortal();
        functionPtr.MakeImmortal();

        return functionPtr;
    }

    private void CompileMethod(DynamicMethod dynamicMethod)
    {
        var handle = (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(dynamicMethod, null)!;
        RuntimeHelpers.PrepareMethod(handle);
    }
}