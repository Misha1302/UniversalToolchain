using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Validation;

/// <summary>
/// Requires an intrinsic invocation to contain an exact number of data operands.
/// </summary>
public sealed class ExpectedDataOperandCountRule(int expectedCount) : IIntrinsicValidationRule
{
    public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(
            invocation.DataOperands.Count == expectedCount,
            $"Expected {expectedCount} data operands for intrinsic '{invocation.Symbol}', but found {invocation.DataOperands.Count}.");
    }
}
