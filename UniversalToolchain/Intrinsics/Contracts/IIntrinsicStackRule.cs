namespace BasicCore.Contracts;

public interface IIntrinsicStackRule
{
    void Apply(
        IntrinsicInvocation invocation,
        List<Type> stack,
        IIntrinsicTypeResolutionContext context);
}