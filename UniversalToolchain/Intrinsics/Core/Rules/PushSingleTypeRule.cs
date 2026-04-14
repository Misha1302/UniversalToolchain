using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core.Rules;

/// <summary>
/// Pushes a single resolved type onto the stack.
/// </summary>
public sealed class PushSingleTypeRule : IIntrinsicStackRule
{
    private readonly Func<IntrinsicInvocation, IIntrinsicTypeResolutionContext, Type> _resolveType;

    public PushSingleTypeRule(Func<IntrinsicInvocation, IIntrinsicTypeResolutionContext, Type> resolveType)
    {
        resolveType = resolveType.ArgNotNull();

        _resolveType = resolveType;
    }

    public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
    {
        stack.Add(_resolveType(invocation, context));
    }
}
