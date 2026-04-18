using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core.Rules;

/// <summary>
///     Applies a binary operation that consumes two values of the same expected type and pushes the same type.
/// </summary>
public sealed class BinarySameTypeResultRule : IIntrinsicStackRule
{
    public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(
            invocation.TypeArguments.Count == 1,
            $"Intrinsic '{invocation.Symbol}' requires exactly one type argument.");
        Thrower.AssertAlways(
            stack.Count >= 2,
            $"Intrinsic '{invocation.Symbol}' requires at least two stack values.");

        var expectedType = context.Resolve(invocation.TypeArguments[0]);
        var leftType = stack[^2];
        var rightType = stack[^1];

        Thrower.AssertAlways(
            context.AreCompatible(expectedType, leftType) && context.AreCompatible(expectedType, rightType),
            $"Intrinsic '{invocation.Symbol}' requires both operands to be compatible with '{expectedType}'.");

        stack.RemoveAt(stack.Count - 1);
        stack.RemoveAt(stack.Count - 1);
        stack.Add(expectedType);
    }
}