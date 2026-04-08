namespace UniversalToolchain.Intrinsics.Contracts;

public sealed record IntrinsicInvocation
{
    public IntrinsicInvocation(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        IReadOnlyList<object?> dataOperands)
    {
        if (typeArguments == null)
            Thrower.ArgumentNull(nameof(typeArguments));

        if (dataOperands == null)
            Thrower.ArgumentNull(nameof(dataOperands));

        Symbol = symbol;
        TypeArguments = [.. typeArguments];
        DataOperands = [.. dataOperands];
    }

    public IntrinsicSymbol Symbol { get; }
    public IReadOnlyList<IntrinsicTypeArgument> TypeArguments { get; }
    public IReadOnlyList<object?> DataOperands { get; }
}
