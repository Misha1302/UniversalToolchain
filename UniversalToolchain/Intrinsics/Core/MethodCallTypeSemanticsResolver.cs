using System.Reflection;
using DotnetHelper;

namespace BasicCore.Core;

/// <summary>
///     Resolves .NET method-call stack semantics from the current stack shape.
/// </summary>
public sealed class MethodCallTypeSemanticsResolver
{
    public MethodCallResolution ResolveForStack(CSharpCallDescriptor descriptor, IReadOnlyList<Type> currentStack)
    {
        descriptor = descriptor.ArgNotNull();
        currentStack = currentStack.ArgNotNull();

        var method = descriptor.Method;
        var parameters = method.GetParameters();
        var parameterCount = parameters.Length;

        Thrower.AssertAlways(
            currentStack.Count >= parameterCount,
            $"Current stack does not contain enough values to resolve '{method}'.");

        var stackTypes = currentStack.TakeLast(parameterCount).ToList();
        var parameterTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
        var consumedTypes = parameterTypes.ToList();

        if (descriptor.Receiver is CSharpCallReceiver.Static)
        {
            if (!method.IsStatic)
            {
                Thrower.AssertAlways(method.DeclaringType != null, $"Method '{method}' must have a declaring type.");
                consumedTypes.Insert(0, method.DeclaringType);
            }
        }
        else if (descriptor.Receiver is not CSharpCallReceiver.ExecutionScopedProvider executionScopedProvider)
        {
            Thrower.InvalidOpEx($"Unsupported C# call receiver '{descriptor.Receiver.GetType().FullName}'.");
        }
        else
        {
            Thrower.AssertAlways(!method.IsStatic, $"Execution-scoped provider method '{method}' must be an instance method.");
            Thrower.AssertAlways(
                method.DeclaringType != null && method.DeclaringType.IsAssignableFrom(executionScopedProvider.ProviderType),
                $"Method '{method}' must be declared on provider type '{executionScopedProvider.ProviderType.FullName}' or its base type.");
        }

        if (method.IsGenericMethod)
            method = GenericTypeResolver.MakeGenericMethod(method, parameterTypes);

        return new MethodCallResolution
        {
            ConsumedTypes = consumedTypes,
            ReturnType = method.ReturnType
        };
    }

    public MethodCallResolution ResolveForStack(MethodInfo method, IReadOnlyList<Type> currentStack)
    {
        method = method.ArgNotNull();
        currentStack = currentStack.ArgNotNull();

        var parameters = method.GetParameters();
        var parameterCount = parameters.Length;

        Thrower.AssertAlways(
            currentStack.Count >= parameterCount,
            $"Current stack does not contain enough values to resolve '{method}'.");

        var stackTypes = currentStack.TakeLast(parameterCount).ToList();
        var parameterTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
        var consumedTypes = parameterTypes.ToList();

        if (!method.IsStatic)
        {
            Thrower.AssertAlways(method.DeclaringType != null, $"Method '{method}' must have a declaring type.");
            consumedTypes.Insert(0, method.DeclaringType);
        }

        if (method.IsGenericMethod)
            method = GenericTypeResolver.MakeGenericMethod(method, parameterTypes);

        return new MethodCallResolution
        {
            ConsumedTypes = consumedTypes,
            ReturnType = method.ReturnType
        };
    }
}