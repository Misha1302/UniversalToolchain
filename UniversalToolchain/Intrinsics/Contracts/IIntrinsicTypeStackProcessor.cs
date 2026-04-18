namespace BasicCore.Contracts;

public interface IIntrinsicTypeStackProcessor
{
    void Process(
        IntrinsicInvocation invocation,
        List<Type> stack);
}