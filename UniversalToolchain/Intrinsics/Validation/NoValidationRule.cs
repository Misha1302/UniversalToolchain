using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Validation;

/// <summary>
///     Performs no intrinsic validation.
/// </summary>
public sealed class NoValidationRule : IIntrinsicValidationRule
{
    public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
    {
    }
}