using DotnetHelper;
using System.Reflection;

namespace UniversalToolchain.Intrinsics.Core;

/// <summary>
/// Resolves .NET method-call stack semantics from the current stack shape.
/// </summary>
public sealed class MethodCallTypeSemanticsResolver
{
    public MethodCallResolution ResolveForStack(MethodInfo method, IReadOnlyList<Type> currentStack)
    {
        if (method == null)
            Thrower.ArgumentNull(nameof(method));
        if (currentStack == null)
            Thrower.ArgumentNull(nameof(currentStack));

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
        {
            method = GenericTypeResolver.MakeGenericMethod(method, parameterTypes);
        }

        return new MethodCallResolution
        {
            ConsumedTypes = consumedTypes,
            ReturnType = method.ReturnType
        };
    }
}
