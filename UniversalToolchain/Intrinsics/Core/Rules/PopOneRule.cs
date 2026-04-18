namespace BasicCore.Core.Rules;

/// <summary>
///     Pops a single value from the stack.
/// </summary>
public sealed class PopOneRule : IIntrinsicStackRule
{
    public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(stack.Count > 0, $"Intrinsic '{invocation.Symbol}' requires at least one stack value.");
        stack.RemoveAt(stack.Count - 1);
    }
}