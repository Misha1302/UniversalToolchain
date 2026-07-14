namespace BasicCore.Contracts;

public sealed record IntrinsicInvocation
{
    public IntrinsicInvocation(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        IReadOnlyList<object?> dataOperands)
    {
        ArgumentNullException.ThrowIfNull(typeArguments);
        ArgumentNullException.ThrowIfNull(dataOperands);

        Symbol = symbol;
        TypeArguments = [.. typeArguments];
        DataOperands = [.. dataOperands];
    }

    public IntrinsicSymbol Symbol { get; }
    public IReadOnlyList<IntrinsicTypeArgument> TypeArguments { get; }
    public IReadOnlyList<object?> DataOperands { get; }
}
