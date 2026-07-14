using System.Reflection;
using DotnetHelper;
using IntermediateRepresentationAbstractions;

namespace BasicCore.Core;

/// <summary>
///     Resolves .NET method-call stack semantics from the current stack shape.
/// </summary>
public sealed class MethodCallTypeSemanticsResolver
{
    public MethodCallResolution ResolveForStack(IManagedCallDescriptor descriptor, IReadOnlyList<Type> currentStack)
    {
        descriptor = descriptor.ArgNotNull();
        currentStack = currentStack.ArgNotNull();

        var method = descriptor.Method;
        var parameters = method.GetParameters();
        Thrower.AssertAlways(
            currentStack.Count >= parameters.Length,
            $"Current stack does not contain enough values to resolve '{method}'.");

        var stackTypes = currentStack.TakeLast(parameters.Length).ToList();
        var parameterTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
        var consumedTypes = parameterTypes.ToList();

        switch (descriptor.ReceiverKind)
        {
            case ManagedCallReceiverKind.Static:
                if (!method.IsStatic)
                {
                    Thrower.AssertAlways(method.DeclaringType != null, $"Method '{method}' must have a declaring type.");
                    consumedTypes.Insert(0, method.DeclaringType);
                }
                break;
            case ManagedCallReceiverKind.ExecutionScopedProvider:
                var providerType = descriptor.ExecutionScopedProviderType;
                Thrower.AssertAlways(providerType != null, "Execution-scoped call descriptor requires a provider type.");
                Thrower.AssertAlways(!method.IsStatic, $"Execution-scoped provider method '{method}' must be an instance method.");
                Thrower.AssertAlways(
                    method.DeclaringType != null && method.DeclaringType.IsAssignableFrom(providerType),
                    $"Method '{method}' must be declared on provider type '{providerType.FullName}' or its base type.");
                break;
            default:
                Thrower.InvalidOpEx($"Unsupported managed-call receiver '{descriptor.ReceiverKind}'.");
                break;
        }

        if (method.IsGenericMethod)
            method = GenericTypeResolver.MakeGenericMethod(method, parameterTypes);

        return new MethodCallResolution { ConsumedTypes = consumedTypes, ReturnType = method.ReturnType };
    }

    public MethodCallResolution ResolveForStack(MethodInfo method, IReadOnlyList<Type> currentStack)
    {
        method = method.ArgNotNull();
        currentStack = currentStack.ArgNotNull();
        var parameters = method.GetParameters();
        Thrower.AssertAlways(currentStack.Count >= parameters.Length,
            $"Current stack does not contain enough values to resolve '{method}'.");
        var stackTypes = currentStack.TakeLast(parameters.Length).ToList();
        var parameterTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
        var consumedTypes = parameterTypes.ToList();
        if (!method.IsStatic)
        {
            Thrower.AssertAlways(method.DeclaringType != null, $"Method '{method}' must have a declaring type.");
            consumedTypes.Insert(0, method.DeclaringType);
        }
        if (method.IsGenericMethod)
            method = GenericTypeResolver.MakeGenericMethod(method, parameterTypes);
        return new MethodCallResolution { ConsumedTypes = consumedTypes, ReturnType = method.ReturnType };
    }
}
