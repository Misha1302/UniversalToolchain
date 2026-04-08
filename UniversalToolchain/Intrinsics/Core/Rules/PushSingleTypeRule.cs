using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core.Rules;

/// <summary>
/// Pushes a single resolved type onto the stack.
/// </summary>
public sealed class PushSingleTypeRule(
    Func<IntrinsicInvocation, IIntrinsicTypeResolutionContext, Type> resolveType) : IIntrinsicStackRule
{
    public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(resolveType);

        stack.Add(resolveType(invocation, context));
    }
}
