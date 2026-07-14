namespace BasicCore.Core.Rules;

/// <summary>
///     Pushes a managed by-reference type for the resolved local value type.
/// </summary>
public sealed class LoadLocalRefStackRule : IIntrinsicStackRule
{
    public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(
            invocation.TypeArguments.Count == 1,
            $"Intrinsic '{invocation.Symbol}' requires exactly one type argument.");

        var valueType = context.Resolve(invocation.TypeArguments[0]);
        stack.Add(valueType.MakeByRefType());
    }
}
