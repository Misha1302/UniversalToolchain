using System.Reflection;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Validation;

/// <summary>
///     Requires the first intrinsic operand to be a <see cref="MethodInfo" />.
/// </summary>
public sealed class MethodInfoOperandValidationRule : IIntrinsicValidationRule
{
    public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(
            invocation.DataOperands.Count > 0,
            $"Intrinsic '{invocation.Symbol}' requires a MethodInfo operand.");
        Thrower.AssertAlways(
            invocation.DataOperands[0] is MethodInfo,
            $"Intrinsic '{invocation.Symbol}' requires DataOperands[0] to be a MethodInfo.");
    }
}