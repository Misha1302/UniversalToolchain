using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace DynamicMethodCalling;

public class DynamicMethodInvokerBase
{
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
        CompileMethod(dynamicMethod);
        FunctionPointer = GetFunctionPointerInternal(dynamicMethod);
    }

    private nint GetFunctionPointerInternal(DynamicMethod dynamicMethod)
    {
        var handle = (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(dynamicMethod, null)!;
        var functionPtr = handle.GetFunctionPointer();

        Thrower.AssertAlways(functionPtr != IntPtr.Zero);

        return functionPtr;
    }

    private void CompileMethod(DynamicMethod dynamicMethod)
    {
        var handle = (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(dynamicMethod, null)!;
        RuntimeHelpers.PrepareMethod(handle);
    }
}