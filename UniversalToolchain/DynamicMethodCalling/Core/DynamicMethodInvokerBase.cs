namespace DynamicMethodCalling.Core;

public class DynamicMethodInvokerBase<TReturn>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly MethodInfo _getMethodDescriptorMethod;

    // The raw function pointer is only valid while the DynamicMethod and its runtime handle
    // remain reachable. Keep both as instance-owned lifetime roots instead of placing every
    // compiled method in process-wide static storage.
    private readonly DynamicMethod _dynamicMethod;
    private readonly RuntimeMethodHandle _methodHandle;
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

        _dynamicMethod = dynamicMethod;
        _methodHandle = GetMethodHandle(dynamicMethod);

        RuntimeHelpers.PrepareMethod(_methodHandle);
        FunctionPointer = _methodHandle.GetFunctionPointer();

        Thrower.AssertAlways(FunctionPointer != IntPtr.Zero);
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

    private static RuntimeMethodHandle GetMethodHandle(DynamicMethod dynamicMethod) =>
        (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(dynamicMethod, null)!;
}
