using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core.Rules;

/// <summary>
///     Consumes two booleans and pushes one boolean.
/// </summary>
public sealed class BooleanBinaryRule : IIntrinsicStackRule
{
    public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(stack.Count >= 2, $"Intrinsic '{invocation.Symbol}' requires two boolean operands.");
        Thrower.AssertAlways(
            stack[^2] == typeof(bool) && stack[^1] == typeof(bool),
            $"Intrinsic '{invocation.Symbol}' requires two boolean operands.");

        stack.RemoveAt(stack.Count - 1);
        stack[^1] = typeof(bool);
    }
}