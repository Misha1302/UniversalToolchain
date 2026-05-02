namespace DynamicMethodCalling.Core;

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

    public DynamicMethodInvokerBase(DynamicMethod dynamicMethod, IReadOnlyList<Type> expectedParameterTypes)
    {
        dynamicMethod = dynamicMethod.ArgNotNull();
        expectedParameterTypes = expectedParameterTypes.ArgNotNull();

        ValidateSignature(dynamicMethod, expectedParameterTypes);

        CompileMethod(dynamicMethod);
        FunctionPointer = GetFunctionPointerInternal(dynamicMethod);
    }

    private static void ValidateSignature(DynamicMethod dynamicMethod, IReadOnlyList<Type> expectedParameterTypes)
    {
        Thrower.AssertAlways(
            dynamicMethod.ReturnType == typeof(TReturn),
            $"Return type must be {typeof(TReturn)} but it is {dynamicMethod.ReturnType}.");

        var actualParameters = dynamicMethod.GetParameters();

        Thrower.AssertAlways(
            actualParameters.Length == expectedParameterTypes.Count,
            $"Dynamic method parameter count must be {expectedParameterTypes.Count} but it is {actualParameters.Length}.");

        for (var i = 0; i < expectedParameterTypes.Count; i++)
        {
            var actualType = actualParameters[i].ParameterType;
            var expectedType = expectedParameterTypes[i];

            Thrower.AssertAlways(
                actualType == expectedType,
                $"Dynamic method parameter {i} must be {expectedType} but it is {actualType}.");
        }
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