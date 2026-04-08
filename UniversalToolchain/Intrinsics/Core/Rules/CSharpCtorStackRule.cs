using System.Reflection;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core.Rules;

/// <summary>
/// Applies stack semantics for a reflected .NET constructor call.
/// </summary>
public sealed class CSharpCtorStackRule : IIntrinsicStackRule
{
    public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(
            invocation.DataOperands.Count > 0 && invocation.DataOperands[0] is ConstructorInfo,
            $"Intrinsic '{invocation.Symbol}' requires DataOperands[0] to be a ConstructorInfo.");

        var ctor = (ConstructorInfo)invocation.DataOperands[0]!;
        Thrower.AssertAlways(ctor.DeclaringType != null, $"Constructor '{ctor}' must have a declaring type.");

        var parameterTypes = ctor.GetParameters().Select(x => x.ParameterType).ToArray();
        Thrower.AssertAlways(
            stack.Count >= parameterTypes.Length,
            $"Intrinsic '{invocation.Symbol}' requires {parameterTypes.Length} stack values.");

        var stackSlice = stack.TakeLast(parameterTypes.Length).ToArray();
        for (var index = 0; index < parameterTypes.Length; index++)
        {
            Thrower.AssertAlways(
                context.AreCompatible(parameterTypes[index], stackSlice[index]),
                $"Intrinsic '{invocation.Symbol}' requires operand '{stackSlice[index]}' to be compatible with '{parameterTypes[index]}'.");
        }

        stack.RemoveRange(stack.Count - parameterTypes.Length, parameterTypes.Length);
        stack.Add(ctor.DeclaringType);
    }
}
