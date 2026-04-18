namespace BasicCore.Core.Rules;

/// <summary>
///     Consumes one boolean and pushes one boolean.
/// </summary>
public sealed class BooleanUnaryRule : IIntrinsicStackRule
{
    public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(stack.Count >= 1, $"Intrinsic '{invocation.Symbol}' requires one boolean operand.");
        Thrower.AssertAlways(stack[^1] == typeof(bool), $"Intrinsic '{invocation.Symbol}' requires a boolean operand.");

        stack[^1] = typeof(bool);
    }
}