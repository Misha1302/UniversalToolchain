namespace UniversalToolchain.Intrinsics.Contracts;

public interface IIntrinsicValidationRule
{
    void Validate(
        IntrinsicInvocation invocation,
        IIntrinsicTypeResolutionContext context);
}