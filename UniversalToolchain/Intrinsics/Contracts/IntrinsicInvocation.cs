namespace UniversalToolchain.Intrinsics.Contracts;

public sealed record IntrinsicInvocation
{
    public IntrinsicInvocation(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        IReadOnlyList<object?> dataOperands)
    {
        typeArguments = typeArguments.ArgNotNull();

        dataOperands = dataOperands.ArgNotNull();

        Symbol = symbol;
        TypeArguments = [.. typeArguments];
        DataOperands = [.. dataOperands];
    }

    public IntrinsicSymbol Symbol { get; }
    public IReadOnlyList<IntrinsicTypeArgument> TypeArguments { get; }
    public IReadOnlyList<object?> DataOperands { get; }
}
