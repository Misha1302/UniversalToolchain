using System.Reflection;
using BasicCore.Core;

namespace BasicCore.Validation;

/// <summary>
///     Requires the first intrinsic operand to be a <see cref="MethodInfo" /> or a <see cref="CSharpCallDescriptor" />.
/// </summary>
public sealed class MethodInfoOperandValidationRule : IIntrinsicValidationRule
{
    public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(
            invocation.DataOperands.Count > 0,
            $"Intrinsic '{invocation.Symbol}' requires a C# call operand.");
        Thrower.AssertAlways(
            invocation.DataOperands[0] is MethodInfo or CSharpCallDescriptor,
            $"Intrinsic '{invocation.Symbol}' requires DataOperands[0] to be MethodInfo or CSharpCallDescriptor.");
    }
}