namespace BasicCore.Validation;

/// <summary>
///     Performs no intrinsic validation.
/// </summary>
public sealed class NoValidationRule : IIntrinsicValidationRule
{
    public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
    {
    }
}