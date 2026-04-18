namespace BasicCore.Contracts;

public interface IIntrinsicValidationRule
{
    void Validate(
        IntrinsicInvocation invocation,
        IIntrinsicTypeResolutionContext context);
}