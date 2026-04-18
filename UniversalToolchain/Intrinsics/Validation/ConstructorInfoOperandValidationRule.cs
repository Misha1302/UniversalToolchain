using System.Reflection;

namespace BasicCore.Validation;

/// <summary>
///     Requires the first intrinsic operand to be a <see cref="ConstructorInfo" />.
/// </summary>
public sealed class ConstructorInfoOperandValidationRule : IIntrinsicValidationRule
{
    public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(
            invocation.DataOperands.Count > 0,
            $"Intrinsic '{invocation.Symbol}' requires a ConstructorInfo operand.");
        Thrower.AssertAlways(
            invocation.DataOperands[0] is ConstructorInfo,
            $"Intrinsic '{invocation.Symbol}' requires DataOperands[0] to be a ConstructorInfo.");
    }
}