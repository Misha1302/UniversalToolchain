using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Validation;

/// <summary>
///     Requires an intrinsic invocation to contain an exact number of type arguments.
/// </summary>
public sealed class ExpectedTypeArgumentCountRule(int expectedCount) : IIntrinsicValidationRule
{
    public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
    {
        Thrower.AssertAlways(
            invocation.TypeArguments.Count == expectedCount,
            $"Expected {expectedCount} type arguments for intrinsic '{invocation.Symbol}', but found {invocation.TypeArguments.Count}.");
    }
}