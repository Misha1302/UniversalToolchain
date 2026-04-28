using System.Reflection;

namespace BasicCore.Core.Rules;

/// <summary>
///     Applies stack semantics for a reflected .NET method call.
/// </summary>
public sealed class CSharpCallStackRule : IIntrinsicStackRule
{
    private readonly MethodCallTypeSemanticsResolver _resolver;

    public CSharpCallStackRule(MethodCallTypeSemanticsResolver resolver)
    {
        resolver = resolver.ArgNotNull();

        _resolver = resolver;
    }

    public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(
            invocation.DataOperands.Count > 0,
            $"Intrinsic '{invocation.Symbol}' requires call operand.");

        MethodCallResolution resolution;
        if (invocation.DataOperands[0] is MethodInfo methodInfo)
            resolution = _resolver.ResolveForStack(methodInfo, stack);
        else if (invocation.DataOperands[0] is CSharpCallDescriptor descriptor)
            resolution = _resolver.ResolveForStack(descriptor, stack);
        else
            resolution = Thrower.InvalidOpEx<MethodCallResolution>(
                $"Intrinsic '{invocation.Symbol}' requires DataOperands[0] to be MethodInfo or CSharpCallDescriptor.");

        Thrower.AssertAlways(
            stack.Count >= resolution.ConsumedTypes.Count,
            $"Intrinsic '{invocation.Symbol}' requires {resolution.ConsumedTypes.Count} stack values.");

        var stackSlice = stack.TakeLast(resolution.ConsumedTypes.Count).ToArray();
        for (var index = 0; index < resolution.ConsumedTypes.Count; index++)
        {
            Thrower.AssertAlways(
                context.AreCompatible(resolution.ConsumedTypes[index], stackSlice[index]),
                $"Intrinsic '{invocation.Symbol}' requires operand '{stackSlice[index]}' to be compatible with '{resolution.ConsumedTypes[index]}'.");
        }

        stack.RemoveRange(stack.Count - resolution.ConsumedTypes.Count, resolution.ConsumedTypes.Count);
        if (resolution.ReturnType != typeof(void))
            stack.Add(resolution.ReturnType);
    }
}
